# process 与敏感列表

VHDL 里 process 之外的一切都是并发的 —— 都是线和门。一旦你想让一段电路**记住**从这个时钟周期到下一个时钟周期之间的某个东西,你就需要 process。

## process 是什么

`process` 是 VHDL architecture 内部的一段顺序代码。它的语句自上而下读,跟软件一样。但 process 本身只在它的**敏感列表**里有信号变化时才 “跑"。

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

这一块的意思是:“每当 `clk` 或 `reset` 变了,就重新执行我的主体。"

## 99% 时候你会见到的两种形态

### 组合 process

敏感列表 = 主体内读到的每个输入。没有时钟。输出是输入的纯函数。

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

综合成 2 选 1 多路选择器。没有存储。

### 时钟驱动(时序)process

敏感列表 = `(clk, reset)`。赋值只在 `rising_edge(clk)`(或者 `falling_edge`,要是你不走运)里发生。输出是**寄存的** —— 只在时钟沿变化。

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

这就是经典的 D 触发器写法。

## 敏感列表陷阱

如果你忘了把主体读到的信号写进敏感列表,仿真和综合会有分歧:

- **仿真**:那个信号变化时 process 不会重新执行。波形看着不对。
- **综合**:工具读主体,看到每一次读,无论如何都会构造出正确的硬件。

结果:你的测试台过了,板子却跑不起来 —— 或者反过来。VHDL-2008 让你写 `process(all)` 来完全避免这个:

```vhdl
process(all)  -- 包含主体里读到的每个信号
begin
  ...
end process;
```

Kmila 的解析器接受 `process(all)`,大多数团队在新设计里用的也是它。

## 经验法则

- 敏感列表 = 只有 `(clk, reset)` → 你描述的是**寄存器**(触发器)。
- 敏感列表 = 你读到的每一个输入 → 你描述的是**组合逻辑**。
- 敏感列表把时钟信号和非时钟信号混着 → 你几乎一定有 bug。

## 要记住的

- process 是在原本并发的文件里、那一段顺序代码的单位。
- 时钟驱动的 process(`rising_edge(clk)`)综合成触发器。这就是你怎么得到记忆的。
- 列出你读到的每个信号,或者用 `process(all)` 让工具替你做。
