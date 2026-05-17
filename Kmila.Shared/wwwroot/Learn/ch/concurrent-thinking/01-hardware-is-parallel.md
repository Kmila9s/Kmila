# 硬件是并行的

从 C、Python 或 JavaScript 过来的学生都内化了同一套心智模型："代码从上到下、一次一条地运行"。在 VHDL 里，只要你还抱着这个假设，就会被反复惩罚，直到你建立起正确的模型。

## 最大的观念转变

看看下面这段架构：

```vhdl
architecture rtl of demo is
begin
  y1 <= a and b;
  y2 <= a or  b;
  y3 <= y1 xor y2;
end architecture rtl;
```

在软件语言里，你会把这读成三条顺序执行的语句。在 VHDL 里，这三行描述的是**同时存在的三根线**。它们不按顺序执行 —— 它们就在那里。

综合器会生成：

```mermaid
flowchart LR
    A([a]) --> AND["AND"]
    B([b]) --> AND
    A --> OR["OR"]
    B --> OR
    AND -- y1 --> XOR["XOR"]
    OR  -- y2 --> XOR
    XOR --> Y3([y3])
```

三个门同时上电。每当 `a` 或 `b` 变化时，输出 `y3` 经过最长路径的传播延迟后就会稳定到新值。不存在"先"和"后"。

## 为什么顺序无关紧要

你可以用任意顺序写这三行：

```vhdl
y3 <= y1 xor y2;   -- 引用在"声明"之前？
y1 <= a and b;
y2 <= a or  b;
```

这仍然是合法的 VHDL。三条赋值描述三根线，而线并不在乎你画原理图时先画哪根。工具会把三行一起读进来并连接好。

## "顺序执行"从哪儿又溜回来

在 `process` 内部，事情**确实**是顺序的 —— 在那个进程内，一次一条、从上到下。但进程作为一个整体，仍然只是与其他所有并发单元并列的另一个并发单元。

```vhdl
architecture rtl of demo is
begin

  -- 并发单元 1
  y1 <= a and b;

  -- 并发单元 2（一个 process，内部是顺序的）
  process(clk)
  begin
    if rising_edge(clk) then
      counter <= counter + 1;
    end if;
  end process;

  -- 并发单元 3
  y2 <= a or b;

end architecture rtl;
```

这三个并发单元同时活着。每一个只要其输入发生变化，就会触发。进程体内部是按顺序读的，但那是进程本身的局部属性,而非架构的属性。

## 一个具体的后果

下面的设计存在一个微妙的竞争：

```vhdl
architecture wrong of demo is
  signal a, b : std_logic;
begin
  a <= not b;
  b <= not a;
end architecture wrong;
```

把它当软件读,像是"把 `a` 设为非 b，然后把 `b` 设为非 a" —— 没问题。把它当硬件读，却是两个反相器接成一个环。那是一个**振荡器** —— 一个永远无法稳定的反馈环。仿真会报错，综合（有时）会把它优化掉，而板子上会出现无法预测的毛刺。

## 你脑中的接线图改造

- 别再想"哪条先跑？"
- 开始想"有哪些线存在？"
- 在进程内部，"一次一条"的老模型又回来了 —— 但仅限于那个进程内部。

当这一点打通之后，VHDL 就不再跟你较劲了。你看到一段架构代码，马上就能看到它所代表的原理图，而不是你以前想象的"程序执行过程"。
