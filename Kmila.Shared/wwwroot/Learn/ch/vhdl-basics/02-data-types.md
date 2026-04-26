# 数据类型：`std_logic`、向量、`unsigned`、`integer`

VHDL 是一门强类型语言。当你看到一个端口被声明为 `in std_logic` 时，编译器不会让你把它接到不同类型的东西上。为每个信号选对类型，是写出整洁 VHDL 的一半功夫。

## `std_logic` —— 通用之线

IEEE 的**九值**单比特逻辑。对，九种：

| 值   | 含义 |
|------|------|
| `'0'` | 强驱低 |
| `'1'` | 强驱高 |
| `'Z'` | 高阻（三态关闭） |
| `'L'` | 弱低（下拉） |
| `'H'` | 弱高（上拉） |
| `'X'` | 未知 / 多驱动器冲突 |
| `'U'` | 未初始化 —— 你的信号从未被赋值 |
| `'W'` | 弱未知 |
| `'-'` | don't care（用于 case 语句） |

综合时只有 `'0'`、`'1'`、`'Z'` 会落实到硬件上。其他几种是仿真诊断 —— 如果你在波形上看到 `'U'`，说明你有一个信号在被写入之前就被读了。

## `std_logic_vector` —— 总线

固定位宽的 `std_logic` 数组：

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 位，索引 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 位，索引 0..15
```

`downto` 更常用 —— 它与我们书写数字的习惯一致（MSB 在左）。

字面量：

```vhdl
data <= "10101010";     -- 二进制字面量，每字符一位
data <= x"CAFE";        -- 十六进制字面量（必须是 4 位的倍数）
data <= (others => '0');-- 全零的简写
data <= (0 => '1', others => '0'); -- 只有 LSB 为高
```

## `unsigned` / `signed` —— 可以做数学的向量

`std_logic_vector` 只是"一袋位"。它没有数值含义。要加减，需要 `unsigned` 或 `signed`：

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- 可以
```

在它们之间转换：

```vhdl
-- 向量 → unsigned 以便做数学
u <= unsigned(slv);

-- unsigned → 向量以便对外输出
slv <= std_logic_vector(u);

-- unsigned → integer 以便索引或用作常量
idx <= to_integer(u);

-- integer → unsigned（需指定位宽）
u <= to_unsigned(42, 8);
```

经验法则：在**端口**上使用 `std_logic_vector`（受众最广），在 architecture 内实际做算术的地方转成 `unsigned` / `signed`。

## `integer` —— 用于索引和计数

对循环变量、常量以及不越过端口边界的东西，使用 `integer`（或受约束的子类型）：

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

无界的 `integer` 在仿真中通常是 32 位。对硬件而言，你要用受约束的范围：

```vhdl
signal counter : integer range 0 to 255;
```

这样综合器就恰好给你 8 位。

## 枚举 —— 用于 FSM 状态

为状态机声明一个可读的类型：

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

这样你就写 `state <= S_ACTIVE` 而不是 `state <= "010"`。综合器会为你挑选二进制编码（若你在意，也可以用属性给出提示）。

## 关键要点

- 单根线 → `std_logic`。总线 → `std_logic_vector`。
- 需要做数学？用 `unsigned`（或 `signed`）和 `IEEE.NUMERIC_STD`。
- 用 `std_logic_vector` 声明**端口**能让你的 entity 最具可移植性。
- FSM 状态用枚举 —— 代码读起来像规格说明。
