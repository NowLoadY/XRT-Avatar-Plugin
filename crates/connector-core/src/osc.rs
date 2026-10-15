//! OSC connector metadata and buffered plain-text encoding. The host owns I/O.
use crate::DisplaySnapshot;
use std::{collections::VecDeque, time::Duration};
use unicode_normalization::UnicodeNormalization;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct Peer {
    pub service_name: &'static str,
    pub identity_path: &'static str,
    pub invalidate_paths: &'static [&'static str],
    pub mute_path: Option<&'static str>,
}

impl Default for Peer {
    fn default() -> Self {
        crate::catalog::DEFAULT_PEER
    }
}

impl Peer {
    pub fn accepts_name(self, name: &str) -> bool {
        name.eq_ignore_ascii_case(self.service_name)
            || (name
                .get(..self.service_name.len())
                .is_some_and(|prefix| prefix.eq_ignore_ascii_case(self.service_name))
                && name.as_bytes().get(self.service_name.len()) == Some(&b'-'))
    }

    pub fn invalidates_on(self, path: &str) -> bool {
        self.invalidate_paths.contains(&path)
    }
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct Contract {
    pub peer: Peer,
    pub enabled: &'static str,
    pub status: &'static str,
    pub version: &'static str,
    pub protocol_version: i32,
    pub caption: CaptionContract,
}

impl Contract {
    pub fn paths(&self) -> Vec<String> {
        let mut paths = [self.enabled, self.status, self.version]
            .map(str::to_owned)
            .to_vec();
        paths.extend(self.caption.paths());
        paths
    }
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct CaptionContract {
    pub version: &'static str,
    pub columns: &'static str,
    pub bytes: &'static str,
    pub pointer: &'static str,
    pub data_prefix: &'static str,
    pub protocol_version: i32,
    pub slots: usize,
    pub clear_pointer: i32,
    pub integer_bytes: usize,
    pub column_counts: &'static [usize],
    pub bytes_per_chunk: &'static [usize],
    pub interval_ms: u64,
    pub receiver_idle_ms: Option<u64>,
}

impl CaptionContract {
    pub fn paths(&self) -> Vec<String> {
        [self.version, self.columns, self.bytes, self.pointer]
            .into_iter()
            .map(str::to_owned)
            .chain(
                (0..self.bytes_per_chunk.iter().copied().max().unwrap_or(0)
                    / self.integer_bytes.max(1))
                    .map(|index| format!("{}{index}", self.data_prefix)),
            )
            .collect()
    }
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct CaptionLayout {
    contract: &'static CaptionContract,
    columns: usize,
    bytes: usize,
}

impl CaptionLayout {
    /// The lookup checks OSC integer type and requested read/write access.
    pub fn discover(
        contract: &'static CaptionContract,
        mut integer: impl FnMut(&str, u8) -> Option<i32>,
    ) -> Option<Self> {
        if integer(contract.version, 1)? != contract.protocol_version {
            return None;
        }
        let columns = usize::try_from(integer(contract.columns, 1)?).ok()?;
        let bytes = usize::try_from(integer(contract.bytes, 1)?).ok()?;
        if columns == 0
            || bytes < 2
            || bytes % 2 != 0
            || !contract.column_counts.contains(&columns)
            || !contract.bytes_per_chunk.contains(&bytes)
            || !matches!(contract.integer_bytes, 1 | 4)
            || bytes % contract.integer_bytes != 0
            || !(0..=contract.clear_pointer).contains(&integer(contract.pointer, 3)?)
        {
            return None;
        }
        for index in 0..bytes / contract.integer_bytes {
            let value = integer(&format!("{}{index}", contract.data_prefix), 3)?;
            if contract.integer_bytes == 1 && !(0..=255).contains(&value) {
                return None;
            }
        }
        Some(Self {
            contract,
            columns,
            bytes,
        })
    }

    pub fn clear_write(self) -> (String, i32) {
        (self.contract.pointer.into(), self.contract.clear_pointer)
    }

    pub fn interval(self) -> Duration {
        Duration::from_millis(self.contract.interval_ms)
    }

    fn glyphs(self, text: &str) -> Vec<u16> {
        self.glyphs_with_limit(text, self.contract.slots)
    }

    fn glyphs_with_limit(self, text: &str, limit: usize) -> Vec<u16> {
        let normalized: String = text
            .chars()
            .map(|c| {
                if c as u32 > 0xffff {
                    '\u{fffd}'
                } else if c.is_control() && c != '\n' {
                    ' '
                } else {
                    c
                }
            })
            .nfc()
            .collect();
        let mut glyphs = Vec::with_capacity(limit + 1);
        let mut previous_line_empty = false;
        'lines: for (line_index, line) in normalized.trim().split('\n').enumerate() {
            if line_index > 0 {
                let remaining = self.columns - glyphs.len() % self.columns;
                if remaining != self.columns || previous_line_empty {
                    glyphs.resize((glyphs.len() + remaining).min(limit + 1), 0);
                }
            }
            previous_line_empty = line.trim().is_empty();
            for (word_index, word) in line.split_whitespace().enumerate() {
                let width = word.chars().count();
                let column = glyphs.len() % self.columns;
                if word_index > 0 && column != 0 {
                    if width <= self.columns && column + width + 1 > self.columns {
                        glyphs.resize((glyphs.len() + self.columns - column).min(limit + 1), 0);
                    } else {
                        glyphs.push(0x20);
                    }
                }
                for character in word.chars().filter(|c| !c.is_control()) {
                    if glyphs.len() > limit {
                        break 'lines;
                    }
                    glyphs.push(u16::try_from(character as u32).unwrap_or(0xfffd));
                }
            }
            if glyphs.len() > limit {
                break;
            }
        }
        if glyphs.len() > limit {
            glyphs.truncate(limit);
            glyphs[limit - 1] = 0x2026;
        }
        glyphs
    }

    pub fn frames(self, text: &str) -> VecDeque<Vec<(String, i32)>> {
        let glyphs = self.glyphs(text);
        let mut frames = VecDeque::from([vec![self.clear_write()]]);
        for (pointer, chunk) in glyphs.chunks(self.bytes / 2).enumerate() {
            frames.push_back(self.write_chunk(pointer, &self.pack(chunk)));
        }
        frames
    }

    fn pack(self, chunk: &[u16]) -> Vec<i32> {
        let mut bytes = chunk
            .iter()
            .flat_map(|glyph| glyph.to_be_bytes())
            .chain(std::iter::repeat(0));
        (0..self.bytes / self.contract.integer_bytes)
            .map(|_| {
                bytes
                    .by_ref()
                    .take(self.contract.integer_bytes)
                    .fold(0u32, |value, byte| (value << 8) | u32::from(byte)) as i32
            })
            .collect()
    }

    fn write_chunk(self, pointer: usize, data: &[i32]) -> Vec<(String, i32)> {
        let mut values: Vec<_> = data
            .iter()
            .enumerate()
            .map(|(index, value)| (format!("{}{index}", self.contract.data_prefix), *value))
            .collect();
        // One OSC bundle writes payload before committing its chunk pointer.
        values.push((self.contract.pointer.into(), pointer as i32));
        values
    }

    fn canvas(self, snapshot: &DisplaySnapshot) -> (Vec<Vec<i32>>, Vec<usize>) {
        let snapshot = snapshot.normalized();
        let start = if snapshot.header.is_some() {
            self.columns
        } else {
            0
        };
        let end = self.contract.slots
            - if snapshot.footer.is_some() {
                self.columns
            } else {
                0
            };
        let capacity = end - start;
        let mut body = String::new();
        for message in snapshot.messages.iter().rev() {
            let candidate = if body.is_empty() {
                message.text.clone()
            } else {
                format!("{}\n{body}", message.text)
            };
            if body.is_empty() || self.glyphs_with_limit(&candidate, capacity + 1).len() <= capacity
            {
                body = candidate;
            } else {
                break;
            }
        }
        let mut cells = vec![0; self.contract.slots];
        for (offset, capacity, text) in [
            (0, self.columns, snapshot.header.as_deref().unwrap_or("")),
            (start, capacity, body.as_str()),
            (end, self.columns, snapshot.footer.as_deref().unwrap_or("")),
        ] {
            if text.is_empty() {
                continue;
            }
            let glyphs = self.glyphs_with_limit(text, capacity);
            cells[offset..offset + glyphs.len()].copy_from_slice(&glyphs);
        }
        let size = self.bytes / 2;
        let priority = (start / size..end / size)
            .chain(0..start / size)
            .chain(end / size..cells.len() / size)
            .collect();
        (
            cells.chunks(size).map(|chunk| self.pack(chunk)).collect(),
            priority,
        )
    }
}

/// One pending write; the delivery owner must confirm only actual sends.
#[derive(Clone, Debug)]
pub struct CaptionFrame {
    pub values: Vec<(String, i32)>,
    chunk: Option<usize>,
    data: Vec<i32>,
}

/// Latest desired display and locally delivered chunks. It performs no I/O.
pub struct CaptionBuffer {
    layout: CaptionLayout,
    desired: Vec<Vec<i32>>,
    delivered: Vec<Option<Vec<i32>>>,
    priority: Vec<usize>,
    clear_pending: bool,
    last_pointer: Option<usize>,
}

impl CaptionBuffer {
    pub fn new(layout: CaptionLayout) -> Self {
        let (desired, priority) = layout.canvas(&DisplaySnapshot::default());
        let delivered = vec![None; desired.len()];
        Self {
            layout,
            desired,
            delivered,
            priority,
            clear_pending: true,
            last_pointer: None,
        }
    }

    pub fn replace(&mut self, snapshot: &DisplaySnapshot) {
        (self.desired, self.priority) = self.layout.canvas(snapshot);
    }

    pub fn is_empty(&self) -> bool {
        self.desired.iter().flatten().all(|value| *value == 0)
    }

    pub fn clear(&mut self) {
        self.replace(&DisplaySnapshot::default());
        self.delivered.fill(None);
        self.clear_pending = true;
        self.last_pointer = None;
    }

    pub fn is_current(&self, frame: &CaptionFrame) -> bool {
        match frame.chunk {
            None => self.clear_pending,
            Some(index) => !self.clear_pending && self.desired[index] == frame.data,
        }
    }

    pub fn discard(&mut self, frame: &CaptionFrame) {
        if let Some(index) = frame.chunk {
            self.delivered[index] = None;
        }
        self.last_pointer = None;
    }

    pub fn confirm(&mut self, frame: CaptionFrame) {
        if let Some(index) = frame.chunk {
            self.delivered[index] = Some(frame.data);
            self.last_pointer = Some(index);
        } else {
            for entry in &mut self.delivered {
                *entry = Some(vec![
                    0;
                    self.layout.bytes / self.layout.contract.integer_bytes
                ]);
            }
            self.clear_pending = false;
            self.last_pointer = None;
        }
    }

    /// Empty receivers need no heartbeat. An idle receiver is rebuilt from the
    /// desired snapshot; otherwise a changed pointer renews its existing text.
    pub fn next_frame(&mut self, idle: Duration) -> Option<CaptionFrame> {
        if self.clear_pending {
            return Some(CaptionFrame {
                values: vec![self.layout.clear_write()],
                chunk: None,
                data: Vec::new(),
            });
        }
        let timeout = self
            .layout
            .contract
            .receiver_idle_ms
            .map(Duration::from_millis);
        if timeout.is_some_and(|timeout| idle >= timeout) {
            self.delivered.fill(None);
        }
        let changed = self
            .priority
            .iter()
            .copied()
            .find(|index| self.delivered[*index].as_ref() != Some(&self.desired[*index]));
        let renewal = timeout.is_some_and(|timeout| idle >= timeout / 2) && !self.is_empty();
        let index = changed.or_else(|| {
            renewal.then(|| (self.last_pointer.unwrap_or(0) + 1) % self.desired.len())
        })?;
        let data = self.desired[index].clone();
        Some(CaptionFrame {
            values: self.layout.write_chunk(index, &data),
            chunk: Some(index),
            data,
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::Connector;
    use std::collections::BTreeMap;

    #[test]
    fn profiles_preserve_byte_and_signed_word_wire_formats() {
        let snapshot = |contract: &CaptionContract| -> BTreeMap<_, _> {
            contract
                .paths()
                .into_iter()
                .map(|path| {
                    let value = match path.rsplit('/').next().unwrap() {
                        "Version" => 1,
                        "Columns" | "Bytes" => 8,
                        "Pointer" => 255,
                        "Word0" => i32::MIN,
                        "Word1" => i32::MAX,
                        _ => 0,
                    };
                    (path, value)
                })
                .collect()
        };
        let vrchat = &Connector::Vrchat.osc().unwrap().caption;
        let cvr = &Connector::Chilloutvr.osc().unwrap().caption;
        let vrchat_values = snapshot(vrchat);
        let mut cvr_values = snapshot(cvr);
        let byte_layout =
            CaptionLayout::discover(vrchat, |path, _| vrchat_values.get(path).copied()).unwrap();
        let word_layout =
            CaptionLayout::discover(cvr, |path, _| cvr_values.get(path).copied()).unwrap();
        assert_eq!(word_layout.glyphs("文한🙂A"), byte_layout.glyphs("文한🙂A"));
        let frames = word_layout.frames("文한🙂A");
        assert_eq!(frames[0], vec![(cvr.pointer.into(), 255)]);
        assert_eq!(frames[1].len(), 3);
        let decoded: Vec<_> = frames[1][..2]
            .iter()
            .flat_map(|(_, word)| [((*word as u32) >> 16) as u16, *word as u16])
            .collect();
        assert_eq!(decoded, vec![0x6587, 0xd55c, 0xfffd, 65]);
        assert!(frames[1][1].1 < 0);
        assert_eq!(frames[1][2], (cvr.pointer.into(), 0));
        assert!(!cvr.paths().iter().any(|path| path.ends_with("Byte0")));
        assert!(CaptionLayout::discover(vrchat, |path, _| cvr_values.get(path).copied()).is_none());
        cvr_values.remove(cvr.pointer);
        assert!(CaptionLayout::discover(cvr, |path, _| cvr_values.get(path).copied()).is_none());
    }

    #[test]
    fn layout_normalizes_wraps_truncates_and_clears_before_writing() {
        let layout = CaptionLayout {
            contract: &Connector::Vrchat.osc().unwrap().caption,
            columns: 16,
            bytes: 4,
        };
        let glyphs = layout.glyphs("A文\n🙂B");
        assert_eq!(&glyphs[..2], &[65, 0x6587]);
        assert!(glyphs[2..16].iter().all(|glyph| *glyph == 0));
        assert_eq!(&glyphs[16..], &[0xfffd, 66]);
        let frames = layout.frames("A文B");
        assert_eq!(frames[0], vec![layout.clear_write()]);
        assert_eq!(
            frames[1].iter().map(|(_, v)| *v).collect::<Vec<_>>(),
            vec![0, 65, 0x65, 0x87, 0]
        );
        assert_eq!(
            frames[2].iter().map(|(_, v)| *v).collect::<Vec<_>>(),
            vec![0, 66, 0, 0, 1]
        );
        assert_eq!(
            layout.glyphs("Cafe\u{301}\t한"),
            vec![67, 97, 102, 0xe9, 32, 0xd55c]
        );
        let wrapped = layout.glyphs("Meet us near the entrance");
        assert_eq!(
            &wrapped[16..24],
            &"entrance".encode_utf16().collect::<Vec<_>>()
        );
        assert_eq!(layout.glyphs("1234567890123456\nB")[16], 66);
        let truncated = layout.glyphs(&"文".repeat(130));
        assert_eq!(truncated.len(), layout.contract.slots);
        assert_eq!(truncated[layout.contract.slots - 1], 0x2026);
    }

    #[test]
    fn sections_update_without_clearing_and_only_confirmed_chunks_are_cached() {
        let layout = CaptionLayout {
            contract: &Connector::Chilloutvr.osc().unwrap().caption,
            columns: 32,
            bytes: 8,
        };
        let mut buffer = CaptionBuffer::new(layout);
        let mut snapshot = DisplaySnapshot {
            header: Some("12:00".into()),
            footer: Some("XRT".into()),
            messages: vec![crate::DisplayMessage {
                id: "1".into(),
                text: "Meet by the stairs".into(),
                ttl_ms: 8000,
            }],
        };
        buffer.replace(&snapshot);
        let clear = buffer.next_frame(Duration::ZERO).unwrap();
        assert!(clear.chunk.is_none());
        buffer.confirm(clear);
        let first = buffer.next_frame(Duration::ZERO).unwrap();
        assert_eq!(first.chunk, Some(8)); // The body precedes clock/footer updates.
        buffer.confirm(first);
        while let Some(frame) = buffer.next_frame(Duration::ZERO) {
            buffer.confirm(frame);
        }

        snapshot.header = Some("12:01".into());
        buffer.replace(&snapshot);
        let clock = buffer.next_frame(Duration::ZERO).unwrap();
        assert_eq!(clock.chunk, Some(1));
        assert_eq!(buffer.next_frame(Duration::ZERO).unwrap().data, clock.data);
        snapshot.header = Some("12:02".into());
        buffer.replace(&snapshot);
        assert!(!buffer.is_current(&clock));
        buffer.discard(&clock);
        let replacement = buffer.next_frame(Duration::ZERO).unwrap();
        assert_eq!(replacement.chunk, clock.chunk);
        assert_ne!(replacement.data, clock.data);
        buffer.confirm(replacement);
        assert!(buffer.next_frame(Duration::ZERO).is_none());

        snapshot.messages.clear();
        buffer.replace(&snapshot);
        let mut erased = 0;
        while let Some(frame) = buffer.next_frame(Duration::ZERO) {
            assert!((8..24).contains(&frame.chunk.unwrap()));
            assert!(frame.data.iter().all(|value| *value == 0));
            buffer.confirm(frame);
            erased += 1;
        }
        assert!(erased > 0);
        let previous = buffer.last_pointer;
        let renewal = buffer.next_frame(Duration::from_secs(10)).unwrap();
        assert!(renewal.chunk.is_some() && renewal.chunk != previous);
        assert!(buffer.is_current(&renewal));
        buffer.confirm(renewal);
        assert!(buffer.next_frame(Duration::ZERO).is_none());
        assert!(buffer.next_frame(Duration::from_secs(21)).is_some());
    }
}
