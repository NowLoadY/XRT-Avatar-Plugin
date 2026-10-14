# Overte adapter

The Overte script adds a tablet control and a compact caption bubble below eye
level. Hover the control to see its action; captions appear only while translating.

[Installation and operation](../../docs/adapters/overte/installation.md)

Source: [`scripts/xrt-avatar.js`](scripts/xrt-avatar.js).

The adapter uses Overte's native Tablet, Text entity and WebSocket APIs. Caption
layout and appearance come from [`presentation/`](../../presentation/); native
font measurements keep the text inside the bubble without reducing its size.
