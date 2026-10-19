# 并发赋值:`when/else` 与 `with/select`

VHDL 里的组合逻辑用并发语句来写 —— process 外面的每一行描述的都是一段永久接好的硬件。同一件事有两种语法味道:**条件**赋值(`when/else`)和**选择**赋值(`with/select`)。这些就是 HDL 单元里的“并发语句(sentencias concurrentes)”,它们是课程里每一个组合电路的骨架:加法器、码转换器、比较器、多路选择器、编码器、解码器。

## `when/else` —— 条件

适合你手里有一组带优先级的条件、或者每个分支测的是*不同*信号的场合:

```vhdl
y <= (others => '0') when reset = '1' else
     a               when sel = "00"  else
     b               when sel = "01"  else
     c               when sel = "10"  else
     d;
```

第一个满足的条件胜出。顺序很重要 —— 它编译成一个**带优先级的 MUX**:最顶上的条件到 `y` 的路径最直接。注意,所有的值表达式(`a`、`b`、`c`、`d`,以及那个全零聚合)都必须和 `y` 有**相同的类型和位宽**,而最后那个 `else` 没有条件 —— 它是包揽一切的兜底。

## `with/select` —— 选择

适合你在单个表达式上分支的场合(就像软件的 `switch`/`case` 那样):

```vhdl
with sel select
  y <= a   when "00",
       b   when "01",
       c   when "10",
       'Z' when others;
```

所有选项都拿同一个表达式(`sel`)来比较,各分支用**逗号**(不是 `else`)隔开,而 `when others` 是**必需的**,用来覆盖每一种可能的值。这种写法编译成一个**对称 MUX** —— 没有哪条分支享有特权。

### 为什么 `when others` 没得商量

这里的 `sel` 是一个 `std_logic_vector(1 downto 0)`。你会以为那就是四种情形 —— `"00"`、`"01"`、`"10"`、`"11"`。但每一位都是*九值*的 `std_logic`,所以编译器数出 9² = 81 种可能的组合(`"0X"`、`"ZU"`、…)。你没法把它们一一列举,所以需要 `when others` 来让这条语句变得完整。综合时那些元值会塌缩掉,你得到的就是你本来想要的那个干净的 4 路 MUX。

你也可以用竖线运算符把多个选项并在一起:

```vhdl
with opcode select
  result <= a + b    when "000" | "001",   -- 两个码,同一个输出
            a - b    when "010",
            (others => '0') when others;
```

## 你该挑哪个?

- **同一个信号、多种组合**(解码器、MUX、码转换器):`with/select` 读起来像一张真值表。用它。
- **不同信号、优先级逻辑**(reset 压倒 enable、enable 压倒 data):`when/else`。天然合适。
- **综合结果:** 在现代 FPGA 上两者产生的门大致相同,所以在源码层面挑更清晰的那种形式几乎总是正确的做法。只有当你真的*想要*优先级时才伸手去拿 `when/else`;否则 `with/select` 会向下一个读代码的人传达“这里没有优先级”。

## 锁存器陷阱(预告)

两种形式都要求 `y` 在**每一种**情形下最终都被赋值。一条并发的 `when/else` 或 `with/select` 其实*没法*让 `y` 不被赋值 —— 语法强迫你给出最后的 `else` / `when others`。但等价的 **process 里的 if/case** 可以留空,而*那*正是综合器推断出一个锁存器来“记住”上次的值的地方,这几乎绝不是你想要的。这和你会在时序模块的修复练习里正面撞上的,是同一个锁存器陷阱,所以养成这个习惯:在每一条路径上都给输出赋值。

## 一个小小的实战衔接:2 位比较器

课程里的幅度比较器直接从 `when/else` 掉出来:

```vhdl
-- gt/eq/lt 都是单个 std_logic 输出
gt <= '1' when unsigned(a) > unsigned(b) else '0';
eq <= '1' when a = b else '0';
lt <= '1' when unsigned(a) < unsigned(b) else '0';
```

三条独立的并发语句,三个比较器,全都同时活着。

## 接下来在哪儿出现

紧接这节课的 3-8 解码器示例里你会用到 `with/select`,而 `when/else` 会贯穿加法器、比较器和 MUX 的工作。两者都是 VHDL 的家常便饭。

## 要点

- `when/else` = 条件、带优先级、允许不同信号 → 带优先级的 MUX。
- `with/select` = 一个选择器、逗号分隔的选项、`when others` 必需 → 对称 MUX。
- 每一条值分支都必须匹配目标的类型和位宽。
- 并发形式没法漏掉输出,但基于 process 的 `if`/`case` 版本可以 —— 锁存器就是从那儿溜进来的。
