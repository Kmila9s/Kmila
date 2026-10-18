# 比较器

**比较器**比较两个数 A 和 B，报告是哪种情况：A < B、A = B 或 A > B。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/16-comparator.html" title="模块工作台上的交互式组合电路" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。输入推杆设定各位，模块进行计算，输出指示灯点亮，**表中当前行高亮**，于是你可以直接读出这个函数。

在 VHDL 中：

```vhdl
lt <= '1' when a < b else '0';
eq <= '1' when a = b else '0';
gt <= '1' when a > b else '0';
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 三个输出中恰好一个为 1。数值比较器逐位扩展，从最高位往下。
