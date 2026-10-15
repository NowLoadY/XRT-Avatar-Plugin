//! A complete display value. Connection identity and ordering belong to its envelope.
use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct DisplaySnapshot {
    pub header: Option<String>,
    pub messages: Vec<DisplayMessage>,
    pub footer: Option<String>,
}

#[derive(Clone, Debug, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct DisplayMessage {
    pub id: String,
    pub text: String,
    pub ttl_ms: u64,
}

fn text(value: &str, limit: usize) -> String {
    value
        .chars()
        .filter(|c| !c.is_control() || *c == '\n')
        .take(limit)
        .collect::<String>()
        .trim()
        .to_owned()
}

impl DisplaySnapshot {
    /// Keep the newest eight messages; a zero lifetime removes a message.
    pub fn normalized(&self) -> Self {
        let banner = |value: &Option<String>| {
            value
                .as_ref()
                .map(|value| text(value, 128))
                .filter(|value| !value.is_empty())
        };
        let mut messages: Vec<_> = self
            .messages
            .iter()
            .rev()
            .filter_map(|message| {
                let value = text(&message.text, 256);
                (!value.is_empty() && message.ttl_ms > 0).then(|| DisplayMessage {
                    id: text(&message.id, 64),
                    text: value,
                    ttl_ms: message.ttl_ms.min(15_000),
                })
            })
            .take(8)
            .collect();
        messages.reverse();
        Self {
            header: banner(&self.header),
            messages,
            footer: banner(&self.footer),
        }
    }

    pub fn is_empty(&self) -> bool {
        self.header.is_none() && self.messages.is_empty() && self.footer.is_none()
    }

    /// Compatibility projection for a single 256-character caption field.
    /// The latest message gets its space before optional banners.
    pub fn legacy_text(&self) -> String {
        let value = self.normalized();
        let body = value
            .messages
            .last()
            .map(|message| message.text.as_str())
            .unwrap_or("");
        let mut remaining = 256usize.saturating_sub(body.chars().count());
        let mut take = |value: Option<&str>| {
            let limit = remaining.saturating_sub(1);
            let result = value.map(|value| text(value, limit)).unwrap_or_default();
            if !result.is_empty() {
                remaining = remaining.saturating_sub(result.chars().count() + 1);
            }
            result
        };
        let header = take(value.header.as_deref());
        let footer = take(value.footer.as_deref());
        [header.as_str(), body, footer.as_str()]
            .into_iter()
            .filter(|part| !part.is_empty())
            .collect::<Vec<_>>()
            .join("\n")
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn bounds_keep_latest_messages_and_legacy_body_space() {
        let mut snapshot = DisplaySnapshot {
            header: Some("h".repeat(200)),
            footer: Some("footer".into()),
            messages: (0..10)
                .map(|index| DisplayMessage {
                    id: index.to_string(),
                    text: "文".repeat(300),
                    ttl_ms: 8000,
                })
                .collect(),
        };
        snapshot.messages[9].ttl_ms = 0;
        let normalized = snapshot.normalized();
        assert_eq!(normalized.header.unwrap().chars().count(), 128);
        assert_eq!(normalized.messages.len(), 8);
        assert_eq!(normalized.messages[0].id, "1");
        assert_eq!(normalized.messages.last().unwrap().id, "8");
        assert_eq!(snapshot.legacy_text(), "文".repeat(256));
    }
}
