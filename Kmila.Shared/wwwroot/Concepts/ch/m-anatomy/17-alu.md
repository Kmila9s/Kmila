# 算术逻辑单元 (ALU)

**ALU**（算术逻辑单元）是 CPU 核心的计算器：它取两个操作数 A 和 B，并按**操作选择**产生 A+B、A−B、A AND B、A OR B 等。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/17-alu.html" title="带操作选择鼓的交互式 ALU" loading="lazy"></iframe></div>

模块上的选择鼓选择运算；操作数推杆设定 **A** 和 **B**。列表高亮当前运算，读数显示 **A op B = R**。它是纯组合逻辑——没有时钟。

在 VHDL 中：

```vhdl
with op select
  r <= a + b     when "00",   -- ADD
       a - b     when "01",   -- SUB
       a and b   when "10",   -- AND
       a or b    when others; -- OR
```

ALU 是**组合逻辑**（加法器、各种门和选择结果的 MUX），本身没有记忆——它周围的寄存器保存操作数和结果。操作选择不过是在候选结果上的一个 MUX。
