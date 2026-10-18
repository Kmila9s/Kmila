# 异或门

**异或门**（排他或）在两个输入**不同**时输出 1。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/08-xor.html" title="模块工作台上的交互式逻辑门" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。模块算出结果，输出指示灯 **Y** 按真值表点亮。

在 VHDL 中：

```vhdl
y <= a xor b;
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 异或是加法（和位）、奇偶校验和比较的核心：它回答“这两个位是否不同？”。
