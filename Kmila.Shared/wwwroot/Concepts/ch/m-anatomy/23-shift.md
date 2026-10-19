# 移位寄存器

**移位寄存器**在每个时钟边沿把它的各位移动一格，一端进入一个新位——数据的接力。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/23-shift.html" title="由时钟驱动的交互式多位时序块" loading="lazy"></iframe></div>

左侧有一个自由运行的时钟；4 位状态既显示为一排 **Q 指示灯**，也显示为实时数值（十进制+二进制）。每个时钟边沿状态更新；边沿之间它保持不变。

在 VHDL 中：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q(2 downto 0) & si;  -- shift left, SI in
  end if;
end process;
```

移位寄存器做串并转换（UART 接收）、乘/除以二，并用来构造产生伪随机序列的 LFSR。
