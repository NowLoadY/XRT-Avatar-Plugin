#!/usr/bin/env node
import { createInterface } from 'node:readline/promises';
import { Writable } from 'node:stream';
import Connector from '../../../connectors/websocket/javascript/xrt_connector.js';
import { runBridge } from './transport.mjs';

export function parseRelayInfo(text) {
  try {
    if (typeof text !== 'string' || text.length > 4096) throw new Error();
    const value = JSON.parse(text);
    const url = new URL(value.url);
    if (value.role !== 'host' || url.protocol !== 'https:' || url.username || url.password ||
        url.pathname !== '/' || url.search || url.hash ||
        typeof value.token !== 'string' || !/^[\x21-\x7e]{32,512}$/.test(value.token)) throw new Error();
    return { relayUrl: url.origin, relayToken: value.token };
  } catch { throw new Error('Paste the host connection info provided by your relay operator.'); }
}

export async function main() {
  if (process.argv.includes('--check')) {
    console.log('Second Life connector resources ready (Node.js only)');
    return;
  }
  const cancellation = new AbortController();
  const stop = () => cancellation.abort();
  process.on('SIGINT', stop);
  process.on('SIGTERM', stop);
  const muted = new Writable({ write(_chunk, _encoding, done) { done(); } });
  const input = createInterface({ input: process.stdin, output: muted, terminal: !!process.stdin.isTTY });
  input.on('SIGINT', stop);
  const needsInput = ![process.env.XRT_RELAY_URL, process.env.XRT_RELAY_HOST_TOKEN,
    process.env.XRT_CONNECTOR_URL, process.env.XRT_CONNECTOR_TOKEN].every(Boolean);
  if (process.stdin.isTTY || needsInput) input.once('close', stop);
  async function ask(prompt, parse) {
    while (!cancellation.signal.aborted) {
      process.stdout.write(prompt + ' (input hidden)\n> ');
      const answer = await input.question('', { signal: cancellation.signal });
      process.stdout.write('\n');
      try { return parse(answer.trim()); }
      catch (error) { console.log(error.message); }
    }
    throw new Error('Cancelled');
  }
  try {
    console.log('XRT · Second Life\nKeep this window open while playing. Ctrl+C disconnects.\n');
    const relay = process.env.XRT_RELAY_URL && process.env.XRT_RELAY_HOST_TOKEN
      ? parseRelayInfo(JSON.stringify({ role: 'host', url: process.env.XRT_RELAY_URL, token: process.env.XRT_RELAY_HOST_TOKEN }))
      : await ask('Paste your relay host connection info', parseRelayInfo);
    const pairing = process.env.XRT_CONNECTOR_URL && process.env.XRT_CONNECTOR_TOKEN
      ? Connector.parsePairing(JSON.stringify({ protocol: 1, connector: 'secondlife', address: process.env.XRT_CONNECTOR_URL, token: process.env.XRT_CONNECTOR_TOKEN }), 'secondlife')
      : await ask('In XRTranslate, select Second Life and Copy connection info. Paste it here', text => Connector.parsePairing(text, 'secondlife'));
    // Keep the terminal's Ctrl+C handler alive, but do not retain input history.
    input.history = [];
    await runBridge({ ...relay, ...pairing, preset: process.env.XRT_CAPTION_PRESET }, { signal: cancellation.signal });
  } finally {
    input.close();
    process.removeListener('SIGINT', stop);
    process.removeListener('SIGTERM', stop);
  }
}
