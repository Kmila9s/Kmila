# ROM 存储器

**存储器**保存可以按**地址**读回的字。ROM 内容固定；每个地址选中一个字，出现在输出端。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/25-memory.html" title="交互式图示" loading="lazy"></iframe></div>

地址推杆选中一个单元；它所存的字点亮输出指示灯，表里显示每个地址 → 数据。

在 VHDL 中：

```vhdl
type rom_t is array(0 to 3) of unsigned(2 downto 0);
constant ROM : rom_t := ("101","011","110","001");
...
data <= ROM(to_integer(addr));  -- addressed read
```

读取就是纯寻址——每个输出位的 ROM 实质上就是一个 N 选 1 的 MUX。RAM 多了写入通路；块 RAM（BRAM）是 FPGA 内置的专用存储器。
