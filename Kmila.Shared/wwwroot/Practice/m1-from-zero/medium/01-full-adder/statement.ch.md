## 全加器

实现一个 1 位全加器。它将三个比特 `a`、`b` 和输入进位 `cin` 相加,得到和 `sum` 与输出进位 `cout`。

### 公式

- `sum  = a XOR b XOR cin`
- `cout = (a AND b) OR (cin AND (a XOR b))`

### 提示

三个输入的异或得到和。当至少两个输入为 `1` 时产生输出进位。
