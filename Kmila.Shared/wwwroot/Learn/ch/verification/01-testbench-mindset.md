# 测试台思维

在之前的每一节课里，Kmila 就是你的测试台 —— 你把激励拖进 Ports 标签，Run 跑一遍设计，然后用眼睛看波形。学习阶段没问题。但专业数字设计不是这么做的。

## 真正的测试台是什么

**测试台**是另一个 VHDL entity，其唯一职责是驱动一个*被测设计*（design under test，DUT）。它没有端口，因为没有东西来驱动它；它把 DUT 声明为一个组件，把自己的内部信号连到 DUT 的端口，然后用一个**进程**在时间上演绎激励序列。

骨架：

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
            report "Counter should be 10 at 200 ns, got " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

三个新概念：

1. **`wait`** —— 只有当进程说话时时间才流逝。
2. **`assert … report … severity`** —— 自检。把一千行波形变成"通过"或"失败"。
3. **零端口 entity** —— 测试台是自包含的；仿真器直接实例化它们。

## 这为什么重要

- **回归**：你昨天写的测试台今天照样跑。你昨天盯着看过的波形已经消失了。
- **覆盖率**：你可以驱动交互式会话永远到不了的激励模式（随机输入、边界情况、角落频率）。
- **自动化**：CI 系统在每次提交时都能跑你的测试台。波形进不了 CI。

## Kmila 目前的局限

只要你把测试台写成普通 entity，Kmila 仿真器运行它们完全没问题 —— 目前唯一缺少的是把 `assert` 语句的通过/失败自动抽取到 Output 日志。这件事在路线图上。

*更多课程即将推出 —— 本模块正在活跃编写中。*
