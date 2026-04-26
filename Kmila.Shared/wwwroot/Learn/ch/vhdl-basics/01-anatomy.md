# VHDL 文件的解剖结构

每一个 VHDL 文件都有相同的四个部分，总是同一个顺序。一旦你认出这种形状，阅读不熟悉的代码就不再令人望而却步 —— 你知道去哪里找每一块。

## 四个部分

```vhdl
library IEEE;                          -- 1. 库
use IEEE.STD_LOGIC_1164.ALL;           -- 2. 正在使用的 package

entity my_design is                    -- 3. entity（"头部"）
  port (
    a : in  std_logic;
    b : in  std_logic;
    y : out std_logic
  );
end entity my_design;

architecture rtl of my_design is       -- 4. architecture（"主体"）
begin
  y <= a and b;
end architecture rtl;
```

让我们逐一看。

### 1. `library`

`library IEEE;` 导入标准 IEEE 库。这相当于数字设计版的 `#include <stdio.h>` —— 你写的几乎每一个文件都以它开头。

### 2. `use`

`use IEEE.STD_LOGIC_1164.ALL;` 把 `STD_LOGIC_1164` package 中的每一个名字都引入作用域。这就是让你能用 `std_logic`、`std_logic_vector`、`rising_edge()` 这类东西的来源。

你经常会看到第二条 `use`：

```vhdl
use IEEE.NUMERIC_STD.ALL;
```

这一条给你 `unsigned`、`signed`、`to_integer`、`resize`，以及针对向量类型的算术运算。只要你在向量上做算术，就加上它。

### 3. `entity`

entity 是**黑盒接口**。它说明：

- 这个设计叫什么（`my_design`）
- 它有哪些端口
- 每个端口的**方向**（`in`、`out`、`inout`、`buffer`）和**类型**

`port (…)` 内部的一切就是外界能看到的。这里不声明设计是怎么工作的。

### 4. `architecture`

architecture 是**实现**。一个 entity 可以有多个 architecture（对应同一个接口）—— 你可以为同一个 `cpu` entity 拥有 `rtl`、`behavioral` 和 `tb` 三种 architecture，按配置选其一。实际中，大多数文件恰好有一个。

`begin` 到 `end architecture` 之间的主体是真正描述信号和门的地方。

## Kmila 如何解析这些

Kmila 的 VHDL 解析器从上到下读你的文件并构建一棵树：

```
File
├── library IEEE
├── use IEEE.STD_LOGIC_1164.ALL
└── entity my_design
    ├── port a (in, std_logic)
    ├── port b (in, std_logic)
    ├── port y (out, std_logic)
    └── architecture rtl
        └── concurrent: y <= a and b
```

当你点击 **Build** 时，端口成为 **Ports** 面板标签里的条目，entity 成为 **Entities** 里的条目，而 architecture 内的赋值则成为仿真器要运行的连线。

## 命名约定

VHDL 是大小写不敏感的。`SIGNAL`、`Signal` 和 `signal` 是同一个关键字。但在同一个文件里保持一致的风格 —— 大多数团队对关键字和信号名使用**小写**、对标识符使用 **snake_case**、对常量使用 **UPPER_CASE**。

## 关键要点

- `library` + `use` —— 你的导入。
- `entity` —— 接口（端口及方向）。
- `architecture` —— 实现（到底发生了什么）。
- 一个 entity 可以有多个 architecture；大多数项目发布一个叫 `rtl` 的。
