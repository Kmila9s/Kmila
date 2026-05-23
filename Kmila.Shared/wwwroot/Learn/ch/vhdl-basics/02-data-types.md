# 数据类型:`std_logic`、向量、`unsigned`、`integer`

VHDL 是强类型语言。一个端口被声明为 `in std_logic` 时,编译器不会让你把它接到另一种类型的东西上。给每个信号挑对的类型,等于写干净 VHDL 的一半工作。

## `std_logic` —— 万能线

IEEE **9 值**逻辑的一个位。是的,九个:

| 值     | 含义 |
|--------|------|
| `'0'`  | 强驱动低 |
| `'1'`  | 强驱动高 |
| `'Z'`  | 高阻(三态关) |
| `'L'`  | 弱低(下拉) |
| `'H'`  | 弱高(上拉) |
| `'X'`  | 未知 / 多个驱动者冲突 |
| `'U'`  | 未初始化 —— 你的信号没被赋值过 |
| `'W'`  | 弱未知 |
| `'-'`  | 不关心(在 case 里用) |

综合时只有 `'0'`、`'1'`、`'Z'` 会进硬件。其余是仿真诊断 —— 在波形上看到 `'U'`,意味着一个信号在被赋值之前就被读了。

## `std_logic_vector` —— 总线

一段宽度固定的 `std_logic` 数组:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 位,索引 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 位,索引 0..15
```

`downto` 是远比 `to` 更常见的方向 —— 它和我们写数字的方式一致(MSB 在左)。

字面量:

```vhdl
data <= "10101010";     -- 二进制字面量,每位一个字符
data <= x"AA";          -- 十六进制字面量(必须是 4 位的倍数)
data <= (others => '0');-- 全零的简写
data <= (0 => '1', others => '0'); -- 只有 LSB 为高
```

## `unsigned` / `signed` —— 可以做算术的向量

`std_logic_vector` 只是 “一袋位",没有数值含义。要做加减,得用 `unsigned` 或 `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- 可以
```

互相转换:

```vhdl
-- 向量 → unsigned 做算术
u <= unsigned(slv);

-- unsigned → 向量给外面
slv <= std_logic_vector(u);

-- unsigned → integer 用于索引或常量
idx <= to_integer(u);

-- integer → unsigned(指定位宽)
u <= to_unsigned(42, 8);
```

经验法则:**端口**里用 `std_logic_vector`(受众最广),在 architecture 里需要算术时再转 `unsigned` / `signed`。

## `integer` —— 用于索引和计数

循环计数器、常量、不跨端口边界的东西,用 `integer`(或带约束的子类型):

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

无约束的 `integer` 在仿真里通常是 32 位。对硬件,你要的是有约束的范围:

```vhdl
signal counter : integer range 0 to 255;
```

综合器就给你正好 8 位。

## 枚举 —— FSM 状态用

给状态机声明一个可读类型:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

然后写 `state <= S_ACTIVE` 而不是 `state <= "010"`。综合器替你挑二进制编码(如果你在意,可以用 attribute 提示)。

## 要记住的

- 单根线 → `std_logic`。总线 → `std_logic_vector`。
- 需要做算术?用 `unsigned`(或 `signed`)加 `IEEE.NUMERIC_STD`。
- **端口**用 `std_logic_vector` 声明能让你的 entity 最大限度地可移植。
- FSM 状态用枚举 —— 代码读起来像规格说明。
