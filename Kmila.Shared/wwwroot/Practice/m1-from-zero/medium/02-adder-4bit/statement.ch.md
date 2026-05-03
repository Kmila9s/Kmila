## 4 位加法器

实现一个带输入进位 `cin` 的 4 位加法器。计算 `sum = a + b + cin` 与输出进位 `cout`。

### 规范

- 输入:`a`、`b` 为 4 位 `STD_LOGIC_VECTOR`,`cin` 为 1 位。
- 输出:`sum` 4 位,`cout` 1 位。

### 提示

使用 `IEEE.NUMERIC_STD.ALL` 与一个 5 位 `unsigned` 临时信号:

```vhdl
signal s : unsigned(4 downto 0);
s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
sum  <= std_logic_vector(s(3 downto 0));
cout <= s(4);
```
