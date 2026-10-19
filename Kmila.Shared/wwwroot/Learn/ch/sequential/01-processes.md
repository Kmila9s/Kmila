# process 与敏感列表

VHDL 里 process 之外的一切都是并发的 —— 都是线和门。一旦你想让一段电路**记住**从这个时钟周期到下一个时钟周期之间的某个东西,你就伸手去拿 process。process 是 HDL 单元里“顺序语句(sentencias secuenciales)”所在的地方,也是从组合逻辑(第三单元)通往存储元件(第四单元)的桥梁。

## process 是什么

`process` 是 VHDL architecture 内部的一段顺序代码。它的语句自上而下读,跟软件一样。但 process 本身只在它的**敏感列表**里有东西变化时才“跑” —— 这个列表就是 process 的唤醒条件。

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

这一块的意思是:“每当 `clk` 或 `reset` 变了,就重新执行我的主体。”在那些事件之间,process 睡着,`q` 保持它的值 —— 这正是一个寄存器的行为。

## 99% 时候你会见到的两种形态

### 组合 process

敏感列表 = 主体内**读到的每一个信号**。没有时钟。输出是输入的纯函数。

```vhdl
process(a, b, sel)
begin
  if sel = '0' then
    y <= a;
  else
    y <= b;
  end if;
end process;
```

综合成一个 2 选 1 多路选择器。没有存储 —— 前提是每条路径上每个输出都被赋值(锁存器修复练习里会细讲)。

### 时钟驱动(时序)process

敏感列表 = 异步复位用 `(clk, reset)`,同步复位就只用 `(clk)`。数据赋值只在 `rising_edge(clk)`(或 `falling_edge`)里发生。输出是**寄存的** —— 它只在时钟沿变化。

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= (others => '0');
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

这就是经典的 D 触发器写法,也是你今后写的几乎每一个寄存器的模板。

## `rising_edge` 与老式的 `clk'event` 写法

你会看到两种检测时钟沿的办法:

```vhdl
elsif rising_edge(clk) then        -- 推荐
elsif clk'event and clk = '1' then -- 更老,对 '0'/'1' 而言等价
```

`rising_edge(clk)`(来自 `std_logic_1164`)是现代、更清晰的形式。它还能正确地忽略涉及元值的沿(比如 `'X'` → `'1'`),而裸的 `clk'event and clk='1'` 在那些值上可能误触发。用 `rising_edge`。

## 敏感列表陷阱

如果你忘了把一个*组合*主体读到的信号列进去,仿真和综合就会有分歧:

- **仿真**:那个信号变化时 process 不会重新执行。波形看着不对(输出像是“卡住了”)。
- **综合**:工具读主体,看到每一次读,无论如何都会构造出正确的硬件 —— 它无视你的列表。

结果:你的测试台和你的板子行为不一样,这是最糟糕的那种 bug。(时钟驱动的 process 对这个特定陷阱免疫,因为它们只在时钟沿上动作,但一个*组合* process 上不完整的列表是幽灵故障的经典成因。)VHDL-2008 用 `process(all)` 把它彻底解决:

```vhdl
process(all)  -- 工具把主体里读到的每个信号都算进去
begin
  ...
end process;
```

Kmila 的解析器接受 `process(all)`,在新设计里大多数团队的组合 process 用的也是它。

## 经验法则

- 敏感列表 = 只有 `(clk)` 或 `(clk, reset)` → 你描述的是**寄存器**(触发器)。
- 敏感列表 = 你读到的每一个输入(或 `all`)→ 你描述的是**组合逻辑**。
- 敏感列表把一个时钟沿和一些你随后在沿*之外*用到的额外数据信号混在一起 → 你几乎一定有 bug。

## 还有一条:别从两个 process 驱动同一个信号

一个在 process 里被驱动的信号,就把那个 process 当成它的驱动源。如果*第二个* process(或一条并发语句)也驱动它,你就造出了多个驱动源 —— 对 `std_logic` 会解析成 `'X'`,对不可解析类型则是编译错误。给每个信号恰好一个驱动源。

## 要点

- process 是在一个原本并发的文件里、那一段顺序代码的单位。
- 时钟驱动的 process(`rising_edge(clk)`)综合成触发器 —— 这就是你怎么得到记忆的。
- 优先用 `rising_edge(clk)`,而不是 `clk'event and clk='1'`。
- 把一个组合 process 读到的每个信号都列出来,或者用 `process(all)`;不完整的列表会让仿真和综合产生分歧。
- 一个信号,一个驱动源。
