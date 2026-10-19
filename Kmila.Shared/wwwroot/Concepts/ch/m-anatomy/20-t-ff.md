# T 触发器

**T 触发器翻转**：每个边沿，若 T = 1 则所存位翻转（0↔1）；若 T = 0 则保持。它把时钟频率二分频。 它是本阶梯中的第一个**时序**元件：与门不同，它的输出取决于时钟和它自己的过去，而不只是当前输入。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/20-t-ff.html" title="带实时时序图的交互式时序元件" loading="lazy"></iframe></div>

这个元件带有**记忆**。看时序图：时钟（**CLK**）在顶部跳动，输出 **Q** 只在时钟处改变——其间无论输入如何变化它都**保持**不变。Q 用彩色画出；虚线标出时钟边沿。

在 VHDL 中：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if t = '1' then q <= not q; end if;
  end if;
end process;
```

把多个 T 触发器级联，每一级再二分频——这正是一个二进制**行波计数器**。T 触发器就是把 D 接到 Q xor T 的 D 触发器。
