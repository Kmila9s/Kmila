# 项目：UART 发送器

UART（通用异步收/发器，Universal Asynchronous Receiver / Transmitter）是多数嵌入式设计师动手构建的第一个真实协议。它是点对点、全双工，只需要三根线（TX、RX、GND）。每一个 FTDI USB-串口转换、每一次 Arduino 的 `Serial.print`、每一台老式终端，底下都是一个 UART。

本项目构建**发送**端。接收端在你完成这个之后，是一个极佳的后续项目。

## 帧格式（无校验、8 位数据、1 位停止）

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    线路通常为高（'1'）；起始位是下降沿。
```

每一位在 `tx` 上保持正好 `clk_freq / baud` 个时钟周期。对 50 MHz 时钟和 115 200 波特，大约是每位 434 个周期。

## 你的任务

1. 构建一个时钟计数器，每 ~434 个周期产生一个单周期的 `baud_tick`。
2. 当 `send = '1'` 时，把 `data` 锁存进一个 10 位移位寄存器：`{ stop_bit, data[7..0], start_bit } = 1 & data & 0`。把 `busy` 置为 `'1'`。
3. 每个 `baud_tick` 上，把寄存器向右移位（让起始位先出，然后是数据从 LSB 到 MSB，再到停止位）。
4. 第 10 个 tick 之后，把 `busy` 拉低，让 `tx` 空闲为高。

## 脚手架

entity 和空闲状态已提供；`tx` 默认高，`busy` 默认低。这些默认值之间的一切都由你负责。

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## 建议结构

两个计数器 + 一个移位寄存器：

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- 当 `send = '1'` 且 `sending = '0'` 时：
  - 加载 `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- 当 `sending = '1'` 时：
  - `clk_count <= clk_count + 1;`
  - 如果 `clk_count = BAUD_PERIOD - 1`：
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` —— 右移，高位用空闲电平填充
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - 如果 `bit_count = 10`：
    - `sending <= '0';`

把 `BAUD_PERIOD` 做成 generic，你就能在仿真中把它压缩（比如每位 5 个周期），同时在硬件上以全速仍然运行同一套逻辑。

## 要测试什么

以 `data = "01001000"`（ASCII `'H'`）驱动 `send = '1'`。帧发完后，`tx` 波形应显示恰好 10 个位周期的活动：

1. Start（低）
2. 数据的 Bit 0（也就是 LSB：`0`）
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0`（MSB）
10. Stop（高）

然后回到空闲高电平。在 Waveforms 标签里放大，逐位核对模式。

## 进阶

- 加一个 **FIFO**，让你能排队多个字节；`send` 变成"push"。
- 构建 **RX** 端：在 1.5 × 位周期处先采样，之后每 1 × 位周期采样一次。
- 把帧参数化：奇偶校验、7 位 vs 8 位数据、1 位 vs 2 位停止位。
