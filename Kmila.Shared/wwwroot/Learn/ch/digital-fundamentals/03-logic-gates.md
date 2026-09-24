# 七种逻辑门

任何数字电路,直到现代 GPU,都是七个基本单元的组合。如果你知道每一种是怎么工作的、在 VHDL 里怎么写,你就已经开始做硬件设计了。

## 六个双输入门

| 门    | VHDL 写法    | 读作 | 输出为 `1` 当… |
|-------|-------------|------|----------------|
| AND   | `a and b`   | “A 与 B"   | 两个输入都是 1 |
| OR    | `a or b`    | “A 或 B"   | 至少一个输入是 1 |
| XOR   | `a xor b`   | “A 异或 B" | 输入不相同 |
| NAND  | `a nand b`  | “非(A 与 B)" | 至少一个输入是 0 |
| NOR   | `a nor b`   | “非(A 或 B)" | 两个输入都是 0 |
| XNOR  | `a xnor b`  | “A = B"   | 输入相同 |

加上单输入 NOT:

```vhdl
y <= not a;
```

## 为什么 NAND 与 NOR 特别

数字逻辑里一个经典结论:**只用 NAND**(或只用 NOR)就足以搭出其它每一种门。这两个被称作 “函数完备" 正是这个原因。CMOS 工艺库里大量提供 NAND2 和 NOR2 单元,因为你能用它们综合出所有东西。

- NOT: `not a = a nand a`
- AND: `a and b = not (a nand b)`
- OR:  `a or b = (not a) nand (not b)`

## XOR 是 “数学" 的那一个

XOR 常用一个圈里的加号(⊕)表示。它是计算 “奇偶性" 的门 —— 当奇数个输入为 `1` 时输出 `1`。所以它是这些东西的核心:

- 加法器:`sum = a xor b xor carry_in`
- 奇偶校验和错误检测码
- 切换触发器:`q_next = q xor toggle_input`

## 在一个文件里看完六种

如果你打开本模块的**真值表浏览器**实验,你会找到一个 VHDL entity,同时算这六种双输入门,`a`、`b` 接到 ports 标签页提供激励。然后波形查看器显示每个输出对每种组合的反应。

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

注意,这六行每一行都是**一根线**,不是一句要 “跑" 的语句。六个门、六块硬件 —— 它们同时求值。

## 要记住的

- 六个双输入门 + NOT 就是全部。
- NAND 与 NOR 是 “通用的" —— 你可以用它们造出任何东西。
- 在 VHDL 里,门是一行一行的并发赋值。它们不被 “调用";它们存在着。
