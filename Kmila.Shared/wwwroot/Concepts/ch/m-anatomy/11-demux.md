# 多路分配器 (DEMUX)

**多路分配器**是 MUX 的逆：它把一个输入 **D** 送到若干输出中的一个，由**选择**线 S 决定。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/11-demux.html" title="模块工作台上的交互式组合电路" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。输入推杆设定各位，模块进行计算，输出指示灯点亮，**表中当前行高亮**，于是你可以直接读出这个函数。

在 VHDL 中：

```vhdl
y0 <= d when s = '0' else '0';
y1 <= d when s = '1' else '0';
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 未被选中的输出保持为 0。译码器与多路分配器是近亲：demux 就是一个由数据输入去闸控有效输出的译码器。
