## 按键去抖

⚠ 翻译未完成 — 描述以英文显示。

## Button debouncer

Filter a noisy button input. The output `clean` only flips after the input `raw` has been stable for **N = 4** consecutive clock cycles.

### Spec

- Track the last sampled value of `raw` and a 2-bit counter.
- Each clock: if `raw == last`, increment the counter; when it saturates at `"11"`, copy `raw` to the held output.
- If `raw` differs, reset the counter and update `last`.
- Reset clears all internal state and `clean` to `'0'`.

### Hint

Use `unsigned(1 downto 0)` for the counter and a single `STD_LOGIC` to hold the stable level.
