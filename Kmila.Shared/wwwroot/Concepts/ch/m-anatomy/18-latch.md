# D 锁存器

**D 锁存器**存一个位。当使能（这里是时钟）为高时它是**透明**的——Q 跟随 D；使能变低时，它**冻结**最后的值。 它是本阶梯中的第一个**时序**元件：与门不同，它的输出取决于时钟和它自己的过去，而不只是当前输入。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/18-latch.html" title="带实时时序图的交互式时序元件" loading="lazy"></iframe></div>

这个元件带有**记忆**。看时序图：时钟（**CLK**）在顶部跳动，输出 **Q** 只在时钟处改变——其间无论输入如何变化它都**保持**不变。Q 用彩色画出；虚线标出时钟边沿。

在 VHDL 中：

```vhdl
process (en, d)
begin
  if en = '1' then   -- transparent while enabled
    q <= d;
  end if;
end process;
```

由于它是**电平敏感**的，锁存器在整个时钟相位内都透明——该窗口内 D 上的任何毛刺都会穿过。所以同步设计偏爱边沿触发的触发器。
