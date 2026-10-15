//! Connector contracts, plain-text encoding and lifecycle, without transport I/O.
//!
//! The host executes returned effects in order, associates callbacks with their
//! operation, and validates a fresh authoritative reset snapshot before calling
//! [`Controller::confirm_reset`]. Parameter-change events alone cannot confirm
//! a reset. Host resources remain owned until a terminal host result arrives.
mod catalog;
mod display;
pub mod osc;

pub use catalog::Connector;
pub use display::{DisplayMessage, DisplaySnapshot};
/// Identity of one verified client/transport binding attempt.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub struct Generation(u64);

impl Generation {
    pub const fn value(self) -> u64 {
        self.0
    }
}

/// Identifies one forced OFF write and its subsequent authoritative snapshot.
/// A binding can contain many resets; a query from an earlier reset cannot
/// unlock a later one, even when both refer to the same binding generation.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub struct ResetToken {
    generation: Generation,
    sequence: u64,
}

impl ResetToken {
    pub const fn generation(self) -> Generation {
        self.generation
    }

    pub const fn sequence(self) -> u64 {
        self.sequence
    }
}

/// Identity of one host task, preserved across asynchronous cancellation.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub struct OperationId {
    generation: Generation,
    sequence: u64,
}

impl OperationId {
    pub const fn generation(self) -> Generation {
        self.generation
    }

    pub const fn sequence(self) -> u64 {
        self.sequence
    }
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Status {
    Unavailable,
    Off,
    Starting,
    Active,
    Stopping,
    Error,
}

impl Status {
    pub const fn name(self) -> &'static str {
        match self {
            Self::Unavailable => "unavailable",
            Self::Off => "off",
            Self::Starting => "starting",
            Self::Active => "active",
            Self::Stopping => "stopping",
            Self::Error => "error",
        }
    }

    pub const fn wire_value(self) -> i32 {
        match self {
            Self::Unavailable => 0,
            Self::Off => 1,
            Self::Starting => 2,
            Self::Active => 3,
            Self::Stopping => 4,
            Self::Error => 5,
        }
    }
}

/// Effects are executed by the host, never by this crate.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Effect {
    Start(OperationId),
    /// Cancel only this operation, including any pending startup.
    Cancel(OperationId),
    /// Write Enabled=false, then validate a fresh authoritative snapshot taken
    /// after this write. Only that snapshot can confirm this reset token.
    ResetEnabled {
        token: ResetToken,
    },
    /// Ignore a write if its binding generation is no longer current.
    WriteStatus {
        generation: Generation,
        value: Status,
    },
}

#[derive(Debug, PartialEq, Eq)]
pub struct BindingUpdate {
    pub generation: Generation,
    pub effects: Vec<Effect>,
}

/// A terminal result is valid only after the host has released this operation's
/// resources. Failure while cleanup is still pending is not a terminal result:
/// disconnect/cancel first, then report `Failed` or `Stopped` after cleanup.
#[derive(Debug, PartialEq, Eq)]
pub enum HostResult {
    Started,
    Stopped,
    /// Covers a rejected startup or a failed operation after cleanup.
    Failed(String),
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct Snapshot<'a> {
    pub generation: Generation,
    pub status: Status,
    pub requested_enabled: bool,
    pub reset_confirmed: bool,
    pub pending_reset: Option<ResetToken>,
    pub operation: Option<OperationId>,
    pub diagnostic: Option<&'a str>,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum Connection {
    Disconnected,
    AwaitingReset,
    Ready,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum Phase {
    Starting,
    Active,
    Stopping,
}

#[derive(Clone, Copy, Debug)]
struct Operation {
    id: OperationId,
    phase: Phase,
}

/// Keep one controller alive for the complete lifetime of its host operations.
/// Rebinding does not abandon a pending cancellation or start a second task.
pub struct Controller {
    generation: Generation,
    next_sequence: u64,
    next_reset_sequence: u64,
    pending_reset: Option<ResetToken>,
    connection: Connection,
    requested_enabled: bool,
    operation: Option<Operation>,
    diagnostic: Option<String>,
}

impl Default for Controller {
    fn default() -> Self {
        Self {
            generation: Generation(0),
            next_sequence: 0,
            next_reset_sequence: 0,
            pending_reset: None,
            connection: Connection::Disconnected,
            requested_enabled: false,
            operation: None,
            diagnostic: None,
        }
    }
}

impl Controller {
    pub fn snapshot(&self) -> Snapshot<'_> {
        Snapshot {
            generation: self.generation,
            status: self.status(),
            requested_enabled: self.requested_enabled,
            reset_confirmed: self.connection == Connection::Ready && self.pending_reset.is_none(),
            pending_reset: self.pending_reset,
            operation: self.operation.map(|operation| operation.id),
            diagnostic: self.diagnostic.as_deref(),
        }
    }

    /// Begin a new binding after the host has verified connector capabilities.
    /// The returned reset must be followed by a fresh authoritative OFF snapshot
    /// tagged with its reset token, even if the old client was already OFF.
    pub fn bind(&mut self) -> BindingUpdate {
        self.generation = Generation(
            self.generation
                .0
                .checked_add(1)
                .expect("binding generation exhausted"),
        );
        self.connection = Connection::AwaitingReset;
        self.requested_enabled = false;
        self.diagnostic = None;
        let mut effects = Vec::new();
        self.cancel_operation(&mut effects);
        effects.push(self.begin_reset());
        effects.push(self.status_write());
        BindingUpdate {
            generation: self.generation,
            effects,
        }
    }

    /// Confirm that the host verified an authoritative Enabled=false snapshot
    /// obtained after the write associated with this exact token. An unsolicited false
    /// event or timeout is not sufficient evidence. Old queries cannot confirm
    /// a later reset in the same binding. Confirming a reset never starts work.
    pub fn confirm_reset(&mut self, token: ResetToken) -> Vec<Effect> {
        if self.pending_reset != Some(token) || self.connection == Connection::Disconnected {
            return Vec::new();
        }
        let before = self.status();
        self.pending_reset = None;
        self.connection = Connection::Ready;
        let mut effects = Vec::new();
        self.append_status(before, &mut effects);
        effects
    }

    /// Apply an observed menu request from the verified current binding.
    /// On requests during reset or cleanup are reset to OFF, never queued for a
    /// later automatic start. Repeated values do not create additional tasks.
    pub fn request_enabled(&mut self, generation: Generation, enabled: bool) -> Vec<Effect> {
        if generation != self.generation || self.connection == Connection::Disconnected {
            return Vec::new();
        }
        if let Some(token) = self.pending_reset {
            return if enabled {
                vec![Effect::ResetEnabled { token }]
            } else {
                Vec::new()
            };
        }
        let before = self.status();
        let mut effects = Vec::new();
        if enabled {
            if let Some(operation) = self.operation {
                if operation.phase == Phase::Stopping || operation.id.generation != generation {
                    return vec![self.begin_reset()];
                }
                return Vec::new();
            }
            self.diagnostic = None;
            self.requested_enabled = true;
            self.next_sequence = self
                .next_sequence
                .checked_add(1)
                .expect("operation sequence exhausted");
            let id = OperationId {
                generation,
                sequence: self.next_sequence,
            };
            self.operation = Some(Operation {
                id,
                phase: Phase::Starting,
            });
            effects.push(Effect::Start(id));
        } else {
            let was_enabled = std::mem::replace(&mut self.requested_enabled, false);
            self.cancel_operation(&mut effects);
            if was_enabled {
                effects.push(self.begin_reset());
            }
        }
        self.append_status(before, &mut effects);
        effects
    }

    /// Consume a callback associated with the exact task returned by Start.
    /// A late Started callback after cancellation produces another Cancel;
    /// callbacks from an older operation cannot activate a newer binding.
    pub fn host_result(&mut self, id: OperationId, result: HostResult) -> Vec<Effect> {
        let Some(operation) = self.operation.filter(|operation| operation.id == id) else {
            return if result == HostResult::Started {
                vec![Effect::Cancel(id)]
            } else {
                Vec::new()
            };
        };
        let before = self.status();
        let mut effects = Vec::new();
        match result {
            HostResult::Started => {
                if operation.phase == Phase::Stopping
                    || self.connection != Connection::Ready
                    || id.generation != self.generation
                    || !self.requested_enabled
                {
                    self.operation = Some(Operation {
                        id,
                        phase: Phase::Stopping,
                    });
                    effects.push(Effect::Cancel(id));
                } else {
                    self.operation = Some(Operation {
                        id,
                        phase: Phase::Active,
                    });
                }
            }
            HostResult::Stopped => {
                self.operation = None;
                self.requested_enabled = false;
                effects.push(self.begin_reset());
            }
            HostResult::Failed(message) => {
                self.operation = None;
                self.requested_enabled = false;
                if id.generation == self.generation && self.connection == Connection::Ready {
                    self.diagnostic = Some(message);
                }
                effects.push(self.begin_reset());
            }
        }
        self.append_status(before, &mut effects);
        effects
    }

    /// Invalidate this binding and request cleanup without forgetting its owner.
    /// Writes are best effort and must never be delivered to a newer binding.
    /// Offline false events and terminal cleanup preserve this diagnostic.
    pub fn disconnect(
        &mut self,
        generation: Generation,
        diagnostic: impl Into<String>,
    ) -> Vec<Effect> {
        if generation != self.generation || self.connection == Connection::Disconnected {
            return Vec::new();
        }
        let before = self.status();
        self.connection = Connection::Disconnected;
        self.requested_enabled = false;
        self.diagnostic = Some(diagnostic.into());
        let mut effects = Vec::new();
        self.cancel_operation(&mut effects);
        effects.push(self.begin_reset());
        self.append_status(before, &mut effects);
        effects
    }

    fn status(&self) -> Status {
        if self.connection != Connection::Ready {
            return Status::Unavailable;
        }
        if let Some(operation) = self.operation {
            return match operation.phase {
                Phase::Starting => Status::Starting,
                Phase::Active => Status::Active,
                Phase::Stopping => Status::Stopping,
            };
        }
        if self.diagnostic.is_some() {
            Status::Error
        } else {
            Status::Off
        }
    }

    fn cancel_operation(&mut self, effects: &mut Vec<Effect>) {
        if let Some(operation) = &mut self.operation {
            if operation.phase != Phase::Stopping {
                operation.phase = Phase::Stopping;
                effects.push(Effect::Cancel(operation.id));
            }
        }
    }

    fn append_status(&self, before: Status, effects: &mut Vec<Effect>) {
        if before != self.status() {
            effects.push(self.status_write());
        }
    }

    fn begin_reset(&mut self) -> Effect {
        self.next_reset_sequence = self
            .next_reset_sequence
            .checked_add(1)
            .expect("reset sequence exhausted");
        let token = ResetToken {
            generation: self.generation,
            sequence: self.next_reset_sequence,
        };
        self.pending_reset = Some(token);
        Effect::ResetEnabled { token }
    }

    fn status_write(&self) -> Effect {
        Effect::WriteStatus {
            generation: self.generation,
            value: self.status(),
        }
    }
}
