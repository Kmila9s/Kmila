# signal 与 variable

这是 VHDL 中最容易被误解的区别。学生看到 “variable 像 C 里的局部变量" 就一路跑偏。我们把它讲透。

## VHDL 里的两类名字

VHDL 有 **signal**(在 architecture 层用 `signal` 声明)和 **variable**(在 process 里用 `variable` 声明)。两者都能保存值,都能被赋值。它们**不可**互换。

## 黄金规则

- **signal 赋值发生在当前仿真 tick 的末尾。**
- **variable 赋值立刻发生,就在 process 内部。**

就这么一句。signal vs variable 的所有 “坑" 都不过是这条规则的复述。

## 演示

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- 请求:把 s 设为 1(还看不到)
      s <= 2;           -- 请求:把 s 设为 2(覆盖第一个)
      -- 这里 s 是多少?还是 0。两条赋值都还没生效。
    end if;
  end process;
end architecture demo;
```

时钟沿完成之后,`s` 变成 **2**(同一 tick 里多次赋值,只有最后一次 “胜出")。在 process 内部,两次赋值之后,`s` 仍是 `0` —— 因为新值要等到 process 结束才传播。

换成 variable:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- variable 在 process 里
  begin
    if rising_edge(clk) then
      v := 1;           -- 立刻生效
      v := 2;           -- 立刻生效
      -- 此刻、在这个 tick 里,v 就是 2
    end if;
  end process;
end architecture demo;
```

注意两点:

- variable 用 `:=`;signal 用 `<=`。
- variable 只在单个 process 内有效。

## 为什么对硬件重要

signal **建模一根线或一个触发器**。你不可能在同一瞬间把一根线的值改两次 —— 一个 tick 内驱动这根线的所有东西,在 tick 末尾归到一个值。

variable **建模一段 process 内部的临时计算**。综合器可以把它实现成组合电路,或者干脆优化掉。

## 何时用哪个

- **signal** —— 默认。你将在波形查看器里看到的每个端口、每个寄存器、每条总线:都是 signal。
- **variable** —— 单个 process 内部的本地循环计数器、中间算术结果,当你希望在那段 process 里得到 C 风格的语义。

## 一个常见陷阱

有些学生这样写:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- 寄存器 1
    b <= a;    -- 寄存器 2 —— “交换" a 与 b
  end if;
end process;
```

他们期望这能交换 `a` 与 `b`。因为 signal 赋值在 tick 末尾发生,所以**真的**会:在时钟沿上,`a` 拿到 `b` 的**旧**值,`b` 拿到 `a` 的**旧**值。这其实是单个周期内交换两个寄存器的惯用写法。

但如果你换成 variable,语义就变了:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a 现在等于 b
    b := a;    -- b 拿到新的 a —— 所以 b 保持旧值,a 改变
  end if;
end process;
```

没有交换。两个都等于旧的 `b`。

## 要记住的

- `signal`:tick 末尾更新。`<=` 赋值。建模线和寄存器。
- `variable`:立刻更新。`:=` 赋值。process 局部。建模中间计算。
- 拿不准 —— 用 signal。
