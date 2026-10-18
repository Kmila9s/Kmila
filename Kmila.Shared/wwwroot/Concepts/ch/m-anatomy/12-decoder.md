# 译码器

**译码器**把 n 位地址变成**独热**输出：它的 2ⁿ 个输出中恰好有一个为 1，就是地址所指的那个。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/12-decoder.html" title="模块工作台上的交互式组合电路" loading="lazy"></iframe></div>

移动光标（或方向键）逐一遍历所有输入组合。输入推杆设定各位，模块进行计算，输出指示灯点亮，**表中当前行高亮**，于是你可以直接读出这个函数。

在 VHDL 中：

```vhdl
with a select y <=
  "0001" when "00",
  "0010" when "01",
  "0100" when "10",
  "1000" when others;
```

它是纯**组合**逻辑：输出只取决于当前输入，没有时钟，也没有记忆。 译码器驱动地址线：存储芯片、七段显示和指令分派都用这种方式从众多线中选一条。
