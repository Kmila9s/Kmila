# delta 周期与竞争条件

仿真器必须给 “同一时刻发生的两件事” 一个确定的顺序。VHDL 的回答是 **delta 周期**:一段无穷小的时间,用来把因和果分开。

## delta 周期是什么

仿真时间以真实步长前进(以 ns、µs、ms 为单位)。但在单个时间步内,变化可以按 **delta 周期** 的顺序发生:

- δ₀:你调度一次信号更新。
- δ₁:更新生效;任何对这个信号敏感的 process 现在有了新的输入。
- δ₂:这些 process 调度它们的更新。
- δ₃:那些更新生效。
- …

每一个 delta 都是 “零真实时间”,但仍代表一个因果步。仿真器在 delta 之间循环,直到没东西需要更新 —— 这时真实时间才前进。

## 为什么需要它

考虑两个在同一时钟沿都更新的信号:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

没有 delta 周期的话,仿真器只能任选一个顺序 —— `a` 在 `b` 之前更新?之后?同一瞬间?delta 周期绕开这个问题:两条赋值都在 δ₀ 排好,都在 δ₁ 生效。任何读 `a` 或 `b` 的 process 同时看到两个新值。

## 竞争条件陷阱

看这个:

```vhdl
process(a)
begin
  b <= a;
end process;

process(b)
begin
  c <= b;
end process;
```

`a` 变化时,第一个 process 触发并调度 `b`。在 δ₁,`b` 有了新值。第二个 process 触发,调度 `c`。在 δ₂,`c` 拿到正确的值。

现在把两个 process 调换。有什么变化?**没有**。delta 周期保证两条路径产出相同的最终结果 —— 即便 process 在文件中的“顺序”不同。这正是 delta 周期存在的原因。

## 哪里会出问题

问题出在你把 **signal** 语义和 “我需要在同一 tick 里这件事先于那件事” 混在一起。例子:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a 在 tick 结束时更新
    b <= a + 1;     -- b 读到的是旧的 a
  end if;
end process;
```

如果你以为 “第 1 行先跑,所以第 2 行运行时 `a` 已经更新好了”,那你错了。`a` 在 tick 结束时才更新。`b` 读到的是旧的 `a`。结果:两者都在旧数据上加 1,而不是基于对方的新数据。

把 signal 换成 variable,就拿到 C 风格的语义:

```vhdl
process(clk)
  variable aa, bb : integer;
begin
  if rising_edge(clk) then
    aa := bb + 1;   -- 立刻
    bb := aa + 1;   -- 看到新的 aa
  end if;
end process;
```

现在它就按软件程序员预期的方式工作了。

## 要点

- delta 周期是仿真器让并发逻辑变得确定的方式。
- signal 赋值在 delta 周期**之后**生效;variable 不会。
- 如果你的测试台在时钟沿附近表现 “怪”,先怀疑 delta 顺序问题,再怪工具。

课程下一道练习里有一个小的竞争条件实例。找到它、命名它、修复它 —— 你以后绝不会再忘这条规则。
