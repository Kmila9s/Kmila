# JK 触发器

**JK 触发器**最通用：每个边沿，J 把 Q 置 1，K 把它清 0，两者同时则**翻转**，都为 0 则保持。 它是本阶梯中的第一个**时序**元件：与门不同，它的输出取决于时钟和它自己的过去，而不只是当前输入。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/21-jk-ff.html" title="带实时时序图的交互式时序元件" loading="lazy"></iframe></div>

这个元件带有**记忆**。看时序图：时钟（**CLK**）在顶部跳动，输出 **Q** 只在时钟处改变——其间无论输入如何变化它都**保持**不变。Q 用彩色画出；虚线标出时钟边沿。

在 VHDL 中：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if    j='1' and k='1' then q <= not q;
    elsif j='1' then q <= '1';
    elsif k='1' then q <= '0';
    end if;
  end if;
end process;
```

JK 消除了老式 SR 锁存器的禁止态。任何触发器都能由另一种搭出——D、T、JK 在前面加点逻辑即可互换。
