# 计数器

**计数器**在每个时钟边沿把所存的值加一，依次 0、1、2……并回绕。它就是一个带 +1 加法器反馈的寄存器。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/24-counter.html" title="由时钟驱动的交互式多位时序块" loading="lazy"></iframe></div>

左侧有一个自由运行的时钟；4 位状态既显示为一排 **Q 指示灯**，也显示为实时数值（十进制+二进制）。每个时钟边沿状态更新；边沿之间它保持不变。

在 VHDL 中：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q + 1;   -- increment every edge
  end if;
end process;
```

计数器测量时间与事件、产生地址、对时钟分频。看最低位每个边沿翻转，每高一位则以一半的速率翻转——运动中的二进制计数。
