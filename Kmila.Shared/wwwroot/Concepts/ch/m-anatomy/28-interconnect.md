# 互连与 I/O

**互连**是单元之间可编程的布线：**布线通道**、交叉处连接它们的**开关盒**，以及边缘连到引脚的 **I/O 块 (IOB)**。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/28-interconnect.html" title="交互式图示" loading="lazy"></iframe></div>

一个信号从某个 I/O 块出发，沿通道、过开关盒，布线到一个 CLB——那条点亮的路径。给开关盒编程，正是把你的设计连起来。

FPGA 的面积与延迟其实大多在**布线**上，而非逻辑。布局布线决定用哪些单元、怎样连线——综合之后那个又慢又巧的步骤。
