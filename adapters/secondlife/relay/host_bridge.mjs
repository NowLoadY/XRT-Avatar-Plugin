#!/usr/bin/env node
// HTTP transport bridge. Session policy remains in the connector and XRTranslate.
import { randomUUID } from 'node:crypto';
import { setTimeout as delay } from 'node:timers/promises';

const relay = new URL(process.env.XRT_RELAY_URL || 'https://invalid.example');
const host = new URL(process.env.XRT_CONNECTOR_URL || 'ws://127.0.0.1:0');
const relayToken = process.env.XRT_RELAY_HOST_TOKEN || '';
const localToken = process.env.XRT_CONNECTOR_TOKEN || '';
if (!process.env.XRT_RELAY_URL || relay.protocol !== 'https:' || relay.username ||
    relay.password || relay.search || relay.hash || relay.pathname !== '/' ||
    host.protocol !== 'ws:' || !['localhost', '127.0.0.1', '[::1]'].includes(host.hostname) ||
    host.username || host.password || host.hash || !process.env.XRT_CONNECTOR_URL ||
    relayToken.length < 32 || !localToken) {
  throw new Error('Set HTTPS XRT_RELAY_URL, loopback XRT_CONNECTOR_URL and both host tokens.');
}
relay.pathname = '/v1/host/exchange';
let instance = randomUUID();
let sequence = 0;
let generation = '';
let outgoing = [];
let websocket = null;
let restart = false;
let stopping = false;

function closeSocket() {
  const previous = websocket;
  websocket = null;
  if (previous) previous.close();
}
function invalidate() {
  closeSocket();
  outgoing = [];
  instance = randomUUID();
  sequence = 0;
  generation = '';
  restart = false;
}
function textWithinBytes(value, limit) {
  let text = '';
  let bytes = 0;
  for (const character of String(value)) {
    const size = Buffer.byteLength(character);
    if (bytes + size > limit) break;
    text += character;
    bytes += size;
  }
  return text;
}
async function openSocket(hello) {
  closeSocket();
  const socket = new WebSocket(host);
  websocket = socket;
  socket.addEventListener('message', event => {
    if (socket !== websocket) return;
    try {
      if (typeof event.data !== 'string' || event.data.length > 32768) throw new Error();
      const frame = JSON.parse(event.data);
      if (!frame || typeof frame !== 'object' || Array.isArray(frame)) throw new Error();
      if (frame.type === 'caption') frame.text = textWithinBytes(frame.text, 254);
      if (Buffer.byteLength(JSON.stringify(frame)) > 2048 || outgoing.length >= 16) throw new Error();
      outgoing.push(frame);
    } catch {
      restart = true;
      closeSocket();
    }
  });
  socket.addEventListener('close', () => {
    if (socket === websocket) restart = true;
  });
  socket.addEventListener('error', () => {
    if (socket === websocket) restart = true;
  });
  await new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('Local connector timed out')), 4000);
    socket.addEventListener('open', () => { clearTimeout(timer); resolve(); }, { once: true });
    socket.addEventListener('error', () => { clearTimeout(timer); reject(new Error('Local connector unavailable')); }, { once: true });
  });
  if (socket !== websocket || restart) throw new Error('Connection replaced');
  // The local pairing token never enters the relay exchange.
  socket.send(JSON.stringify({ ...hello, connector: 'secondlife', token: localToken }));
}

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => { stopping = true; closeSocket(); });
}
console.log('Second Life bridge running.');
while (!stopping) {
  if (restart) invalidate();
  const batch = outgoing.splice(0, 4);
  try {
    const response = await fetch(relay, {
      method: 'POST', redirect: 'error', signal: AbortSignal.timeout(5000),
      headers: { Authorization: `Bearer ${relayToken}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ instance, sequence: ++sequence, generation, messages: batch }),
    });
    if (!response.ok) throw new Error('Relay unavailable');
    const reader = response.body.getReader();
    const chunks = [];
    let bytes = 0;
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      bytes += value.byteLength;
      if (bytes > 12288) { await reader.cancel(); throw new Error('Oversized exchange'); }
      chunks.push(Buffer.from(value));
    }
    const exchange = JSON.parse(Buffer.concat(chunks).toString('utf8'));
    if (typeof exchange.generation !== 'string' || !exchange.generation ||
        !Array.isArray(exchange.messages) || exchange.messages.length > 4 ||
        typeof exchange.reset !== 'boolean' || typeof exchange.peer_online !== 'boolean') {
      throw new Error('Invalid exchange');
    }
    if (restart) continue;
    if (exchange.reset || generation !== exchange.generation || !exchange.peer_online) {
      closeSocket();
      outgoing = [];
      generation = exchange.generation;
      continue;
    }
    generation = exchange.generation;
    for (const frame of exchange.messages) {
      if (!frame || typeof frame !== 'object' || Array.isArray(frame) ||
          Buffer.byteLength(JSON.stringify(frame)) > 2048) throw new Error('Invalid frame');
      if (frame.type === 'hello') {
        if (frame.connector !== 'secondlife' || frame.protocol !== 1) throw new Error('Invalid hello');
        await openSocket(frame);
      } else if (websocket?.readyState === WebSocket.OPEN) {
        websocket.send(JSON.stringify(frame));
      }
    }
  } catch {
    // A failed exchange establishes a new generation; buffered intent is discarded.
    invalidate();
    if (!stopping) console.error('Connection interrupted; reconnecting from Off.');
  } finally {
    if (!stopping) await delay(2000);
  }
}
closeSocket();
