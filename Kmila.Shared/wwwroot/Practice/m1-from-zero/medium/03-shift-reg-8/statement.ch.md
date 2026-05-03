## 8 位左移寄存器

构建一个 8 位移位寄存器。在每个时钟上升沿,将内容左移一位,并将串行输入 `din` 写入第 0 位。异步高有效的 `reset` 清零寄存器。

### 规范

- 内部信号:`r : std_logic_vector(7 downto 0)`。
- 更新规则:`r <= r(6 downto 0) & din;`。
- 输出:`q <= r;`。

### 提示

使用 `process(clk, reset)`,其中 `if reset = '1' then ... elsif rising_edge(clk) then ...`。
