# 测试台思维

到目前为止的每节课里,Kmila 一直就是你的测试台 —— 你把激励拖进 Ports 标签页,Run 跑设计,然后你用眼睛盯波形。学习阶段这样很好。但专业的数字设计不是这样工作的。

## 真正的测试台是什么

**测试台**是另一个 VHDL entity,它唯一的工作就是去激励一个*待测设计*(DUT)。它没有端口,因为没人在驱动它;它把 DUT 声明成 component,把自己内部的信号接到 DUT 的端口上,然后用一个 **process** 在时间上推进激励序列。

骨架:

```vhdl
entity tb_counter is
end entity tb_counter;

architecture sim of tb_counter is
    component counter
        port ( clk, reset : in  std_logic;
               q          : out std_logic_vector(3 downto 0) );
    end component;

    signal clk   : std_logic := '0';
    signal reset : std_logic := '1';
    signal q     : std_logic_vector(3 downto 0);
begin
    dut : counter port map (clk, reset, q);

    clk <= not clk after 10 ns;            -- 50 MHz

    process
    begin
        wait for 20 ns;
        reset <= '0';
        wait for 200 ns;
        assert q = "1010"
            report "200 ns 时计数器应为 10,实际得到 " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

三个新概念:

1. **`wait`** —— 只有 process 说 “走" 时间才会前进。
2. **`assert … report … severity`** —— 自检。把 1000 行的波形浓缩成 “pass" 或 “fail"。
3. **没有端口的 entity** —— 测试台是自包含的;仿真器直接实例化它们。

## 为什么重要

- **回归**:你昨天写的测试台今天还能跑。你昨天盯着的波形已经没了。
- **覆盖率**:你能驱动交互会话永远到不了的激励模式(随机输入、边界情况、角频)。
- **自动化**:CI 系统在每次提交时都跑你的测试台。波形塞不进 CI。

## Kmila 当前的局限

Kmila 仿真器能很好地跑测试台,只要你把它们写成普通 entity —— 现在唯一缺的是把 `assert` 的 pass/fail 自动抽到输出日志里。这在 roadmap 上。

*更多课程将至 —— 本模块正在积极编写中。*
