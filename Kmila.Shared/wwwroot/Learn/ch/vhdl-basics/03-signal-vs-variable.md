# signal 与 variable

这是 VHDL 里最容易被误解的一个区别。学生读到“variable 就像 C 里的局部变量”,就一路朝错误的方向狂奔。我们把它讲扎实,因为搞对这点,正是区分“能仿真的代码”和“真正变成你想要的硬件的代码”的分水岭。

## VHDL 里的两类名字

VHDL 有 **signal**(在 architecture 层用 `signal` 声明)和 **variable**(在 process 里用 `variable` 声明)。两者都能保存值,都能被赋值。它们**不可**互换。

| | signal | variable |
|---|---|---|
| 声明位置 | architecture(或 package) | process / 子程序内部 |
| 赋值运算符 | `<=` | `:=` |
| 何时生效 | 在仿真步的末尾 | 立刻 |
| 作用域 | 整个 architecture 共享 | 仅限其所在的 process |
| 建模的是 | 一根线或一个寄存器 | 一次临时计算 |

## 黄金规则

- **signal 赋值是被*调度*的;值在当前仿真步的末尾才出现。**
- **variable 赋值立刻发生,就在那一行所在的位置。**

就这么一句。signal 与 variable 的每一个“坑”,都只是这条规则的另一种说法。

## 演示

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- 调度:把 s 设为 1(还看不到)
      s <= 2;           -- 调度:把 s 设为 2(覆盖第一个)
      -- 这里 s 是多少?仍然是 0。两条赋值都还没生效。
    end if;
  end process;
end architecture demo;
```

时钟沿完成之后,`s` 变成 **2** —— 当一个 signal 在一次 process 执行中被调度多次,只有*最后*一条赋值胜出。在 process 内部,两条赋值之后,`s` 仍是 `0`,因为新值要等到 process 结束才传播。

现在换成 variable:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- variable 住在 process 里
  begin
    if rising_edge(clk) then
      v := 1;           -- 立刻生效
      v := 2;           -- 立刻生效
      -- 此刻,就在这一趟执行里,v 就是 2
    end if;
  end process;
end architecture demo;
```

注意两点:

- variable 用 `:=`;signal 用 `<=`。
- variable 只在单个 process 内有效 —— 你不能从另一个 process 或一条并发赋值里读到 `v`。

> 关于初值有个细微之处:`variable v : integer := 0;` 里的 `:= 0` 只在仿真开始时执行**一次**,不是每趟 process 都执行。除非你重新赋值,否则 variable 会在两趟之间保留它的值。这正是它能*记住*东西的原因,也是为什么在时钟进程里一个还没写就被读的 variable 会意外推断出一个寄存器。

## 为什么这对硬件重要

signal **建模一根线或一个触发器**。你不可能在同一瞬间把一根线的值改两次 —— 一个步内驱动这根线的所有东西,在步末归到唯一一个值。

variable **建模 process 内部的一次临时计算**。综合器通常把它实现成喂进(或流出)寄存器的组合逻辑 —— 或者干脆把它整个优化掉。

## 一个完整的例子:数出置位的比特

当你需要一个将在循环里不断更新的中间结果时,variable 大放异彩:

```vhdl
process(clk)
  variable ones : integer range 0 to 8;
begin
  if rising_edge(clk) then
    ones := 0;                      -- 每趟都把累加器清零
    for i in data'range loop
      if data(i) = '1' then
        ones := ones + 1;           -- 立刻更新,下一轮迭代就读得到
      end if;
    end loop;
    popcount <= ones;               -- 把最终计数发布到一个 signal 上
  end if;
end process;
```

把这里的 variable 换成 signal 试试,它就坏了:循环里的 `ones <= ones + 1` 会调度八条互相竞争的赋值,只有最后一条活下来,你永远只能得到 `0` 或 `1`。这个循环需要的是 variable 那种*立刻*生效的语义。

## 什么时候用哪个

- **signal** —— 默认。每个端口、每个寄存器、你将在波形查看器里看到的每条总线:都用 signal。
- **variable** —— 用于单个 process 内部的本地循环计数器或中间算术结果,当你想要 C 风格、写完立刻读得到的语义时。

## 一个常见陷阱

有些学生这样写:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- 寄存器 1
    b <= a;    -- 寄存器 2
  end if;
end process;
```

他们担心这不会交换 `a` 与 `b`。因为 signal 赋值是被调度的,它**确实**会交换:在时钟沿上,`a` 拿到 `b` 的**旧**值,`b` 拿到 `a` 的**旧**值,同时发生。这其实正是在一个周期内交换两个寄存器的惯用写法。

但换成 variable,语义就变了:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a 现在等于 b
    b := a;    -- b 拿到新的 a —— 所以 b 保持旧值,而 a 变了
  end if;
end process;
```

没有交换。两个最后都等于旧的 `b`。看起来一样的代码,结果相反 —— 全因为那条黄金规则。

## 要点

- `signal`:值被调度,在步末才出现;用 `<=` 赋值;建模线和寄存器;整个 architecture 共享。
- `variable`:立刻更新;用 `:=` 赋值;process 局部;建模中间计算。
- 一趟里多条 signal 赋值 → 最后一条胜出。多条 variable 赋值 → 全都按顺序执行。
- 拿不准 —— 用 signal。
