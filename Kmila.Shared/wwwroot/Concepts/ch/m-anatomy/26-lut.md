# 查找表 (LUT)

**查找表 (LUT)** 是 FPGA 变成任意门的方式：一块装着真值表的小存储器。输入构成进入它的**地址**；输出就是该地址处所存的位。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/26-lut.html" title="交互式图示" loading="lazy"></iframe></div>

输入对 4 个配置单元（载入的真值表）寻址；输出是被选中的那一位。看配置如何**重新编程**——同一个 LUT 变成 AND、再变 OR、再变 XOR。这就是可重构逻辑。

在 VHDL 中：

```vhdl
y <= a and b;   -- the synthesizer loads this
                -- truth table into ONE LUT
```

你的整个设计都映射到 LUT：综合器把每个布尔表达式变成真值表，上电时从配置比特流载入。一个 N 输入的 LUT 可以是 N 输入的**任意**函数。
