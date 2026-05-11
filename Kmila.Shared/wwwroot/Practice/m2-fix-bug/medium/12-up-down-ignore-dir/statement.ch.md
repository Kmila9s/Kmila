## Up/down counter, direction ignored

⚠ 翻译未完成 — 描述以英文显示。

A 4-bit up/down counter controlled by `dir`: `dir = '1'` counts up, `dir = '0'` counts down. Async high-active reset.

The starter ignores `dir` and always counts up.

**Hint:** add `if dir = '1' then count <= count + 1; else count <= count - 1; end if;` in the rising-edge branch.
