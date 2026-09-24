## Up/down counter, direction ignored

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

A 4-bit up/down counter controlled by `dir`: `dir = '1'` counts up, `dir = '0'` counts down. Async high-active reset.

The starter ignores `dir` and always counts up.

**Hint:** add `if dir = '1' then count <= count + 1; else count <= count - 1; end if;` in the rising-edge branch.
