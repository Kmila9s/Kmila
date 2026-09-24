## Round-robin arbiter — grant rotation

A 4-input round-robin arbiter tracks `last_grant` (the index of who got the bus most recently). On each clock, the arbiter grants the next requester after `last_grant` in cyclic order. If no `req` bit is set, no grant fires.

Implement the case statement that drives `nxt_grant` based on `last_grant` and `req`. The state machine and `last_grant` register are wired outside the markers.

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are graded.
