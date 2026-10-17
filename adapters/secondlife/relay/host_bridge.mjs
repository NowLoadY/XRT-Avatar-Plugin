#!/usr/bin/env node
import { main } from './launcher.mjs';

main().catch(error => {
  if (error.name !== 'AbortError') {
    // Endpoint exceptions can contain credentials.
    console.error('Connector stopped. Check your connection info and try again.');
    process.exitCode = 1;
  }
});
