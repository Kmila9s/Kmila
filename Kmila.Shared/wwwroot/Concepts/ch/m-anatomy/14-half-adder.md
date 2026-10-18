# 半加器

**半加器**把两个位 A 和 B 相加，得到**和** S 与**进位** C。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/14-half-adder.html" title="模块工作台上的交互式组合电路" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。输入推杆设定各位，模块进行计算，输出指示灯点亮，**表中当前行高亮**，于是你可以直接读出这个函数。

在 VHDL 中：

```vhdl
s <= a xor b;   -- sum
c <= a and b;   -- carry
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 和就是 **XOR**，进位就是 **AND**：半加器其实就是这两个门并排。它没有进位输入，所以只是第一级。
