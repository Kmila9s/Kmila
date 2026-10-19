# 示例:你的第一个 entity(2 选 1 MUX)

**多路选择器**是带“选择”的最简单的组合器件。2 选 1 版根据一个 `sel` 位在两个输入里挑一个:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

对应的 VHDL 一行就够:

```vhdl
y <= a when sel = '0' else b;
```

## 整个文件

这样你就能在一个地方看到前几节课讲的全部四节:

```vhdl
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux21 is
  port (
    a   : in  std_logic;
    b   : in  std_logic;
    sel : in  std_logic;
    y   : out std_logic
  );
end entity mux21;

architecture rtl of mux21 is
begin
  y <= a when sel = '0' else b;
end architecture rtl;
```

## 示例做了什么

打开示例,你会得到一个叫 **Your first entity** 的项目,里头有:

- `mux21.vhd`(上面那段设计)
- 一份伴随的仿真 config,在 40 微秒内驱动 `sel`、`a`、`b`

你按 Run 之后,Waveforms 标签页显示四行 —— 三个输入和一个输出。看 `y` 随着 `sel` 翻转在 `a` 和 `b` 之间来回切换。

## 值得注意的

赋值 `y <= a when sel = '0' else b;` 是一条**并发语句**:它存在于任何 process 之外,描述的是一段永久的连线。没有所谓“这一行什么时候执行?” —— 它就*在*那儿。如果 `a`、`b` 或 `sel` 变了,`y` 会在一段传播延迟之后重新稳定下来。

`when/else` 里的条件可以是任何布尔表达式,而且你可以把它们串起来:

```vhdl
y <= a   when sel = '0' else
     b   when enable = '1' else
     'Z';
```

这样的链综合成一个**带优先级的**多路选择器(最顶上的条件在平局时胜出)。

## 底下的理论:香农展开

那一行并不只是方便的语法 —— 它是布尔代数里一条定理的硬件形态,课程里称之为**香农展开定理(teorema de expansión de Shannon)**。任何关于某个变量 `s` 的函数都可以拆成:

```
f(s, …) = (s' · f(0, …)) + (s · f(1, …))
```

一个 2 选 1 MUX 计算的正好就是 `y = sel'·a + sel·b`。换句话说,多路选择器就是围绕选择位做香农展开的物理实现 —— 这也是为什么更大的函数常常靠堆叠 MUX 来搭。把这点揣在兜里;它是课程后面多路选择器/多路分配器那一单元背后的思想。

## 轮到你:扩展它

看到示例能跑之后:

1. 把 entity 改成 **4 选 1 MUX** —— `sel` 变成一个 2 位 `std_logic_vector`,你在 `a`、`b`、`c`、`d` 之间挑一个。(这里用 `with sel select` 语句读起来很清爽 —— 你会在组合逻辑模块里见到它。)
2. 加一个第五端口 `output_enable`。当它为 `'0'` 时,无论 `sel` 是什么都驱动 `y <= 'Z'`(高阻)。
3. 搭一个**多路分配器** —— 它的镜像。一个输入、一个 `sel`、几个输出;输入被路由到恰好一个输出,其余的停在 `'0'`。

Kmila 的编辑器会高亮 VHDL 关键字,也认识 `std_logic` —— 把鼠标悬到 `when` 上看看内置的文档。

## 要点

- **entity** 给接口起名;**architecture** 给出行为。这个 MUX 是两者的一个完整而最小的例子。
- `y <= a when sel = '0' else b;` 是一条并发条件赋值 —— 永久的连线,不是一行会“运行”的代码。
- 多路选择器就是硅片里的香农展开:`y = sel'·a + sel·b`。这一个思想能放大到你将要搭的每一个更宽的 MUX。
