# 项目:单周期 8 位 CPU

这是收官之作。你要把前面所有模块里出现过的东西 —— ROM、寄存器、ALU、像 FSM 的控制 —— 组装成一台真的能跑程序的小机器。

## 这台机器

```mermaid
flowchart TD
    RST(["reset = 0000"]) -.-> PC["PC (4 位)"]
    PC -- addr --> ROM["ROM<br/>16 × 8 字节<br/>(综合时预加载)"]
    ROM -- "instr (8 位)" --> SPLIT{{"opcode = instr(7..4)<br/>imm4 = instr(3..0)"}}
    SPLIT --> DECEXE["译码 / 执行"]
    SPLIT --> PCNXT["PC next 逻辑"]
    DECEXE --> R0["R0 (8 位)"]
    PCNXT --> PCN["PC_next"]
```

## 脚手架给了你什么

一颗完整、能跑的 CPU。仔细读起步代码:

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    pc   <= (others => '0');
    r0   <= (others => '0');
    halt <= '0';
  elsif rising_edge(clk) then
    if halt = '0' then
      case instr(7 downto 4) is
        when "0000" => r0 <= resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0001" => r0 <= r0 + resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0010" => pc <= unsigned(instr(3 downto 0));
        when "1111" => halt <= '1';
        when others => pc <= pc + 1;
      end case;
    end if;
  end if;
end process;
```

它已经实现了:

- LOAD (`0000`)
- ADD  (`0001`)
- JUMP (`0010`)
- HALT (`1111`)

预加载的 ROM 跑的是 `R0 = 3; R0 += 4; R0 += 2; HALT`。

## 你要做的

先让它原样跑起来。用配套的 clock + reset 激励仿真 200 µs。Waveforms 标签页里你应该看到 `r0_out` 走 `3 → 7 → 9`,然后 `halted` 拉高并一直保持。

基线跑通后,**扩展** ISA:

### 任务 1:SUB

opcode `0011`。`R0 <= R0 - imm4`。一行改动。

### 任务 2:条件跳转

opcode `0100`:`JZ addr` —— 如果 `R0 = 0` 就跳到 `addr`,否则 `pc <= pc + 1`。

写一段小程序,从某个值倒计数,到 0 时跳回开头。用 `JZ` 当循环出口。

### 任务 3:第二个寄存器

加一个 `r1` 作为第二个 8 位寄存器。把 `LOAD R1, #i` 定义为 opcode `0101`,把 `ADD R0, R1`(没有立即数)定义为 opcode `0110`。你需要扩译码 case,加第二个输出端口便于观察。

### 任务 4:外部数据存储器

把 Memories 那条线里的 RAM 模块接进这颗 CPU。加 `LOAD R0, [addr]` 和 `STORE R0, [addr]` opcode。当心:单周期设计要求存储器读端口的输出在你下地址的同一周期内有效 —— ROM 用组合读,数据 RAM 用带寄存器的读,流水线要仔细规划。

## 为什么要做这个?

因为当你第一次看到自己的 CPU 跑着自己写的程序(用自己写进 ROM 的字节),会有什么东西在脑子里咔哒一声。上面所有抽象 —— 汇编器、编译器、操作系统 —— 都建立在这同一个机制上。一直到上周浏览器里跑你写的 JavaScript,都是同一个把戏在更大的规模上重复。

## 再进一步

- 在同一 ISA 上实现经典的五级流水线。加冒险检测和旁路单元。你现在就有了 RISC-V 教科书所讲东西的微缩版。
- 给 ROM 换源:不再用常量,而是仿真时从外部文件加载,然后构建一条把文本汇编成那些字节的工具链。
- 把前面那个项目的 UART TX 接进来,让 CPU 在波形上打出 `'HI'`。恭喜 —— 你有了一颗 SoC。
