# 测试台思维

到这里为止的每一课里,Kmila 一直就是你的测试台——你把激励拖进 Ports 标签页,Run 跑设计,然后你用眼睛盯波形。学习阶段这样挺好。但专业的数字设计不是这么干的。

## 真正的测试台是什么

一个**测试台**是另一个 VHDL 实体,它唯一的工作就是去激励一个*待测设计*(DUT)。它没有端口,因为没有任何东西在驱动它;它把 DUT 声明成一个 component,把自己内部的信号接到 DUT 的端口上,然后用一个 **process** 在时间上走完一个个激励序列。

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

1. **`wait`** —— 只有当一个进程说"走"时,时间才会前进。
2. **`assert … report … severity`** —— 自检。把 1000 行的波形变成"pass"或"fail"。
3. **零端口实体** —— 测试台是自包含的;仿真器直接实例化它们。

## 产生一个时钟

那一行,`clk <= not clk after 10 ns;`,就是整个时钟:它每 10 ns 翻转一次,给出一个 20 ns 的周期(50 MHz)。因为 `clk` 有一个初始值 `'0'`,它起步很干净。要一个非 50% 占空比的时钟,或者一个你后面需要停下来的时钟,就改用一个进程:

```vhdl
clk_gen : process
begin
    clk <= '0'; wait for 10 ns;
    clk <= '1'; wait for 10 ns;
    if done then wait; end if;   -- 测试结束后冻住
end process;
```

把时钟产生和激励放在**分开的**进程里。时钟只管滴答;激励进程驱动输入并检查输出,通常用 `wait until rising_edge(clk)` 来和 DUT 对齐。

## 自检:assert、severity 和黄金值

测试台的意义在于它检查*它自己*。`assert` 对一个条件求值,如果为假,就以你选的 `severity` 打印那个 `report` 字符串。四个严重级别是 **note**、**warning**、**error** 和 **failure**——而且大多数仿真器可以被告知,一旦某个给定级别的 assert 触发就立刻停下。这就是你怎么把"看起来对"变成一个硬性的 pass/fail。

对于不那么平凡的设计,别去手写每一个期望值。用一个**黄金模型**把它算出来——一个简单、明显正确的参考(哪怕只是测试台里一段朴素的软件式计算)——然后每个周期都拿 DUT 和它比。那种"和参考比对"正是**计分板(scoreboard)**背后的核心思想,而且它从一个计数器一路扩展到一颗 CPU。

```vhdl
expected <= std_logic_vector(unsigned(expected) + 1) when rising_edge(clk);
assert q = expected
    report "mismatch: got " & to_string(q) & " expected " & to_string(expected)
    severity error;
```

(`std_logic_vector` 的 `to_string` 是 VHDL-2008 的一个便利——用 2008 模式编译测试台才能用它。)

## 干净地结束仿真

一个只看波形的会话会永远跑下去。一个自检的测试台应该在激励用尽时**自己停下来**。在 VHDL-2008 里,干净的办法是用 `std.env` package 里的 `finish`:

```vhdl
use std.env.all;
...
report "All tests passed";
finish;          -- 结束仿真;stop 则会暂停下来供检查
```

`finish` 结束整次运行(自动化/CI 用最理想);`stop` 把它暂停下来,好让你交互式地翻看。那个老办法——强行用 `assert … severity failure` 来停机——在任何 VHDL 版本里也管用,但它即使在通过的运行上也会报一个 *failure*,这会把回归工具搞糊涂——所以首选 `finish`。

## 定向激励 vs 随机激励

- **定向**测试打的是具体的、手工挑选的情形:复位行为、计数器的回绕、一段已知棘手的序列。拿来钉住一个 bug、以及覆盖规格里的验收用例,都很棒。
- **随机 / 约束随机**测试往 DUT 上扔许多合法但没预先计划的输入,抓住那些你永远想不到去写的角落情形。它们倚仗黄金模型,因为你没法手工预测每一个结果。

大多数真实验证两者兼用:定向测试覆盖你知道要紧的情形,随机测试覆盖你不知道的那些。

## 为什么这重要

- **回归:**你昨天写的测试台今天还能跑。你昨天盯过的那张波形已经没了。
- **覆盖率:**你能驱动交互会话永远到不了的激励模式(随机输入、边界情况、角落频率)。
- **自动化:**一个 CI 系统在每次提交时跑你的测试台,并从退出码读出 pass/fail。波形塞不进 CI。

## 与课程的联系

验证是整个*数字系统设计*实验序列里沉默的骨架。每一个实践——寄存器、时序系统、计数器、存储器、ASM 机——其实都是"用 HDL 把它搭出来,然后证明它能用"。一个自检的测试台,正是你怎么把每一份*实践报告*从"这是一张波形截图"变成"这是一个通过了的测试"的办法,而这恰恰是这个单元以问题为导向的考核所追求的那份严谨。

## Kmila 当前的局限

只要你把测试台写成普通的实体,Kmila 仿真器就能很好地跑它们——如今唯一缺的,是把 `assert` 语句的 pass/fail 自动抽取到输出日志里。这在路线图上。

## 要点

- 一个测试台是一个无端口的实体,它实例化 DUT、产生时钟、驱动激励,并用 `assert` **检查它自己**。
- 把时钟和激励放在分开的进程里;用 `wait until rising_edge(clk)` 让检查和时钟同步。
- 和一个黄金模型(计分板)比对,别去手写每一个期望值。
- 用 `std.env.finish` 结束自动化运行;用好严重级别,让仿真器能在真正的失败上停机。
- 定向测试(已知情形)和随机测试(你永远猜不到的角落)兼用。
