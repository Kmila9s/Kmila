# 同或门（等价）

**同或门**在两个输入**相同**时输出 1——它就是带反相气泡的异或门。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/09-xnor.html" title="模块工作台上的交互式逻辑门" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。模块算出结果，输出指示灯 **Y** 按真值表点亮。

在 VHDL 中：

```vhdl
y <= a xnor b;
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 同或回答“这两个位是否相同？”——它就是 1 位的相等（比较）函数。
