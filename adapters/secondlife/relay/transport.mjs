#!/usr/bin/env node
// HTTP transport bridge. Session policy remains in the connector and XRTranslate.
import { randomUUID } from 'node:crypto';
import bubble from '../../../presentation/javascript/bubble.js';
import { setTimeout as delay } from 'node:timers/promises';

const { layout, lifetime, sourceText, profiles, CaptionSource } = bubble;

export async function runBridge(config, { signal, onStatus = console.log } = {}) {
  const preset = config.preset || 'horizontal';
  if (!Object.hasOwn(profiles, preset)) throw new Error('Choose horizontal, vertical or board for XRT_CAPTION_PRESET.');

  // Preserve an unwrapped source alongside the native hover-text projection.
  function captionFrame(frame) {
    const text = sourceText(frame.text);
    const project = value => ({
      ...frame, text: value, display_text: layout(value, { profile: preset, maxBytes: 254 }).text,
      expires_ms: Math.floor(lifetime(frame)),
    });
    const fits = value => Buffer.byteLength(JSON.stringify(value)) <= 2048;
    const complete = project(text);
    if (fits(complete)) return complete;
    if (!fits(project('…'))) throw new Error('Caption metadata exceeds the relay frame limit.');
    let low = 0, high = Buffer.byteLength(text);
    while (low < high) {
      const middle = Math.ceil((low + high) / 2);
      if (fits(project(sourceText(text, middle) + '…'))) low = middle;
      else high = middle - 1;
    }
    return project(sourceText(text, low) + '…');
  }

  const source = new CaptionSource();
  function displayFrame(frame, now) {
    if (!source.accept(frame, now)) return null;
    const value = source.snapshot(now);
    const result = {
      type: 'display', session: frame.session, revision: value.revision,
      header: value.header, footer: value.footer,
      messages: value.messages.map(item => ({ id: item.id, text: item.text, ttl_ms: Math.floor(item.expiresAt - now) })),
    };
    // The relay envelope carries one bounded snapshot, including its identities.
    while (Buffer.byteLength(JSON.stringify(result)) > 8192) {
      const fields = [[result, 'header'], [result, 'footer'], ...result.messages.map(item => [item, 'text'])];
      fields.sort((a, b) => Buffer.byteLength(b[0][b[1]] || '') - Buffer.byteLength(a[0][a[1]] || ''));
      const [object, field] = fields[0];
      const text = object[field] || '';
      if (!text) throw new Error('Display metadata exceeds the relay frame limit.');
      object[field] = sourceText(text, Math.max(0, Buffer.byteLength(text) - 256));
    }
    return result;
  }
  function remainingFrame(frame, elapsed) {
    if (frame.type === 'caption') {
      const expires_ms = frame.expires_ms - elapsed;
      return expires_ms > 0 ? { ...frame, expires_ms } : null;
    }
    if (frame.type === 'display') return { ...frame, messages: frame.messages
      .filter(item => item.ttl_ms > elapsed).map(item => ({ ...item, ttl_ms: item.ttl_ms - elapsed })) };
    return frame;
  }

  const relay = new URL(config.relayUrl || 'https://invalid.example');
  const host = new URL(config.address || 'ws://127.0.0.1:0');
  const relayToken = config.relayToken || '';
  const localToken = config.token || '';
  if (!config.relayUrl || relay.protocol !== 'https:' || relay.username ||
      relay.password || relay.search || relay.hash || relay.pathname !== '/' ||
      host.protocol !== 'ws:' || !['localhost', '127.0.0.1', '[::1]'].includes(host.hostname) ||
      host.username || host.password || host.hash || !config.address ||
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
  async function openSocket(hello) {
    closeSocket();
    source.reset();
    const socket = new WebSocket(host);
    websocket = socket;
    socket.addEventListener('message', event => {
      if (socket !== websocket) return;
      try {
        if (typeof event.data !== 'string' || event.data.length > 32768) throw new Error();
        let frame = JSON.parse(event.data);
        if (!frame || typeof frame !== 'object' || Array.isArray(frame)) throw new Error();
        const receivedAt = performance.now();
        if (frame.type === 'state') status('Translation: ' + ({off:'Off', starting:'Starting', active:'On', stopping:'Stopping', error:'Check XRTranslate', unavailable:'Connecting'}[frame.status] || 'Connecting'));
        if (frame.type === 'display') {
          frame = displayFrame(frame, receivedAt);
          if (!frame) return;
          outgoing = outgoing.filter(item => !['caption', 'display'].includes(item.frame.type));
        } else if (frame.type === 'caption') {
          if (source.displayMode) return;
          frame = captionFrame(frame);
          outgoing = outgoing.filter(item => item.frame.type !== 'caption');
        } else if (frame.type === 'clear') {
          source.clear();
          outgoing = outgoing.filter(item => !['caption', 'display'].includes(item.frame.type));
        } else if (frame.type === 'reset' || frame.type === 'state' && frame.status !== 'active') {
          outgoing = outgoing.filter(item => item.frame.type !== 'caption');
        }
        if (Buffer.byteLength(JSON.stringify(frame)) > 8192 || outgoing.length >= 16) throw new Error();
        outgoing.push({ frame, receivedAt });
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
    if (socket !== websocket || restart || stopping) throw new Error('Connection replaced');
    // The local pairing token never enters the relay exchange.
    socket.send(JSON.stringify({ ...hello, connector: 'secondlife', token: localToken }));
  }

  const stop = () => { stopping = true; closeSocket(); };
  signal?.addEventListener('abort', stop, { once: true });
  if (signal?.aborted) stop();
  let lastStatus = '';
  function status(message) {
    if (lastStatus !== message) { lastStatus = message; onStatus(message); }
  }
  status('Waiting for your attachment. Wear it; touch it to pair on first use.');
  try {
    while (!stopping) {
      if (restart) invalidate();
      const now = performance.now();
      const batch = [];
      let payloadSize = 512;
      while (outgoing.length && batch.length < 4) {
        const { frame, receivedAt } = outgoing[0];
        const remaining = remainingFrame(frame, Math.max(0, Math.floor(now - receivedAt)));
        if (!remaining) { outgoing.shift(); continue; }
        const size = Buffer.byteLength(JSON.stringify(remaining)) + 1;
        if (payloadSize + size > 12288) break;
        outgoing.shift();
        payloadSize += size;
        batch.push(remaining);
      }
      try {
        const response = await fetch(relay, {
          method: 'POST', redirect: 'error', signal: signal
            ? AbortSignal.any([signal, AbortSignal.timeout(5000)]) : AbortSignal.timeout(5000),
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
        if (restart || stopping) continue;
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
        if (!stopping) status('Connection interrupted. Check the relay and XRTranslate; reconnecting from Off.');
      } finally {
        if (!stopping) await delay(2000, undefined, { signal }).catch(() => {});
      }
    }
  } finally {
    signal?.removeEventListener('abort', stop);
    closeSocket();
  }
}
