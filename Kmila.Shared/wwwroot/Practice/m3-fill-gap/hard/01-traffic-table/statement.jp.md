## Traffic-light table — fill the gap

⚠ 翻訳待ち — 説明は英語で表示されます。

A 3-state traffic-light controller is **almost** complete. The
state register, the asynchronous reset and the output assignment
`state <= cur;` are already wired for you.

Your task is to write the **next-state combinational CASE** that
selects `nxt` from `cur`.

### Specification

* The FSM cycles through three encoded states:
  * `00` → `01` → `10` → `00` → …
* Use a `case cur is … end case;` with explicit branches for
  `"00"`, `"01"`, `"10"` and a `when others =>` fallback that
  returns to `"00"`.
* The block is purely combinational — do **not** add a clock edge.

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded. Everything outside those markers is locked scaffolding.
