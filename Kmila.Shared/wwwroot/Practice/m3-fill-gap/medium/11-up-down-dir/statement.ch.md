## 加/减计数器 — 填上方向控制

一个 4 位同步计数器**几乎**完成:复位、上升沿检测以及
`q <= std_logic_vector(count)` 都已就位。

请在上升沿实现方向控制:

* `dir = '1'` → `count ← count + 1`
* `dir = '0'` → `count ← count - 1`

### 规范

* 异步复位:`count ← 0000`。
* 计数器自由回绕(4 位无符号运算)。

### 在哪里写

只编辑 `-- BEGIN_EDIT` 与 `-- END_EDIT` 之间。复位与输出已锁定。

### 提示

简短的 `if dir = '1' then … else … end if;` 即可。
