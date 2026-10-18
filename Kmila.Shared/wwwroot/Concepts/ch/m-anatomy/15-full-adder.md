# 全加器

**全加器**把三个位——A、B 和进位输入 **Ci**——相加，得到和 S 与进位输出 **Co**。级联起来就能加宽位数。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/15-full-adder.html" title="模块工作台上的交互式组合电路" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。输入推杆设定各位，模块进行计算，输出指示灯点亮，**表中当前行高亮**，于是你可以直接读出这个函数。

在 VHDL 中：

```vhdl
s  <= a xor b xor ci;
co <= (a and b) or (ci and (a xor b));
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 全加器是一切二进制加法器的基本块：把 n 个级联起来，进位接进位，就能把两个 n 位数相加。它由两个半加器加一个或门构成。
