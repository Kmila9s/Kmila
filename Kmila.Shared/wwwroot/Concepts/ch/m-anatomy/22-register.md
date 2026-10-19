# 寄存器

**寄存器**是一组触发器，一次存下一整个字。在时钟边沿（装载时），D 的所有位一起被捕获到 Q。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/22-register.html" title="由时钟驱动的交互式多位时序块" loading="lazy"></iframe></div>

左侧有一个自由运行的时钟；4 位状态既显示为一排 **Q 指示灯**，也显示为实时数值（十进制+二进制）。每个时钟边沿状态更新；边沿之间它保持不变。

在 VHDL 中：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if ld = '1' then q <= d; end if;  -- load on edge
  end if;
end process;
```

CPU 把工作数值存在寄存器里，每个流水级都是一个寄存器。在前面加一个 MUX（新值 vs. 当前 Q），装载使能就变成了保持。
