# 修复:意外推断出的锁存器

这是每个 VHDL 学生至少写过一次的两三个 bug 之一。代码在纸面上看着没问题,编译没错,仿真出来的输出也像那么回事 —— 直到你综合,工具警告说它推断出了一个你从没要过的**锁存器**。

## 起步代码

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- 缺少:enable = '0' 时 y 应该是什么?
  end process;
end architecture rtl;
```

## 哪里出错

在一段纯组合 process 里,每个输出在 process 的**每条路径**上都必须被赋值。这段只在 `enable = '1'` 时给 `y` 赋值。`enable = '0'` 时,process 对 `y` 什么都没说。

VHDL 对 “你没告诉我拿这个信号怎么办" 的解法是:**记住上一个值**。这就是锁存器 —— 一个透明的存储元件。综合会推出一个,设计就有时序路径噩梦,板子在信号跳变时会毛刺。

## 你的任务

打开示例,把设计改成**纯组合** —— 没锁存器、没触发器,只有门。两种符合习惯的改法:

**改法 1:顶部默认赋值**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- 默认
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

一个信号可以在 process 里多次赋值 —— 只有最后一次生效。顶部的默认值意思是 “除非有什么覆盖我,我就是 `'0'`"。

**改法 2:补完 `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';
  end if;
end process;
```

两种都对。综合出的硬件一模一样。哪种对当前设计读起来更好就用哪种。

## 验证你的修复

修完之后跑仿真。波形里:

- `15–25 µs` 期间(`enable = '1'` 且 `a=b='1'`):`y = '1'`。
- 其它时间:`y = '0'`。

修复之前,`enable` 掉回 `'0'` 之后 `y` 会一直保持 `'1'` —— 那正是锁存器在保持它上一个值,恰好是你要避免的。

## 规则

在组合 process 里,**每个输出都必须在每条路径上被赋值**。你做不到,要么加默认值,要么你其实是想要触发器 —— 那就换成时钟驱动 process,并且对那个决定负责。
