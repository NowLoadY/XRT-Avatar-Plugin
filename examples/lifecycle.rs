//! Executable integration example for asynchronous startup/cancellation races.
use xrt_avatar_control::{
    Controller, Effect, Generation, HostResult, OperationId, ResetToken, Status,
};

fn reset_token(effects: &[Effect]) -> ResetToken {
    effects
        .iter()
        .find_map(|effect| match effect {
            Effect::ResetEnabled { token } => Some(*token),
            _ => None,
        })
        .expect("forced OFF write carries its own reset token")
}

fn start(controller: &mut Controller, generation: Generation) -> OperationId {
    let effects = controller.request_enabled(generation, true);
    let operation = effects
        .iter()
        .find_map(|effect| match effect {
            Effect::Start(operation) => Some(*operation),
            _ => None,
        })
        .expect("ready On request starts one task");
    assert_eq!(controller.snapshot().status, Status::Starting);
    assert!(controller.request_enabled(generation, true).is_empty());
    operation
}

fn assert_blocked_true(controller: &mut Controller, generation: Generation, token: ResetToken) {
    // The reset UDP write may be lost while a new query still reads true.
    // Retrying that value reissues this barrier without starting or churning IDs.
    assert_eq!(
        controller.request_enabled(generation, true),
        vec![Effect::ResetEnabled { token }]
    );
    assert_eq!(
        controller.request_enabled(generation, true),
        vec![Effect::ResetEnabled { token }]
    );
    assert_eq!(controller.snapshot().pending_reset, Some(token));
    assert!(!controller.snapshot().reset_confirmed);
}

fn main() {
    let mut controller = Controller::default();
    assert_eq!(controller.snapshot().status, Status::Unavailable);

    let first = controller.bind();
    let initial_reset = reset_token(&first.effects);
    assert_eq!(initial_reset.generation(), first.generation);
    assert_blocked_true(&mut controller, first.generation, initial_reset);
    controller.request_enabled(first.generation, false);
    assert!(
        !controller.snapshot().reset_confirmed,
        "a false event is not a verified reset snapshot"
    );
    controller.confirm_reset(initial_reset);
    assert_eq!(controller.snapshot().status, Status::Off);
    assert_eq!(controller.snapshot().pending_reset, None);

    let operation = start(&mut controller, first.generation);
    let effects = controller.request_enabled(first.generation, false);
    let off_reset = reset_token(&effects);
    assert!(effects.contains(&Effect::Cancel(operation)));
    assert_eq!(controller.snapshot().status, Status::Stopping);
    assert!(controller
        .request_enabled(first.generation, false)
        .is_empty());
    assert!(controller
        .host_result(operation, HostResult::Started)
        .contains(&Effect::Cancel(operation)));
    assert_eq!(
        controller.snapshot().status,
        Status::Stopping,
        "late startup cannot undo Off"
    );
    controller.confirm_reset(off_reset);

    // An On request during cleanup is rejected by a new barrier. A late query
    // from the preceding Off reset cannot unlock it in this same generation.
    let rejected_on_reset = reset_token(&controller.request_enabled(first.generation, true));
    assert_ne!(rejected_on_reset, off_reset);
    assert!(controller.confirm_reset(off_reset).is_empty());
    assert_blocked_true(&mut controller, first.generation, rejected_on_reset);
    let terminal_reset = reset_token(&controller.host_result(operation, HostResult::Stopped));
    assert_ne!(terminal_reset, rejected_on_reset);
    assert_eq!(controller.snapshot().status, Status::Off);
    assert!(controller.confirm_reset(rejected_on_reset).is_empty());
    assert_blocked_true(&mut controller, first.generation, terminal_reset);
    controller.confirm_reset(terminal_reset);
    assert_eq!(
        controller.snapshot().operation,
        None,
        "reset confirmation never starts a task"
    );

    let operation = start(&mut controller, first.generation);
    controller.host_result(operation, HostResult::Started);
    assert_eq!(controller.snapshot().status, Status::Active);
    let second = controller.bind();
    let binding_reset = reset_token(&second.effects);
    assert!(second.effects.contains(&Effect::Cancel(operation)));
    assert_eq!(
        controller.snapshot().operation,
        Some(operation),
        "rebind retains cleanup ownership"
    );
    assert!(controller.confirm_reset(terminal_reset).is_empty());
    assert!(controller
        .request_enabled(first.generation, true)
        .is_empty());
    controller.confirm_reset(binding_reset);
    assert_eq!(controller.snapshot().status, Status::Stopping);
    let rejected_on_reset = reset_token(&controller.request_enabled(second.generation, true));
    assert_blocked_true(&mut controller, second.generation, rejected_on_reset);
    let cleanup_reset = reset_token(&controller.host_result(operation, HostResult::Stopped));
    assert_eq!(controller.snapshot().status, Status::Off);
    assert!(controller.confirm_reset(rejected_on_reset).is_empty());
    assert_blocked_true(&mut controller, second.generation, cleanup_reset);
    controller.confirm_reset(cleanup_reset);

    let current = start(&mut controller, second.generation);
    assert_ne!(current, operation);
    assert_eq!(
        controller.host_result(operation, HostResult::Started),
        vec![Effect::Cancel(operation)]
    );
    assert_eq!(controller.snapshot().operation, Some(current));
    let failure_reset = reset_token(
        &controller.host_result(current, HostResult::Failed("Microphone unavailable".into())),
    );
    assert_eq!(controller.snapshot().status, Status::Error);
    assert!(!controller.snapshot().requested_enabled);
    assert!(controller
        .request_enabled(second.generation, false)
        .is_empty());
    assert_eq!(
        controller.snapshot().diagnostic,
        Some("Microphone unavailable")
    );
    assert!(controller.confirm_reset(cleanup_reset).is_empty());
    assert_blocked_true(&mut controller, second.generation, failure_reset);
    assert_eq!(
        controller.snapshot().status,
        Status::Error,
        "lost reset cannot turn failure into restart"
    );
    controller.confirm_reset(failure_reset);
    assert_eq!(
        controller.snapshot().status,
        Status::Error,
        "confirmed Off retains startup diagnostics"
    );

    let current = start(&mut controller, second.generation);
    controller.host_result(current, HostResult::Started);
    let effects = controller.disconnect(second.generation, "OSC transport disconnected");
    assert!(effects.contains(&Effect::Cancel(current)));
    let disconnected_reset = reset_token(&effects);
    assert!(controller.confirm_reset(disconnected_reset).is_empty());
    assert_eq!(controller.snapshot().status, Status::Unavailable);
    assert_eq!(controller.snapshot().operation, Some(current));
    assert!(controller
        .request_enabled(second.generation, false)
        .is_empty());
    assert_eq!(
        controller.snapshot().diagnostic,
        Some("OSC transport disconnected")
    );
    assert!(controller
        .host_result(current, HostResult::Started)
        .contains(&Effect::Cancel(current)));
    controller.host_result(current, HostResult::Stopped);
    assert_eq!(controller.snapshot().status, Status::Unavailable);
    assert_eq!(
        controller.snapshot().diagnostic,
        Some("OSC transport disconnected")
    );

    let third = controller.bind();
    let binding_reset = reset_token(&third.effects);
    controller.confirm_reset(binding_reset);
    let current = start(&mut controller, third.generation);
    controller.host_result(current, HostResult::Started);
    let ended_reset = reset_token(&controller.host_result(current, HostResult::Stopped));
    assert_eq!(controller.snapshot().status, Status::Off);
    assert!(
        !controller.snapshot().requested_enabled,
        "natural completion clears menu intent"
    );
    assert!(controller.confirm_reset(binding_reset).is_empty());
    assert_blocked_true(&mut controller, third.generation, ended_reset);
    controller.request_enabled(third.generation, false);
    assert_eq!(controller.snapshot().pending_reset, Some(ended_reset));
    controller.confirm_reset(ended_reset);
    assert_eq!(controller.snapshot().operation, None);
    assert_eq!(controller.snapshot().status, Status::Off);

    println!("Lifecycle example passed: reset generations, lost writes, stale confirmations, cancellation, rebind and diagnostics.");
}
