# 示例:交通灯 FSM

交通灯是教科书式的 Moore FSM:三个状态(`RED`、`GREEN`、`YELLOW`),每种颜色一个输出,还有一个计时器说 “每个状态保持几拍再换"。

## 结构

例子遵循**双进程模式**:

- 进程 1 是**状态寄存器** —— 一个经典的 D 触发器,保存 `state` 并推动一个保持计数器。
- 进程 2 是**次态逻辑** —— 纯组合,根据当前状态决定 `next_state` 是什么。
- 输出赋值(在 architecture 底部)是组合的 Moore 输出 —— 值只依赖 `state`。

## 走一遍代码

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 每色 7 拍
```

`state_t` 是**枚举** —— 综合器自行选具体的二进制编码,我们只引用命名状态。`count` 跟踪在当前状态待了多久。`HOLD` 是保持时长。

状态寄存器:

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

次态逻辑:

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

输出(纯 Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## 示例做了什么

时钟跑大约 400 µs。每 8 拍(7 拍保持 + 1 拍转移)FSM 前进一步。你会看到三个方波依次轮转:`red` 拉高 8 拍,然后 `green` 8 拍,然后 `yellow`,再回到 `red`。

## 轮到你

- **加一个行人请求输入**。当 `ped = '1'` 且状态为 `S_GREEN` 时,强制 `next_state <= S_YELLOW`(把这一段缩短,让行人能过)。这就把 Moore 机变得有点 Mealy 味 —— 输出 `yellow` 现在依赖 `state` **和** `ped`。
- **输出一个 `walk` 信号**,只有 `state = S_RED` 时才为高。新的 Moore 输出。
- **把 `HOLD` 做成 generic** —— 仿真用 7 没问题,但在 50 MHz 的 FPGA 上你需要好几亿个周期才能保持 1 秒。
