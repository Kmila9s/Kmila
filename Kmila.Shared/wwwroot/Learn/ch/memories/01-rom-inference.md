# 示例:推断出的同步 ROM

每个 FPGA 都有 **block RAM**(BRAM)—— 综合器在你的代码中识别出 ROM 或 RAM 模式时使用的专用存储块。你不需要实例化厂商原语;你写普通的 VHDL,只要写成工具能识别的形式。

这个示例是可移植的教科书式 ROM 写法。任何正经的综合器(Xilinx Vivado、Intel Quartus、Lattice Radiant、Yosys)都会识别它并自动分配 BRAM。

## 这个写法

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

三件事让它成为 BRAM 友好的 ROM:

1. **存储是常量**(`constant ROM : rom_t := (...)`)。数据在综合时就固定。
2. **读取是时钟驱动的**。组合 ROM 读取只能映射到 LUT —— 16 项的小 ROM 还行,稍大就需要时钟读取,综合器才会给你分配 BRAM。
3. **`addr` 仅作为索引使用**。`addr` 与存储查表之间没有昂贵的组合逻辑。

## 为什么是这些内容?

示例 ROM 存的是 `n²`(`n = 0..15`)。在你只需要小操作数时,作为一个查表平方单元比完整乘法器更快。你也可以存:

- 一张正弦波表(用于 DDS 信号发生器)
- CRC 多项式
- 协议查表
- 字体

## 示例做了什么

仿真把 `addr` 驱动到 5 个特定值:0、1、4、8、15。在时钟沿之后一个周期,`data` 显示对应的 ROM 项(这就是同步 ROM 的读延迟 —— 你给地址,下一拍才拿到数据)。

## 轮到你

- **参数化它**。把固定 16 项替换成 `generic (DEPTH : integer := 16; WIDTH : integer := 8)`。其余代码应该照常工作。
- **从文件读内容**。真实项目通过 `init_from_file()` 或单独的常量 package 加载 ROM。试试在 elaboration 阶段用 `textio` + `hread` 读一个 hex 文本文件。
- **加第二个读端口**。比如单周期 CPU 里的指令/数据存储器。课程后面 CPU 项目里你会看到这种模式。
