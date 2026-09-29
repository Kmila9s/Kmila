# 示例:4 位行波进位加法器

把两个二进制数相加是数字算术里的 “hello world”。这个示例构建一个 4 位、带进位输入和进位输出的加法器 —— 这正是你会在更大的 ALU 里、或在 CPU 的地址自增逻辑里平铺起来的那种单元。

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

发生了什么:

- `unsigned(a)` 和 `unsigned(b)` 把向量变成数值类型,这样能用 `+`。
- `resize(…, 5)` 扩到 5 位,让和能溢出到最高位。
- `("0000" & cin)` 构造一个只在 LSB 有 `cin` 的 5 位掩码。
- 加完之后,`t(3 downto 0)` 是和,`t(4)` 是进位输出。

## 为什么是 5 位?

两个 4 位数加一个进位输入,结果最大可以到 `1111 + 1111 + 1 = 11111` —— 五位。要是用 4 位容器算,最高位会被悄悄丢掉。resize 是安全的写法。

## 示例做了什么

仿真配置驱动 `a` 与 `b` 经过四种模式(`3+5 = 8`,然后一个会产生进位的 `10 + 6 = 16`,然后 `10 + 15 = 25`…)。波形显示 `sum` 与 `cout` 组合地变化 —— 不需要时钟,因为这个设计没有寄存器。

## 轮到你

- **参数化它**。给 entity 加一个 `generic (N : integer := 4)`,在所有出现 4 的地方用 `N`。现在你有了一个任意位宽的 `adder_N`。
- **构造一个减法器**。把 `b` 取反再加 `+1` —— 这就是二补码减法。除了每位一个 NOT,几乎不要额外硬件。
- **接两个**。一个负责低半字节、一个负责高半字节,你就得到了一颗 8 位、带行波进位的加法器。
