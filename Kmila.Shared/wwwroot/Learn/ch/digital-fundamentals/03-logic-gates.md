# 七种逻辑门

每一个数字电路 —— 一直到现代 GPU —— 都是七种基本单元的组合。只要你知道每一种的行为以及如何用 VHDL 写出来，你就已经开始做硬件设计了。

## 六种双输入门

| 门    | VHDL 中的符号  | 读作          | 输出为 `1` 的条件 |
|-------|----------------|---------------|-------------------|
| AND   | `a and b`      | "A 与 B"      | 两个输入都为 1    |
| OR    | `a or b`       | "A 或 B"      | 至少一个输入为 1  |
| XOR   | `a xor b`      | "A 异或 B"    | 两个输入不同      |
| NAND  | `a nand b`     | "非 (A 与 B)" | 至少一个输入为 0  |
| NOR   | `a nor b`      | "非 (A 或 B)" | 两个输入都为 0    |
| XNOR  | `a xnor b`     | "A 等于 B"    | 两个输入相同      |

以及单输入的 NOT：

```vhdl
y <= not a;
```

## NAND 和 NOR 为什么特殊

数字逻辑里有一条经典结论：**仅用 NAND**（或仅用 NOR）就足以构造出其他所有门。两者都因此被称为"功能完备的"。这就是 CMOS 工艺库会大量提供 NAND2 和 NOR2 单元的原因 —— 你可以用它们综合出所有东西。

- NOT：`not a = a nand a`
- AND：`a and b = not (a nand b)`
- OR： `a or b = (not a) nand (not b)`

## XOR 是最"数学"的那一个

XOR 常被画成一个带圈的加号（⊕）。它是计算"奇偶性"的门 —— 当有奇数个输入为 `1` 时输出为 `1`。这让它成为下列事物的核心：

- 加法器：`sum = a xor b xor carry_in`
- 奇偶校验和错误检测码
- 触发式触发器：`q_next = q xor toggle_input`

## 把六种门放进一个文件

如果你打开本模块的**真值表探索器**实验，你会看到一个 VHDL entity 同时计算所有六种双输入门，输入 `a` 和 `b` 连到 Ports 标签做激励。波形查看器则展示每个输出在每种组合下的反应。

```vhdl
architecture rtl of gates is
begin
  o_and  <= a and b;
  o_or   <= a or  b;
  o_xor  <= a xor b;
  o_nand <= a nand b;
  o_nor  <= a nor  b;
  o_xnor <= a xnor b;
end architecture rtl;
```

注意这六行每一行都是**一根线**，而不是一条会被执行的语句。六个门，六块硬件 —— 它们全部同时求值。

## 关键要点

- 六种双输入门 + NOT 就是完整集合。
- NAND 和 NOR 是"通用"门 —— 用它们能搭出任何东西。
- 在 VHDL 里，门是单行的并发赋值。它们并不被"调用"；它们就在那里。
