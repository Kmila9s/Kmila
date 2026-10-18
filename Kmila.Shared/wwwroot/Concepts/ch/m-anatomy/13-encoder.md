# 编码器

**编码器**是译码器的逆：给定一个独热输入（一条线有效），它输出该线的**二进制地址**。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/13-encoder.html" title="模块工作台上的交互式组合电路" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。输入推杆设定各位，模块进行计算，输出指示灯点亮，**表中当前行高亮**，于是你可以直接读出这个函数。

在 VHDL 中：

```vhdl
a <= "11" when i3 = '1' else
     "10" when i2 = '1' else
     "01" when i1 = '1' else
     "00";
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 **优先**编码器通过取最高者来处理多个有效输入——对必须选出一个请求的中断控制器很有用。
