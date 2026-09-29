# 示例:写优先读策略的单端口 RAM

读写存储是 “推断 BRAM” 的另一半。还是用同样的 `type…array…of` 写法,只是数组现在变成了 **signal**,在某些时钟沿写、另一些时钟沿读。

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

任何同步 RAM 都必须决定**写操作期间** `dout` 该怎么办。三种约定:

- **写优先(write-first,上方所示):** 写入时 `dout` 显示新写入的数据。语义最干净 —— 同周期对同地址的读写都拿到新值。
- **读优先(read-first):** 写入时 `dout` 显示该地址**旧的**内容。一些 CPU 的寄存器堆会这样。
- **不变(no-change):** 写入时 `dout` 不更新。一些低功耗设计。

三种都能映射到任何现代 FPGA 的 block RAM,但综合器会根据你 process 的精确写法选择模式。write-first 模板最便携 —— 拿不准就用它。

## 示例做了什么

前 30 µs,仿真往三个地址(`0`、`1`、`2`)写入三种不同的字节模式。然后 `we` 掉,在 40 µs 把 `addr` 重置回 0。Waveforms 标签页里 `dout` 在写期间跟随 `din`,然后在读时把存储的值送出来。

## 轮到你

- **升级成真正的双端口 RAM**。两套完整的 `addr`/`din`/`dout`/`we`,都用 `clk` 驱动。这是各厂商 FIFO 和双时钟 DPRAM 底层都在用的模式。
- **加一个输出寄存器**。Xilinx 的 block RAM 支持在存储阵列之后再加一级流水寄存器。在第二个时钟块里加 `dout_reg <= dout;`,看看波形上有什么变化(提示:多一个周期延迟,但时序闭合好得多)。
- **改初始内容**。改 `signal mem` 的初始值,把前几项预置成可识别的模式。并非所有综合器都尊重便携 VHDL 的 BRAM 初始化 —— 在生产里依赖它前先确认目标平台。
