# 并发赋值:`when/else` 与 `with/select`

VHDL 里的组合逻辑用并发语句来写 —— process 外面的每一行描述的都是一段永久接好的硬件。同一件事有两种语法味道:**条件**赋值与**选择**赋值。

## `when/else` —— 条件

适合你手里有一组带优先级的条件、或者每个分支测的是不同信号:

```vhdl
y <= '0' when reset = '1' else
     a   when mode  = "read" else
     b   when mode  = "write" else
     'Z';
```

第一个满足的条件获胜。顺序很重要 —— 它编译成**带优先级的 MUX**:最上面的条件到 `y` 的路径最短。

## `with/select` —— 选择

适合你在单个表达式上分支(就像软件的 `switch`):

```vhdl
with mode select
  y <=
    a   when "00",
    b   when "01",
    c   when "10",
    'Z' when others;
```

所有分支都比较同一个信号(`mode`),并且 `when others` 必须有,以覆盖一切可能模式。这种写法编译成**对称 MUX** —— 没哪条分支有特权。

## 你该挑哪个?

- **同一信号、多种模式:** `with/select` 读起来像真值表。用它。
- **不同信号、优先级逻辑:** `when/else`。天然合适。
- **综合结果:** 在现代 FPGA 上两者产生的门几乎一样 —— 在源码层面挑更清晰的形式几乎总是正确选择。

## 一个小陷阱

两种写法都要求 `y` 在**每一种**情形里都被赋值。如果你忘了 `when/else` 末尾的 `else`、或者漏掉 `with/select` 的 `when others`,编译器会警告 —— 而综合器可能会推断出一个锁存器来 “记住” 上次的值,这几乎绝不是你想要的。这就是你将在时序模块的 “fix-it” 练习中再次遇到的同一个锁存器陷阱。

## 下次再见

紧接这节课的 3-8 解码器示例用 `with/select`;后面交通灯 FSM 里 `when/else` 到处都是。两者都是 VHDL 的家常便饭。
