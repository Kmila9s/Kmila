# 示例：4 位行波进位加法器

两个二进制数相加是数字算术的"hello world"。本示例构建一个带进位输入和进位输出的 4 位加法器 —— 正是你会堆叠到更大 ALU 内部、或用在 CPU 地址递增逻辑里的那个单元。

## 接口

```
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## 主体

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

细节解读：

- `unsigned(a)` 和 `unsigned(b)` 把向量转换为可用于 `+` 运算符的数值类型。
- `resize(…, 5)` 把它们扩展为 5 位，以便求和时能溢出到最高位。
- `("0000" & cin)` 构造一个仅在最低位为 `cin` 的 5 位掩码。
- 加法之后，`t(3 downto 0)` 就是和，`t(4)` 就是进位输出。

## 为什么是 5 位？

两个 4 位数再加上一个进位输入，和最大可达 `1111 + 1111 + 1 = 11111` —— 五位。如果只用 4 位容器，你会悄无声息地丢掉最高位。resize 是安全的惯用写法。

## 这个示例做了什么

仿真配置驱动 `a` 和 `b` 经过四组数据（`3+5 = 8`，然后一个产生进位的 `10 + 6 = 16`，再 `10 + 15 = 25`……）。波形上会看到 `sum` 和 `cout` 随组合逻辑变化 —— 不需要时钟，因为这个设计里没有寄存器。

## 轮到你了

- **让它参数化。** 给 entity 加 `generic (N : integer := 4)`，用 `N` 替换所有出现的 4。现在你就有了一个可在任意位宽下实例化的 `adder_N`。
- **造一个减法器。** 把 `b` 取反再多加 1 —— 这就是二进制补码减法。除了每位一个 NOT 门，不需要额外硬件。
- **把两个接起来。** 低 4 位用一个，高 4 位用另一个，它们之间做行波进位，你就得到了一个 8 位加法器。
