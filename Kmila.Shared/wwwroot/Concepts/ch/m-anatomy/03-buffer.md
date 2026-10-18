# 缓冲器 (BUF)

**缓冲器**把输入原样复制到输出（Y = A）。它不改变逻辑——用来**驱动**信号：增强它，或加入可控的延迟。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/03-buffer.html" title="模块工作台上的交互式逻辑门" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。模块算出结果，输出指示灯 **Y** 按真值表点亮。

在 VHDL 中：

```vhdl
y <= a;
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 注意输出端没有反相小圆圈——这正是它与非门唯一的区别。
