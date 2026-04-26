# 示例：写优先读的单端口 RAM

读写存储器是"推断 BRAM"的另一半。同样的 `type…array…of` 模式适用，只不过现在数组是一个**信号**，你在某些时钟沿上写、在另一些时钟沿上读。

## 模板

```vhdl
type mem_t is array(0 to 31) of std_logic_vector(7 downto 0);
signal mem : mem_t := (others => (others => '0'));
...
process(clk)
begin
  if rising_edge(clk) then
    if we = '1' then
      mem(to_integer(unsigned(addr))) <= din;
      dout <= din;                             -- 写优先读
    else
      dout <= mem(to_integer(unsigned(addr)));
    end if;
  end if;
end process;
```

## 三种读策略

每一个同步 RAM 都必须决定在一次写操作**期间** `dout` 该呈现什么。有三种约定：

- **写优先（如上所示）：** 在写操作期间，`dout` 显示刚刚写入的数据。语义最干净 —— 同一个周期里对同一地址的读和写能拿到新值。
- **读优先：** 在写操作期间，`dout` 显示该单元的**旧**内容。一些 CPU 的寄存器堆使用这种方式。
- **不变：** 在写操作期间，`dout` 保持不变。一些低功耗设计采用。

在任何现代 FPGA 上三种都会映射到块 RAM，但综合器会根据你进程的具体形状来选模式。写优先模板最具可移植性 —— 拿不准就写它。

## 示例做了什么

仿真前 30 µs 对三个地址（`0`、`1`、`2`）写入三种不同的字节模式。之后 `we` 变低，40 µs 时 `addr` 复位回 0。Waveforms 标签会显示：写入时 `dout` 跟随 `din`，之后读取时又把存下来的值吐出来。

## 轮到你了

- **升级为真正的双端口 RAM。** 两套完整的 `addr`/`din`/`dout`/`we`，都由 `clk` 驱动。厂商的 FIFO 和双时钟 DPRAM 底层都是这种模式。
- **加一个输出寄存器。** Xilinx 的块 RAM 支持在存储阵列之后再加一级流水寄存器。在另一个时钟块中加 `dout_reg <= dout;`，看看波形上会有什么变化（提示：多一个周期的延迟，但时序收敛会好得多）。
- **更改初始内容。** 修改 `signal mem` 的初始值，预装一些可辨识的模式。并非所有综合器都尊重可移植 VHDL 中的 BRAM 初始值 —— 在生产中依赖它之前先检查你的目标器件。
