# D 触发器

**D 触发器**存一个位，在时钟的**上升沿**被捕获。Q 取下 D 在边沿那一刻的值并保持到下一个边沿。 它是本阶梯中的第一个**时序**元件：与门不同，它的输出取决于时钟和它自己的过去，而不只是当前输入。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/19-d-ff.html" title="带实时时序图的交互式时序元件" loading="lazy"></iframe></div>

这个元件带有**记忆**。看时序图：时钟（**CLK**）在顶部跳动，输出 **Q** 只在时钟处改变——其间无论输入如何变化它都**保持**不变。Q 用彩色画出；虚线标出时钟边沿。

在 VHDL 中：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= d;          -- sampled on the edge
  end if;
end process;
```

D 触发器是同步设计的**基本**构件——每个寄存器、流水级和状态位都是一个或多个 D 触发器。在 D 前加一个 MUX 就得到加载使能或同步复位。
