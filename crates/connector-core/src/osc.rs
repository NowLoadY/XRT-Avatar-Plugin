//! OSC connector metadata and buffered plain-text encoding. The host owns I/O.
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
        let mut glyphs = Vec::with_capacity(self.contract.slots + 1);
        let mut previous_line_empty = false;
        'lines: for (line_index, line) in normalized.trim().split('\n').enumerate() {
            if line_index > 0 {
                let remaining = self.columns - glyphs.len() % self.columns;
                if remaining != self.columns || previous_line_empty {
                    glyphs.resize((glyphs.len() + remaining).min(self.contract.slots + 1), 0);
                }
            }
            previous_line_empty = line.trim().is_empty();
            for (word_index, word) in line.split_whitespace().enumerate() {
                let width = word.chars().count();
                let column = glyphs.len() % self.columns;
                if word_index > 0 && column != 0 {
                    if width <= self.columns && column + width + 1 > self.columns {
                        glyphs.resize(
                            (glyphs.len() + self.columns - column).min(self.contract.slots + 1),
                            0,
                        );
                    } else {
                        glyphs.push(0x20);
                    }
                }
                for character in word.chars().filter(|c| !c.is_control()) {
                    if glyphs.len() > self.contract.slots {
                        break 'lines;
                    }
                    glyphs.push(u16::try_from(character as u32).unwrap_or(0xfffd));
                }
            }
            if glyphs.len() > self.contract.slots {
                break;
            }
        }
        if glyphs.len() > self.contract.slots {
            glyphs.truncate(self.contract.slots);
            glyphs[self.contract.slots - 1] = 0x2026;
        }
        glyphs
    }

    pub fn frames(self, text: &str) -> VecDeque<Vec<(String, i32)>> {
        let glyphs = self.glyphs(text);
        let mut frames = VecDeque::from([vec![self.clear_write()]]);
        for (pointer, chunk) in glyphs.chunks(self.bytes / 2).enumerate() {
            let mut bytes = chunk
                .iter()
                .flat_map(|glyph| glyph.to_be_bytes())
                .chain(std::iter::repeat(0));
            let mut values = Vec::with_capacity(self.bytes / self.contract.integer_bytes + 1);
            for index in 0..self.bytes / self.contract.integer_bytes {
                let packed = bytes
                    .by_ref()
                    .take(self.contract.integer_bytes)
                    .fold(0u32, |value, byte| (value << 8) | u32::from(byte));
                values.push((
                    format!("{}{index}", self.contract.data_prefix),
                    packed as i32,
                ));
            }
            // One OSC bundle writes payload before committing its chunk pointer.
            values.push((self.contract.pointer.into(), pointer as i32));
            frames.push_back(values);
        }
        frames
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
}
