# 进程与敏感信号列表

VHDL 中进程之外的一切都是并发的 —— 线与门。当你需要让一个电路从一个时钟周期**记住**到下一个时，就需要一个进程。

## 进程是什么

`process` 是 VHDL 架构内的一段顺序代码块。它的各行从上到下被阅读，就像软件一样。但进程本身只有在其**敏感信号列表**中的某个信号发生变化时才会"运行"。

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

这一段的意思是："每当 `clk` 或 `reset` 变化时，重新求值我的主体。"

## 你 99% 的时间会看到的两种形状

### 组合进程

敏感信号列表 = 主体内读到的每一个输入。没有时钟。输出是输入的纯函数。

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

综合为一个 2-to-1 多路选择器。没有存储。

### 时钟（时序）进程

敏感信号列表 = `(clk, reset)`。赋值只在 `rising_edge(clk)`（或你不走运的话 `falling_edge`）内发生。输出是**寄存的** —— 只在时钟沿变化。

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

这是经典的 D 触发器模式。

## 敏感信号列表陷阱

如果你忘了在列表里列出主体读到的某个信号，仿真和综合会给出不一致的行为：

- **仿真**：该信号变化时进程不会重新求值。波形看起来不对。
- **综合**：工具读完主体，看到每次读取，照样构建正确的硬件。

结果：你的测试台通过了，而板子不工作 —— 或者反过来。现代 VHDL-2008 允许你写 `process(all)` 来彻底避免此问题：

```vhdl
process(all)  -- 包含主体内读到的每一个信号
begin
  ...
end process;
```

Kmila 的解析器接受 `process(all)`，而这也是大多数团队在新设计里使用的写法。

## 经验法则

- 敏感列表 = 仅 `(clk, reset)` → 你在描述**寄存器**（触发器）。
- 敏感列表 = 你读的每一个输入 → 你在描述**组合逻辑**。
- 敏感列表同时混杂了时钟和非时钟信号 → 你几乎肯定有一个 bug。

## 关键要点

- 进程是这个原本并发的文件中顺序代码的单位。
- 时钟进程（`rising_edge(clk)`）综合为触发器。这就是你获得记忆的方式。
- 要么列出你读到的每一个信号，要么用 `process(all)` 让工具替你列。
