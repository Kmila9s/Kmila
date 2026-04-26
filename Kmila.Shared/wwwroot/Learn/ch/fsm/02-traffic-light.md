# 示例：交通灯 FSM

交通灯是 Moore FSM 的典型案例：三个状态（`RED`、`GREEN`、`YELLOW`），每种颜色一个输出，再加上一个"在每个状态保持若干周期再切换"的计时器。

## 结构

示例遵循**双进程模式**：

- 进程 1 是**状态寄存器** —— 经典的 D 触发器，保存 `state` 并滴答一个保持计数器。
- 进程 2 是**次态逻辑** —— 纯组合，根据当前状态决定 `next_state`。
- 输出赋值（架构末尾）是组合的 Moore 输出 —— 它们只依赖 `state`。

## 代码走读

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 每种颜色持续 7 个 tick
```

`state_t` 是一个**枚举** —— 综合器自行选择实际的二进制编码，我们只需按名字引用状态。`count` 记录当前状态已经待了多久。`HOLD` 是保持时间。

状态寄存器：

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    state <= S_RED;
    count <= (others => '0');
  elsif rising_edge(clk) then
    if count = HOLD then
      state <= next_state;
      count <= (others => '0');
    else
      count <= count + 1;
    end if;
  end if;
end process;
```

次态逻辑：

```vhdl
process(state)
begin
  case state is
    when S_RED    => next_state <= S_GREEN;
    when S_GREEN  => next_state <= S_YELLOW;
    when S_YELLOW => next_state <= S_RED;
  end case;
end process;
```

输出（纯 Moore）：

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## 示例做了什么

时钟运行约 400 µs。每 8 个时钟（7 个保持 + 1 个切换），FSM 推进一步。你会看到三路方波按顺序轮换：`red` 高 8 个 tick，然后 `green` 高 8 个，然后 `yellow`，再回到 `red`。

## 轮到你了

- **添加行人请求输入。** 当 `ped = '1'` 且处于 `S_GREEN` 时，强制下一个状态为 `S_YELLOW`（以便周期变短让行人通行）。这就把 Moore 机变成了一点点 Mealy 的感觉 —— 输出 `yellow` 同时依赖 `state` 和 `ped` 输入。
- **发出 `walk` 信号**，仅在 `state = S_RED` 时为高。这是一个新的 Moore 输出。
- **把 `HOLD` 参数化**为 generic —— 7 对仿真够用，但在 50 MHz FPGA 上你需要几亿个周期才能获得 1 秒的保持时间。
