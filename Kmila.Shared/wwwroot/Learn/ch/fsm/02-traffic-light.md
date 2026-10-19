# 示例:交通灯 FSM

交通灯是教科书式的 Moore FSM:三个状态(`RED`、`GREEN`、`YELLOW`),每种颜色一个输出,还有一个计时器说"每个状态先保持几拍再往下走"。

## 结构

这个示例遵循**双进程模式**:

- 进程 1 是**状态寄存器**——一个经典的 D 触发器,保存 `state` 并推动一个保持计数器。
- 进程 2 是**次态逻辑**——纯组合,根据当前状态决定 `next_state` 是什么。
- 输出赋值(在 architecture 底部)是组合的 Moore 输出——它们的值只依赖 `state`。

## 走一遍代码

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 每色 7 拍
```

`state_t` 是一个**枚举**——综合器会去挑真正的二进制编码,我们只引用命名的状态。`count` 跟踪我们在当前状态待了多久。`HOLD` 是驻留时长。

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

有两个细节值得点名。第一,这是一个**异步复位**(`reset` 在敏感列表里,而且在 `rising_edge` 之前就被判断)——`reset` 一拉起,灯就立刻跳到 `S_RED`。同步复位则会把 `reset` 从敏感列表里拿掉,只在 `rising_edge` 分支内部判断它;两者都合法,挑一种风格并保持一致就行。第二,计数器把一次状态转移拉成了 8 个周期的驻留——状态只在 `count = HOLD` 的那个周期才前进。

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

因为这个进程在每个分支上都给 `next_state` 赋了值,而且 `case` 对 `state_t` 是穷举的,所以这里不会推断出锁存器。敏感列表只有 `(state)`——这是对的,因为 `next_state` 不依赖别的任何东西。一旦你让次态依赖某个输入,那个输入就必须加进敏感列表(否则这个组合进程仿真会错),而且你应当在第一行加一个 `next_state <= state;` 默认值作为保险。

输出(纯 Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

每个输出单独是 `state` 的函数——所以它们只在寄存器跳变时才变,且在时钟沿之间不会出毛刺。这正是 Moore 机对于驱动一个物理信号这种事的全部魅力所在。

## 示例做了什么

时钟跑大约 400 µs。每 8 拍(7 拍保持 + 1 拍转移),FSM 前进一步。你会看到三个方波依次轮转:`red` 拉高 8 拍,然后 `green` 8 拍,然后 `yellow`,再回到 `red`。

## 从仿真到真实的路口

`HOLD = 7` 是为了仿真方便。在 50 MHz 的 FPGA 上,一拍是 20 ns,七拍就是 140 ns——这不是红绿灯,是频闪灯。要得到真实的 1 秒驻留,你得数到 50 000 000,这意味着要加宽 `count` 和 `HOLD` 常量。干净的办法是用一个 **generic**:

```vhdl
generic ( HOLD : natural := 7 );
...
signal count : unsigned(31 downto 0);
```

仿真时用一个很小的 `HOLD`,综合时用一个很大的——同一份 RTL,不用改一个字。真实的路口还会在相互冲突的绿灯之间加一个**全红安全状态**;加上它,是一个在不破坏 Moore 机无毛刺保证的前提下扩展它的好练习。

## 轮到你

- **加一个行人请求输入。**当 `ped = '1'` 且状态为 `S_GREEN` 时,强制次态为 `S_YELLOW`(把这一段缩短,好让行人通过)。这就把 Moore 机变得有点 Mealy 味了——次态现在同时依赖 `state` **和** `ped` 输入,所以要把 `ped` 加进敏感列表,并加一个默认赋值。
- **发出一个 `walk` 信号**,只有当 `state = S_RED` 时才为高。这是一个新的 Moore 输出。
- **把 `HOLD` 参数化**成一个 generic——仿真用 7 没问题,但在 50 MHz 的 FPGA 上,你需要 5000 万个周期才能得到 1 秒的保持。

## 与课程的联系

这个示例是第二单元*自动机与硬件描述语言的应用*的微缩版:一个确定性 Moore 自动机,先画成三状态图,再用 HDL 描述。那个把每个状态拉成定时驻留的计数器,也是你第一次尝到第三单元(计数器)活在状态机内部的滋味。

## 要点

- 交通灯是教科书式的 Moore FSM:输出只依赖 `state`,所以无毛刺。
- 双进程的拆分(时钟寄存器 + 组合次态)让设计保持可读、无锁存器。
- 状态寄存器内部的一个计数器把一次转移变成了定时驻留;用 generic 把驻留时长参数化,同一份 RTL 就能同时服务仿真和硅片。
- 一旦某个输入影响了次态,就把它加进敏感列表并给 `next_state` 赋默认值。
