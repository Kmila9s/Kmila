# 数据类型:`std_logic`、向量、`unsigned`、`integer`

VHDL 是一门**强类型**语言。当你看到一个端口声明成 `in std_logic`,编译器就不允许你在不做显式转换的情况下,把它接到另一种类型的东西上。为每个信号挑对类型,就等于写好 VHDL 的一半。

## `std_logic` —— 万能导线

IEEE 1164 **九值**逻辑里的一个比特。没错,九个:

| 值 | 含义 |
|-------|---------|
| `'0'` | 强驱动低 |
| `'1'` | 强驱动高 |
| `'Z'` | 高阻(tristate 关断) |
| `'L'` | 弱低(下拉) |
| `'H'` | 弱高(上拉) |
| `'X'` | 强未知 —— 例如两个驱动源以不同的值互相较劲 |
| `'U'` | 未初始化 —— 你的信号从没被赋过值 |
| `'W'` | 弱未知 |
| `'-'` | 不关心(用作 `std_match` 匹配模式 / 综合提示) |

为什么是九个而不是两个?因为真实的导线不只是“0 或 1”。它可以悬空(`'Z'`)、被弱弱地拉住(`'L'`/`'H'`),或者处于真正的冲突中(`'X'`)。正是这一点让 `std_logic` 成为一个**可解析(resolved)**类型:当多个驱动源共享一根导线时,由一个解析函数来决定结果。强值压倒弱值;两个互相矛盾的强值得到 `'X'`;没被驱动的导线停在 `'Z'`。

综合时只有 `'0'`、`'1'` 和 `'Z'` 能进到硬件里。其余的都是仿真诊断用的 —— 如果你在波形里看到 `'U'`,说明有个信号在被写之前就被读了;一条 `'X'` 的条纹通常意味着一根总线上有两个活动的驱动源。

> 它还有个不可解析的表亲 `std_ulogic`(`u` 是“unresolved”,不可解析)。`std_logic` 字面上就是 `std_ulogic` 加上一个解析函数。默认用 `std_logic` —— 对总线来说,那个解析正是你想要的。

## `std_logic_vector` —— 总线

一个定宽的 `std_logic` 数组:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 位,下标 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 位,下标 0..15
```

`downto` 是远远更常见的方向 —— 它和我们写数字的习惯一致(MSB 在左,所以 `data(7)` 是最高位)。每个项目挑一个方向并坚持用它;混用 `to` 和 `downto` 是导致总线被悄悄反转的经典根源。

字面量:

```vhdl
data <= "10101010";      -- 二进制字面量,一个字符一个比特
addr <= x"CAFE";         -- 十六进制字面量(位宽必须是 4 的倍数)
data <= (others => '0'); -- 聚合:全零,任意位宽
data <= (0 => '1', others => '0'); -- 只有第 0 位(LSB)为高
```

`(others => '0')` 这种写法值得你喜欢:以后你改了位宽它也会自动适配,所以永远不会过时。

## `unsigned` / `signed` —— 可以做数学运算的向量

`std_logic_vector` 只是“一袋比特”。它没有数值含义,所以在 `numeric_std` 下 `slv1 + slv2` 会是编译错误。要做加减,你需要 `unsigned` 或 `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- 可行:同宽 unsigned 相加
```

这两种类型的唯一区别在于最高位怎么读:`unsigned` 把所有位都当成数值大小;`signed` 把 MSB 当成二进制补码的符号位。

在它们之间转换 —— 下面这四种转换你会不停地用到:

```vhdl
-- 把同一组比特重新当成 unsigned(用来做数学)
u   <= unsigned(slv);

-- 把 unsigned 重新当成普通比特(给外部世界 / 端口用)
slv <= std_logic_vector(u);

-- unsigned -> integer,用于索引或常量
idx <= to_integer(u);

-- integer -> unsigned;你必须给出位宽
u   <= to_unsigned(42, 8);
```

`unsigned(slv)` 和 `std_logic_vector(u)` 是**类型转换(cast)** —— 它们改的是比特上的标签,不是比特本身。`to_integer` 和 `to_unsigned` 是真正做计算的**函数**。新手常犯的错是对一个向量调用 `to_unsigned`(它接收的是整数),或者用 `unsigned(…)` 去 cast 一个整数(cast 只在数组类型之间起作用)。

还有一个能帮你免掉溢出 bug 的 —— `resize`,它把值补齐或截断到新的位宽。它对 `unsigned` 做**零扩展**、对 `signed` 做**符号扩展**,所以在算术之前它保留了数值:

```vhdl
wide <= resize(a, 9) + resize(b, 9);  -- 9 位结果保住进位
```

经验法则:在**端口**里用 `std_logic_vector`(受众最广),在真正做算术的 architecture 内部再转成 `unsigned` / `signed`。

## `integer` —— 用于索引和计数

循环计数器、常量,以及任何不会跨越端口边界的东西,都用 `integer`(或一个带约束的子类型):

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

无约束的 `integer` 在仿真里至少是 32 位(范围 −2³¹+1 到 2³¹−1)。做硬件时你几乎总想要一个带约束的范围,好让综合器给寄存器定好大小:

```vhdl
signal counter : integer range 0 to 255;  -- 综合出正好 8 位
```

一个无约束的 `integer` 计数器会让你白白搭上 32 个你并不想要的触发器。

## 枚举 —— 用于 FSM 状态

给状态机声明一个可读的类型:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

这样你就写 `state <= S_ACTIVE` 而不是 `state <= "010"`。二进制(或 one-hot)编码由综合器替你挑,如果你在意还可以用属性给它提示。你的代码读起来像一份规格说明,波形里显示的是状态*名字*,而不是原始比特。

## 要点

- 单根导线 → `std_logic`。总线 → `std_logic_vector`。两者都来自 `STD_LOGIC_1164`。
- 需要做数学?用 `unsigned`(或 `signed`)和 `IEEE.NUMERIC_STD`。`std_logic_vector` 没有数值含义。
- `unsigned(x)` / `std_logic_vector(x)` 是 cast(给比特重新贴标签);`to_integer` / `to_unsigned` 是函数(而且 `to_unsigned` 需要一个位宽)。
- 把**端口**声明成 `std_logic_vector` 让你的 entity 的可移植性最强;在内部再转换。
- 给你的 `integer` 加上范围约束,用枚举来表示 FSM 状态。
