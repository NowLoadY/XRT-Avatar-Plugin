import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import Connector from '../connectors/websocket/javascript/xrt_connector.js';
import { parseRelayInfo } from '../adapters/secondlife/relay/launcher.mjs';
import { runBridge } from '../adapters/secondlife/relay/transport.mjs';

test('environment-only launch survives stdin EOF and stops on SIGTERM', () => {
  const launcher = new URL('../adapters/secondlife/relay/launcher.mjs', import.meta.url).href;
  const result = spawnSync(process.execPath, ['--input-type=module', '-e', `
    import assert from 'node:assert/strict';
    import { main } from ${JSON.stringify(launcher)};
    const ended = new Promise(resolve => process.stdin.once('end', resolve));
    let stoppedOnEof;
    globalThis.fetch = async (_url, options) => {
      await ended;
      await new Promise(setImmediate);
      stoppedOnEof = options.signal.aborted;
      process.emit('SIGTERM');
      throw new DOMException('Stopped', 'AbortError');
    };
    await main();
    assert.equal(stoppedOnEof, false);
  `], { encoding: 'utf8', timeout: 3000, stdio: ['ignore', 'pipe', 'pipe'], env: {
    ...process.env, XRT_RELAY_URL: 'https://relay.example', XRT_RELAY_HOST_TOKEN: 'h'.repeat(64),
    XRT_CONNECTOR_URL: 'ws://localhost:43821/xrt', XRT_CONNECTOR_TOKEN: 'l'.repeat(64),
  } });
  assert.ifError(result.error);
  assert.equal(result.status, 0, result.stderr);
});

test('clipboard info accepts only the selected loopback connector', () => {
  const value = { protocol: 1, connector: 'secondlife', address: 'ws://127.0.0.1:43821/xrt', token: 'x'.repeat(64) };
  for (const host of ['127.0.0.1', 'localhost', '[::1]']) {
    const valid = { ...value, address: `ws://${host}:43821/xrt` };
    assert.deepEqual(Connector.parsePairing(JSON.stringify(valid), 'secondlife'), { address: valid.address, token: valid.token });
  }
  const invalid = [null, [], { ...value, protocol: true }, { ...value, connector: 'overte' },
    { ...value, token: 'short' }, { ...value, token: 'x'.repeat(63) + '\n' },
    ...['ws://example.org:43821/xrt', 'wss://127.0.0.1:43821/xrt', 'ws://127.0.0.1:0/xrt',
      'ws://localhost:65536/xrt', 'ws://user@localhost:43821/xrt', 'ws://localhost:43821/xrt?token=x']
      .map(address => ({ ...value, address }))];
  for (const candidate of invalid) assert.throws(() => Connector.parsePairing(JSON.stringify(candidate), 'secondlife'));
});

test('relay pairing rejects client credentials and non-origin URLs', () => {
  const value = { role: 'host', url: 'https://relay.example', token: 'h'.repeat(64) };
  assert.deepEqual(parseRelayInfo(JSON.stringify(value)), { relayUrl: value.url, relayToken: value.token });
  for (const change of [{ role: 'client' }, { url: 'http://relay.example' }, { url: 'https://user@relay.example' },
    { url: 'https://relay.example/path' }, { url: 'https://relay.example?token=x' }, { token: 'short' }]) {
    assert.throws(() => parseRelayInfo(JSON.stringify({ ...value, ...change })));
  }
});

test('stopping the bridge aborts its outstanding exchange', async t => {
  const cancellation = new AbortController();
  let contacted = false;
  t.mock.method(globalThis, 'fetch', async (_url, options) => {
    contacted = true;
    queueMicrotask(() => cancellation.abort());
    return await new Promise((_resolve, reject) => options.signal.addEventListener('abort',
      () => reject(new DOMException('Stopped', 'AbortError')), { once: true }));
  });
  await runBridge({ relayUrl: 'https://relay.example', relayToken: 'h'.repeat(64),
    address: 'ws://localhost:43821/xrt', token: 'l'.repeat(64) }, { signal: cancellation.signal, onStatus() {} });
  assert.equal(contacted, true);
});

test('pairing stays on the local connection and stopping closes it', { timeout: 7000 }, async t => {
  const cancellation = new AbortController();
  const localToken = 'l'.repeat(64);
  let socket;
  let localHello;
  class LocalSocket extends EventTarget {
    static OPEN = 1;
    constructor() {
      super(); socket = this; this.readyState = 1;
      setImmediate(() => this.dispatchEvent(new Event('open')));
    }
    send(text) { localHello = JSON.parse(text); cancellation.abort(); }
    close() { this.readyState = 3; }
  }
  const originalWebSocket = globalThis.WebSocket;
  globalThis.WebSocket = LocalSocket;
  t.after(() => { globalThis.WebSocket = originalWebSocket; });
  t.mock.method(globalThis, 'fetch', async (_url, options) => {
    assert.equal(options.body.includes(localToken), false);
    const body = JSON.parse(options.body);
    return new Response(JSON.stringify({ generation: 'current', reset: !body.generation, peer_online: true,
      messages: body.generation ? [{ type: 'hello', connector: 'secondlife', protocol: 1, token: '',
        client_id: 'attachment', capabilities: [] }] : [] }));
  });
  await runBridge({ relayUrl: 'https://relay.example', relayToken: 'h'.repeat(64),
    address: 'ws://localhost:43821/xrt', token: localToken }, { signal: cancellation.signal, onStatus() {} });
  assert.equal(localHello.token, localToken);
  assert.equal(localHello.connector, 'secondlife');
  assert.equal(socket.readyState, 3);
});
