# 逻辑单元 (slice)

**逻辑单元**（或 *slice*）是 FPGA 重复的基本块：一个 **LUT** + 一个 **触发器** + 一个输出 **MUX**（外加进位链）。它能给出组合或寄存的输出。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/27-slice.html" title="交互式图示" loading="lazy"></iframe></div>

输入送入 LUT；其结果要么直接输出（**组合**），要么经过触发器（**寄存**）；由 MUX 选择。时钟驱动触发器；一条进位链横跨该块。

把 `y <= a and b` 和一个 `rising_edge(clk)` 寄存器放在一起，它们就装进一个 slice——LUT 管逻辑，触发器管状态。现代 slice 含有多个 LUT 和触发器。
