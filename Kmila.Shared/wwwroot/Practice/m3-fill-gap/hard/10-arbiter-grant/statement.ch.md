## 轮询仲裁器 — 授权轮转

⚠ 翻译未完成 — 描述以英文显示。

A 4-input round-robin arbiter tracks `last_grant`. On each clock it grants the next requester after `last_grant` in cyclic order. With no `req` bit set, no grant fires.

Implement the `case` that drives `nxt_grant` from `last_grant` and `req`. The state machine and `last_grant` register are wired outside the markers.

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are graded.
