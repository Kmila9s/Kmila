# 示例:你的第一个 entity(2 选 1 MUX)

**多路选择器**是带 “选择" 的最简单组合器件。2 选 1 版根据一个 `sel` 位在两个输入里挑一个:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

对应的 VHDL 一行就够:

```vhdl
y <= a when sel = '0' else b;
```

## 示例做了什么

打开示例,你会得到一个叫 **Your first entity** 的项目,里头有:

- `mux21.vhd`(上面那段设计)
- 一份伴随的仿真 config,40 微秒内驱动 `sel`、`a`、`b`

你按 Run 之后,Waveforms 标签页显示四行 —— 三个输入和一个输出。看 `y` 在 `a` 和 `b` 之间随 `sel` 来回切换。

## 值得注意的

赋值 `y <= a when sel = '0' else b;` 是一条**并发语句**:它存在于任何 process 之外,描述的是一个永久连线。没有所谓 “这一行什么时候执行?" —— 它就在那儿。

`when/else` 里的条件可以是任何布尔表达式:

```vhdl
y <= a when sel = '0' else
     b when enable = '1' else
     'Z';
```

这样的链综合成一个优先级多路选择器(顶端的条件赢)。

## 轮到你:扩展它

看到示例能跑之后:

1. 把 entity 改成 **4 选 1 MUX** —— `sel` 变成 2 位,你在 `a`、`b`、`c`、`d` 之间挑一个。
2. 加一个第五端口 `output_enable`。当它为 `'0'` 时,无论 `sel` 是什么,都驱动 `y <= 'Z'`(高阻)。
3. 比较一下 `when/else` 与下一模块要见到的 `with/select` 版本。

Kmila 的编辑器会高亮 VHDL 关键字,也认识 `std_logic` —— 把鼠标悬到 `when` 上看看内置的文档。
