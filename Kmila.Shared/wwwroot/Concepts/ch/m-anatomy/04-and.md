# 与门

**与门**只有当**两个**输入都为 1 时才输出 1。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/04-and.html" title="模块工作台上的交互式逻辑门" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。模块算出结果，输出指示灯 **Y** 按真值表点亮。

在 VHDL 中：

```vhdl
y <= a and b;
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 可以想成两个开关串联：只有两个都闭合，电流才能到达灯。
