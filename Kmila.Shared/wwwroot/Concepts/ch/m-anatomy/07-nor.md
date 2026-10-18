# 或非门

**或非门**是“或”之后接一个“非”：只有两个输入都为 0 时才输出 1。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/07-nor.html" title="模块工作台上的交互式逻辑门" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。模块算出结果，输出指示灯 **Y** 按真值表点亮。

在 VHDL 中：

```vhdl
y <= a nor b;
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 小圆圈表示取反。**或非门同样是通用的**：任何电路都可以只用或非门搭出来。
