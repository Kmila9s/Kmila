# 项目：投币式自动售货机

这是你第一个像样的项目。规格已定，起始代码已经把管道铺好了，你的工作是写出完成它所需的逻辑。

## 你要构建的内容

一个 FSM，追踪顾客已投入的金额，并在总额达到 25 ¢ 时出货（单脉冲输出 `dispense`）。输入有：

- `nickel` —— 一个 1 周期脉冲，表示"一枚 5 ¢ 硬币刚刚投入"
- `dime`   —— 一个 1 周期脉冲，表示"一枚 10 ¢ 硬币刚刚投入"
- `clk`、`reset`

输出有：

- `dispense` —— 当你达到 ≥ 25 ¢ 时恰好拉高一个时钟周期
- `change`   —— 如果总额**超过** 25 ¢（因此机器需要找零），则与 `dispense` 同时拉高一个周期

## 验收标准

| 硬币序列             | 期望行为                      |
|----------------------|-------------------------------|
| 5 个 nickel          | `dispense=1, change=0`        |
| 2 个 dime + 1 个 nickel | `dispense=1, change=0`     |
| 3 个 dime（= 30 ¢）  | `dispense=1, change=1`        |
| 2 个 nickel + 1 个 dime | 不出货（只有 20 ¢）         |

## 起始代码给你的部分

entity 和 `total` 累加器都已接好。每个 `nickel` 脉冲加 5；每个 `dime` 脉冲加 10。但起始代码始终把 `dispense = '0'` 和 `change = '0'`。**这正是你要改的地方。**

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

1. **检测出货条件。** 每个上升沿上，检查加上当前硬币**之后**的 `total` 是否 `>= 25`。（注意：你需要计算的是**新的**总额，而不是上一个周期的。）
2. **让 `dispense` 只拉高一个周期。** 技巧：检测到出货时，把一个标志信号置位并把 `total` 重置为 0。该标志只存活一个周期。
3. **计算 `change`。** 如果出货时刻的总额 > 25，就把 `change` 也拉高。

提示：如果你把"下一次的总额"作为一个本地信号（或进程内的 `variable`）来计算，会更容易：

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

## 进阶

- 加一个**退币**按钮，把 `total` 清零而不出货。
- 支持一枚 **quarter**（25 ¢）硬币 —— 应该立即出货。
- 用一个单独的 8 位计数器跟踪已售商品数量，并作为输出公开。这是真实售货机向上游发送的反馈。
