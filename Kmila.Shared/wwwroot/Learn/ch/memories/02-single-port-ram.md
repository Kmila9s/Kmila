# 示例:写优先读的单端口 RAM

读写存储器是"推断 BRAM"的另一半。还是同样的 `type…array…of` 写法,只是现在数组是一个 **signal**,你在某些时钟沿写、在另一些时钟沿读。

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

为什么用 **signal** 而不是 `variable` 或常量?这个数组必须是跨时钟沿持续存在、而且在运行时会被*重写*的共享状态——这恰恰就是信号的定义。常量(就像 ROM 示例里那样)不能被写;一个进程 `variable` 在进程外面不可见。而且因为每一次访问都在 `rising_edge(clk)` 内部,写和读都是**同步的**——这正是让工具能把整件事映射到一块 block RAM 的条件。

## 三种读策略

每一个同步 RAM 都必须决定:在对正在读的那个地址进行**写操作期间**,`dout` 该怎么办。这种"写期间读"的行为有三种标准约定,而你进程的形状决定了你得到哪一种:

- **写优先**(write-first,也叫 read-through,就是上面展示的):写入时,`dout` 显示新写入的数据。语义最干净——同一周期里对同一地址的读和写,给你的是新值。
- **读优先**(read-first):写入时,`dout` 显示该单元**旧的**内容。一些 CPU 的寄存器堆会用这种。
- **不变**(no-change):写入时,`dout` 根本不更新——它保持上一次的值。常见于低功耗设计,因为写操作不会翻转输出寄存器。

区别纯粹在于进程里那两条赋值的*顺序*:

```vhdl
-- 读优先:先取出旧数据,再覆盖
if we = '1' then
  dout <= mem(to_integer(unsigned(addr)));   -- 送出旧值
  mem(to_integer(unsigned(addr))) <= din;    -- 然后写入
end if;

-- 不变:只在不写的时候才更新 dout
if we = '1' then
  mem(to_integer(unsigned(addr))) <= din;    -- dout 保持不动
else
  dout <= mem(to_integer(unsigned(addr)));
end if;
```

这三种在任何现代 FPGA 上都映射到 block RAM,而且一个真双端口 RAM 的每个端口甚至可以用不同的模式。但综合器是从你进程的*确切*形状来挑模式的,所以要让模板和你真正想要的行为对上。写优先这种形式是最广泛可移植的——拿不准就写它。

## 示例做了什么

前 30 µs,仿真往三个地址(`0`、`1`、`2`)写三种不同的字节模式。然后 `we` 掉下来,在 40 µs 时 `addr` 被复位回 0。Waveforms 标签页里,你会看到 `dout` 在写期间跟着 `din` 走,然后在你读的时候把存着的值送出来——比你给出地址晚一个周期,因为读取是被寄存的。

## 要当心

- **地址位宽 vs 深度。**和 ROM 一样,`to_integer(unsigned(addr))` 必须留在 `0 .. 31` 以内。把 `addr` 定成 `log2(DEPTH)` 位,这样它就不可能越界索引。
- **别做组合读。**组合式的读取(`dout <= mem(...)` 写在时钟进程外面)通常会挡住 BRAM 推断,逼着数组进到触发器/LUT 里——几十个字节还行,几 KB 就是灾难。
- **初始化值不保证。**`signal mem := (others => ...)` 的初始值在仿真里有效,在许多 FPGA 上也有效(那里的 BRAM 有上电状态),但不是在所有目标上都有效,而且在真实的外部 SRAM/DRAM 上绝不会有——它们上电时是未定义的。在生产里别依赖它,除非你确认过你的器件。

## 轮到你

- **升级成一个真双端口 RAM。**两套完整的 `addr`/`din`/`dout`/`we`,都用 `clk` 驱动。这是每个厂商的 FIFO 和双时钟 DPRAM 底下用的模式。
- **加一个输出寄存器。**block RAM 支持在存储阵列之后再加一级流水寄存器。在第二个时钟块里加 `dout_reg <= dout;`,看看波形上有什么变化(提示:多一个周期的延迟,但时序收敛好得多)。
- **在它之上搭一个 FIFO。**用一个写指针、一个读指针,以及 `full`/`empty` 标志把这个 RAM 包起来。那就是同步 FIFO 的标准搭法,而且这个单元里就点名要它。

## 与课程的联系

这是第四单元*存储器*的核心:RAM / SRAM / DRAM,以及用 HDL 通过数组处理来设计 RAM、ROM 和 **FIFO** 存储器。写优先/读优先/不变,是真实 SRAM 单元所表现的"写期间读"行为在 HDL 里的那张脸;上面那个 FIFO 练习,正是这个单元列出的*特殊类型的存储器*。

## 要点

- 读写存储器 = 和 ROM 一样的数组写法,只不过是一个你在运行时会去写的 **signal**。
- 把读和写都放在 `rising_edge(clk)` 内部,工具才会推断出 block RAM。
- 写优先 / 读优先 / 不变,是由进程里语句的顺序决定的——挑你设计需要的那个;写优先是安全的默认选项。
- 留意地址位宽,避免组合读,别指望 BRAM 的初始化值在所有目标上都靠得住。
