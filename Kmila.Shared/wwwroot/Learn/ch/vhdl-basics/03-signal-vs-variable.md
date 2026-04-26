# signal 与 variable

这是 VHDL 中被误解得最深的一组区分。学生们读到"variable 就像 C 里的局部变量"，然后朝错误的方向狂奔。让我们把它落到地上。

## VHDL 中两种命名

VHDL 有 **signal**（在 architecture 级用 `signal` 声明）和 **variable**（在进程内用 `variable` 声明）。两者都能持有值，两者都能被赋值。但它们**不可**互换。

## 黄金法则

- **signal 的赋值在当前仿真 tick 结束时发生。**
- **variable 的赋值在进程内立即发生。**

就这样。所有关于 signal 与 variable 的"坑"都是这条规则的不同说法。

## 演示

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- 请求：把 s 设为 1（还看不见）
      s <= 2;           -- 请求：把 s 设为 2（覆盖前一次）
      -- 此处 s 是多少？仍为 0。两次赋值都未生效。
    end if;
  end process;
end architecture demo;
```

时钟沿结束后，`s` 变为 **2**（同一 tick 内多次赋值时，只有最后一次"胜出"）。进程内部，两次赋值之后 `s` 仍为 `0` —— 因为新值要到进程结束才会传播。

现在换成 variable：

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- variable 生存在进程内
  begin
    if rising_edge(clk) then
      v := 1;           -- 立即生效
      v := 2;           -- 立即生效
      -- 此时此刻，v 就是 2
    end if;
  end process;
end architecture demo;
```

注意两点：

- variable 用 `:=`；signal 用 `<=`。
- variable 是单个进程的局部变量。

## 为什么这对硬件重要

一个 signal **建模一根线或一个触发器**。你不能在同一瞬间把一根线的值改两次 —— 在一个 tick 内无论谁驱动它，最终都会在 tick 结束时稳定到某一个值。

一个 variable **建模进程内的临时计算**。综合器可以把它实现为组合逻辑，也可能把它完全优化掉。

## 什么时候用哪个

- **signal** —— 默认。每个端口、每个寄存器、每条你想在波形查看器中看到的总线：signal。
- **variable** —— 进程内的本地循环计数或中间算术结果，当你在那个进程内想要 C 风格语义时。

## 一个常见陷阱

有些学生写：

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- 寄存器 1
    b <= a;    -- 寄存器 2 —— "交换" a 和 b
  end if;
end process;
```

他们期望这段交换 `a` 和 `b`。因为 signal 赋值在 tick 结束时才发生，它**确实**做到了：在边沿上，`a` 拿到 `b` 的**旧**值，而 `b` 拿到 `a` 的**旧**值。这正是用一个周期交换两个寄存器的惯用写法。

但如果你换成 variable，语义就变了：

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a 现在等于 b
    b := a;    -- b 得到新的 a —— 因此 b 保持旧值，a 变了
  end if;
end process;
```

没有交换，两者都变成了旧的 `b`。

## 关键要点

- `signal`：在 tick 末更新。用 `<=` 赋值。建模线与寄存器。
- `variable`：立即更新。用 `:=` 赋值。进程局部。建模中间计算。
- 拿不准 —— 就用 signal。
