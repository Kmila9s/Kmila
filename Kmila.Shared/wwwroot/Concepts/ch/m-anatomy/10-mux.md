# 多路复用器 (MUX)

**多路复用器**把若干数据输入中的一个送到输出。一条**选择**线 **S** 决定选哪个：这里 S = 1 时 Y = D1，否则 Y = D0。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/10-mux.html" title="模块工作台上的交互式组合电路" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。输入推杆设定各位，模块进行计算，输出指示灯点亮，**表中当前行高亮**，于是你可以直接读出这个函数。

在 VHDL 中：

```vhdl
y <= d1 when s = '1' else d0;
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 VHDL 里每个 `if/then/else` 和 `case` 综合之后都成了 MUX。寄存器的“加载使能”也是这样：由 MUX 在新值与保持旧值之间选择。
