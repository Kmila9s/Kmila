# 示例：推断得到的同步 ROM

每一颗 FPGA 都有**块 RAM**（BRAM）—— 专用的存储块，综合器在识别出 ROM 或 RAM 模式时会使用它们。你不用实例化厂商的原语，只要写出工具能识别的标准 VHDL 形状即可。

本示例是可移植的、教科书式的 ROM 模式。每一个正经的综合器（Xilinx Vivado、Intel Quartus、Lattice Radiant、Yosys）都能识别并自动分配 BRAM。

## 模式

```vhdl
type rom_t is array(0 to 15) of std_logic_vector(7 downto 0);
constant ROM : rom_t := (
  x"00", x"01", x"04", x"09", x"10", x"19", x"24", x"31",
  x"40", x"51", x"64", x"79", x"90", x"A9", x"C4", x"E1"
);

process(clk)
begin
  if rising_edge(clk) then
    data <= ROM(to_integer(unsigned(addr)));
  end if;
end process;
```

三件事让这个 ROM 适合映射到 BRAM：

1. **存储是一个常量**（`constant ROM : rom_t := (...)`）。数据在综合时固定。
2. **读是时钟驱动的。** 组合式 ROM 读只能映射到 LUT —— 对于只有 16 项的迷你 ROM 没问题，但只要更大就需要时钟化模式，综合器才会分配 BRAM。
3. **`addr` 纯粹用作索引。** `addr` 与存储器查找之间没有代价昂贵的组合逻辑。

## 为什么是这些内容？

示例中的 ROM 保存了 `n²`（`n = 0..15`）。适合一个用查表实现的平方单元 —— 当你只需要小操作数时，它比完整乘法器更快。你也可以放：

- 正弦波表（用于 DDS 信号发生器）
- CRC 多项式
- 协议查找表
- 字符字体

## 示例做了什么

仿真驱动 `addr` 经过 5 个特定值：0、1、4、8、15。时钟沿之后，`data` 会在下一个周期显示对应的 ROM 项（这就是同步 ROM 的读延迟 —— 你这一拍给地址，下一拍才拿到数据）。

## 轮到你了

- **参数化。** 用 `generic (DEPTH : integer := 16; WIDTH : integer := 8)` 替换固定的 16 项规模。其余代码应无需改动即可工作。
- **从文件读内容。** 真实项目会通过 `init_from_file()` 或单独的常量 package 加载 ROM 内容。试试让 VHDL 在精化时通过 `textio` + `hread` 读一个十六进制文本文件。
- **加一个第二读端口。** 用于单周期 CPU 中的指令/数据存储器这类场景。你会在后续课程的 CPU 项目中看到这种模式。
