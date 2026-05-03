## Handshake — fix the metastable race

A simple `req`/`ack` handshake is failing intermittently. When the
upstream block raises `req` for a single cycle (a glitch caused by
clock-domain crossing or a noisy bus), `ack` follows immediately
and the downstream block falsely starts a transaction.

### Specification

* Synchronous, active-high reset.
* `ack` may only assert when `req` has been observed **stable
  high for at least one full clock cycle**.
* A one-cycle glitch on `req` must **never** produce `ack = '1'`.

### Hint

Two flip-flops in series form a tiny "stability filter": the output
becomes high only when both stages have already latched a `'1'`.
That is the canonical synchroniser pattern.
