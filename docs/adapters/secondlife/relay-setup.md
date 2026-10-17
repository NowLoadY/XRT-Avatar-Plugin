# Second Life relay setup

This guide is for the person running the relay. Complete it before following the
[attachment setup](installation.md).

## Requirements

Use a server reachable from both Second Life and the player's computer. It needs
Python 3.10 or later and an HTTPS reverse proxy with a valid public certificate.
The relay itself listens on `127.0.0.1:8788` by default.

The implementation uses Python's standard library. Copy
[`server.py`](../../../adapters/secondlife/relay/server.py) to the server, or use
that file from an extracted source release.

## Configure a pair

Generate two independent tokens by running this command twice:

```sh
python3 -c 'import secrets; print(secrets.token_urlsafe(32))'
```

Set the resulting values in the service environment:

```sh
export XRT_RELAY_CLIENT_TOKEN='generated-client-token'
export XRT_RELAY_HOST_TOKEN='different-generated-host-token'
python3 adapters/secondlife/relay/server.py
```

If `server.py` was copied on its own, run `python3 server.py` instead. Each token
must contain 32–512 printable ASCII characters without spaces. Use at most 128
characters for the client token so its pairing text fits the attachment. The client token goes into the LSL
attachment; the host token goes into the player's local Node.js bridge. Use a
separate service process, port and token pair for each player.

## Configure HTTPS

Forward the public HTTPS origin to `http://127.0.0.1:8788`, preserving request paths
and the `Authorization` header. The service exposes:

- `/v1/client/exchange` for the attachment.
- `/v1/host/exchange` for the local bridge.

Set the proxy request-body limit to at most 16 KiB, and disable request-body and
authorization logging. Keep the Python listener private behind the proxy. The
player's XRTranslate port stays local to their computer.

Generate the two private clipboard values for the player with:

```sh
python3 adapters/secondlife/relay/server.py --connection-info https://relay.example.net
```

This explicit command prints credentials; run it privately, not in service logs.
Give the **client** JSON to the attachment and the **host** JSON to the local
connector. The client value must fit the native 250-byte textbox. The player
never needs an owner UUID or environment-variable commands. The
attachment verifies the HTTPS certificate using Second Life's
[`llHTTPRequest`](https://wiki.secondlife.com/wiki/LlHTTPRequest) API.

## Operation

The service retains queues only in memory. Exchanges are limited to 12288 bytes
and four frames; a display snapshot is limited to 8192 bytes. It keeps the latest
snapshot and deducts buffering time from each message's remaining lifetime.
Stopping the service clears pending data.
Restarting the service or losing a peer resets the connection; the player must
choose Start again. Retire both tokens when the pairing is removed.

Use the [developer checks](development.md#in-world-checks) to confirm the
attachment, local bridge and relay work together. The
[platform architecture](README.md) owns protocol and lifecycle behavior.
