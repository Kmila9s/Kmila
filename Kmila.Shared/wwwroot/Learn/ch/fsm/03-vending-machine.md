# 项目:投币式自动售货机

这是你的第一个像样的项目。规格是定死的,起步代码把管线搭好,你的任务是把补完逻辑的代码写出来。

## 你要构建什么

一个跟踪顾客投入的金额、累计到 25¢ 就出货(`dispense` 单一脉冲)的 FSM。输入:

- `nickel` —— 1 周期脉冲,表示 “刚投入一枚 5¢”
- `dime` —— 1 周期脉冲,表示 “刚投入一枚 10¢”
- `clk`、`reset`

输出:

- `dispense` —— 累计到 ≥ 25¢ 时,拉高恰好一个时钟周期
- `change` —— 如果累计 **超过** 25¢,与 `dispense` 同时拉高一个周期(机器要找零)

## 验收标准

| 投币序列            | 期望行为                       |
|---------------------|--------------------------------|
| 5 个 nickel         | `dispense=1, change=0`         |
| 2 个 dime + 1 nickel| `dispense=1, change=0`         |
| 3 个 dime(= 30¢)   | `dispense=1, change=1`         |
| 2 个 nickel + 1 dime| 不出货(只有 20¢)             |

## 起步代码给了你什么

entity 和 `total` 累加器都接好了。每个 `nickel` 脉冲加 5,每个 `dime` 脉冲加 10。但起步代码始终驱动 `dispense = '0'` 和 `change = '0'`。**这就是你要改的地方。**

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    total <= (others => '0');
  elsif rising_edge(clk) then
    if    nickel = '1' then total <= total + 5;
    elsif dime   = '1' then total <= total + 10;
    end if;
  end if;
end process;

dispense <= '0';  -- TODO
change   <= '0';  -- TODO
```

## 建议的做法

1. **检测出售条件**。每个上升沿,在加上当前硬币之后判断 `total >= 25`。(注意:你要算的是*新的* total,不是上一周期的。)
2. **让 `dispense` 拉高恰好一个周期**。诀窍:检测到出售时,设一个标志信号,把 `total` 清零。这个标志只活一个周期。
3. **计算 `change`**。如果出售那一刻的总额 > 25,也把 `change` 拉高一个周期。

提示:把 “下一拍 total” 算成一个本地信号(或 process 内部 `variable`)会更顺:

```vhdl
variable next_total : unsigned(5 downto 0);
...
next_total := total;
if    nickel = '1' then next_total := total + 5;
elsif dime   = '1' then next_total := total + 10;
end if;

if next_total >= 25 then
  dispense <= '1';
  change   <= '1' when next_total > 25 else '0';
  total    <= (others => '0');
else
  dispense <= '0';
  change   <= '0';
  total    <= next_total;
end if;
```

## 再进一步

- 加一个**退币**按钮,把 `total` 清零但不出货。
- 支持 **quarter**(25¢)硬币 —— 应该立刻出货。
- 把出货数用一个独立的 8 位计数器记下来作为输出。真实的售货机就是这样向上游汇报的。
