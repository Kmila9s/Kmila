## 大于 / 等于 / 小于比较器

比较两个 4 位无符号值 `a` 与 `b`,并驱动三个标志:

- `gt = 1` 当 `a > b`
- `eq = 1` 当 `a = b`
- `lt = 1` 当 `a < b`

任意时刻恰好有一个标志为 `1`。

### 提示

使用 `IEEE.NUMERIC_STD.ALL` 与并发条件赋值:

```vhdl
gt <= '1' when unsigned(a) > unsigned(b) else '0';
```
