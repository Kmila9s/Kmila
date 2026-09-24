## 4 位同步计数器

构建一个 4 位计数器,每个时钟上升沿加 1。异步高有效的 `reset` 将计数清零为 `0000`。

### 规范

- 内部信号:`count : unsigned(3 downto 0) := (others => '0')`。
- 在 `rising_edge(clk)`:`count <= count + 1;`。
- 输出:`q <= std_logic_vector(count);`。

### 提示

引入 `IEEE.NUMERIC_STD.ALL` 后才能对 `unsigned` 执行 `+ 1`。
