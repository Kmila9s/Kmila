# 项目:UART 发送器

UART(通用异步收发器)是大多数嵌入式设计者构建的第一个真实协议。点对点、全双工,只需要三根线(TX、RX、GND)。每个 FTDI USB-串口适配器、每条 Arduino 的 `Serial.print`、每个老旧终端,底层都是 UART。

这个项目构建**发送**端。完成之后,接收端是一个绝佳的下一个项目。

## 帧格式(无校验、8 位数据、1 位停止)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    线路平时为高('1');起始位是一个下降沿。
```

每个 bit 在 `tx` 上保持恰好 `clk_freq / baud` 个时钟周期。50 MHz 时钟、115 200 波特,大约是每 bit 434 个周期。

## 你要做的

1. 构建一个时钟计数器,每 ~434 个周期产生一个单周期 `baud_tick`。
2. 当 `send = '1'` 时,把 `data` 锁进一个 10 位移位寄存器:`{ stop_bit, data[7..0], start_bit } = 1 & data & 0`。驱动 `busy <= '1'`。
3. 每个 `baud_tick` 把寄存器右移(起始位先出去,然后 LSB→MSB 的数据,最后停止位)。
4. 第 10 个 tick 之后,把 `busy` 拉低,`tx` 回到 idle 的高电平。

## 脚手架

entity 和空闲状态已经给你;`tx` 默认高,`busy` 默认低。介于这两个默认之间的所有事情都归你。

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## 建议结构

两个计数器 + 一个移位寄存器:

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- 当 `send = '1'` 且 `sending = '0'`:
  - 加载 `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- 当 `sending = '1'`:
  - `clk_count <= clk_count + 1;`
  - 如果 `clk_count = BAUD_PERIOD - 1`:
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` —— 右移,顶端用 idle 补齐
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - 如果 `bit_count = 10`:
    - `sending <= '0';`

把 `BAUD_PERIOD` 做成 generic,这样仿真时可以调小(比如 5 周期/bit),硬件上同一段逻辑仍能跑满速。

## 要测试什么

用 `data = "01001000"`(ASCII `'H'`)驱动 `send = '1'`。帧结束后,`tx` 波形应当显示恰好 10 个 bit-period 的活动:

1. 起始(低)
2. data 的 bit 0(LSB:`0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0`(MSB)
10. 停止(高)

之后回到 idle 高。放大到 Waveforms 标签页,逐 bit 验证模式。

## 再进一步

- 加一个 **FIFO**,这样你可以排队多个字节;`send` 变成 "push"。
- 构建 **RX** 端:第一位在 1.5 × bit 周期采样,后面的在 1 × 采样。
- 把帧参数化:校验、7 位 vs 8 位数据、1 vs 2 停止位。
