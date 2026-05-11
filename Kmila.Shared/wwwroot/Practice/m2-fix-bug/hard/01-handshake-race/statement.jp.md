## Handshake race fix

⚠ 翻訳待ち — 説明は英語で表示されます。

A simple `req`/`ack` handshake fails intermittently. When the upstream
block raises `req` for a single cycle (a glitch from a clock-domain
crossing or a noisy bus), `ack` follows immediately and the
downstream block falsely starts a transaction.

### Specification

* Synchronous, active-high reset.
* `ack` may only assert when `req` has been observed **stable high
  for at least one full clock cycle**.
* A one-cycle glitch on `req` must **never** produce `ack = '1'`.

### Hint

Two flip-flops in series form a small "stability filter" — the output
becomes high only when both stages have latched a `'1'`. That is the
canonical synchroniser pattern.
