## 时钟分频器 — 填上翻转

一个 4 位时钟分频器**几乎**完成:模 10 计数器(`TOP = 9`)、复位
和输出 `clk_out <= clk_reg` 都已就位。计数器到达 `TOP` 时会被清零。

你需要描述在计数器溢出**同一时刻**对 `clk_reg` 做什么:它应该翻转。
其余时刻 `clk_reg` 保持不变。

### 规范

* 异步复位:`count ← 0`、`clk_reg ← '0'`。
* 在 `rising_edge(clk)`:
  * 若 `count = TOP` → `count ← 0`、`clk_reg ← not clk_reg`
  * 否则 → `count ← count + 1`

### 在哪里写

只编辑 `-- BEGIN_EDIT` 与 `-- END_EDIT` 之间。计数器、复位和输出
已锁定。

### 提示

`clk_reg <= not clk_reg;`
