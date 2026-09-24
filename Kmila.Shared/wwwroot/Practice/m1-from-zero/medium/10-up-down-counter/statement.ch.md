## 4 位加减计数器

实现一个带方向控制的 4 位计数器。时钟上升沿:

- 若 `dir = '1'`,加 1:`count <= count + 1`
- 若 `dir = '0'`,减 1:`count <= count - 1`

异步高有效的 `reset` 把计数清零为 `0000`。

### 提示

使用 `IEEE.NUMERIC_STD.ALL` 与一个 `unsigned(3 downto 0)` 内部信号。计数按模 16 回绕。
