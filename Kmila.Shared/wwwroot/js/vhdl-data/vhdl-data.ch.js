// VHDL 语言文档 — 中文（简体）。
// 与 vhdl-data.en.js 结构相同。

(function () {
    window.kmilaVhdlDocs = window.kmilaVhdlDocs || {};
    window.kmilaVhdlDocs.ch = {

        keywords: {
            // --- 结构性声明 ---
            entity:       { summary: "声明硬件模块的对外接口。", description: "**entity** 命名一个设计单元，并列出它的端口（输入/输出）和泛型（参数）。它是黑盒视图 — 实现由 architecture 提供。", example: "entity counter is\n  port (\n    clk : in  std_logic;\n    q   : out std_logic_vector(7 downto 0)\n  );\nend entity counter;" },
            architecture: { summary: "entity 的实现主体。", description: "**architecture** 为 entity 提供具体实现。一个 entity 可以有多个 architecture（例如 `behavioral`、`rtl`、`structural`）。", example: "architecture behavioral of counter is\n  signal cnt : unsigned(7 downto 0) := (others => '0');\nbegin\n  process(clk)\n  begin\n    if rising_edge(clk) then cnt <= cnt + 1; end if;\n  end process;\n  q <= std_logic_vector(cnt);\nend architecture behavioral;" },
            component:    { summary: "可实例化的 entity 接口副本。", description: "**component** 声明允许 architecture 实例化另一个 entity。在现代直接实体实例化出现之前是必须的。", example: "component fifo is\n  port ( clk : in std_logic; din : in std_logic_vector(7 downto 0) );\nend component;" },
            configuration:{ summary: "把组件绑定到特定的 entity/architecture 对。", description: "在可综合代码中很少使用；多用于一个 entity 存在多个 architecture 的仿真场景。" },
            package:      { summary: "可复用的类型、常量与子程序集合。", description: "**package** 把可以通过 `use` 导入到其他设计单元的声明聚集到一起。", example: "package my_pkg is\n  constant WIDTH : integer := 8;\n  type state_t is (idle, busy, done);\nend package my_pkg;" },
            body:         { summary: "package 的实现部分。", description: "`package body` 包含 `package` 中声明的子程序的实现。" },

            // --- 库与 use 子句 ---
            library:      { summary: "使一个库可见。", description: "导入一个 VHDL 库。`ieee` 几乎总是你想要的，用来取得 `std_logic_1164` 和 `numeric_std`。", example: "library ieee;" },
            use:          { summary: "使 package 的内容可见。", description: "从 package 导入名称，使用时无需限定。", example: "use ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;" },

            // --- 端口与泛型 ---
            port:         { summary: "声明 entity 的端口（跨越边界的信号）。", description: "出现在 entity 内部。每个端口有名称、方向（`in`、`out`、`inout`、`buffer`）和类型。", example: "port (\n  clk  : in  std_logic;\n  data : out std_logic_vector(7 downto 0)\n);" },
            generic:      { summary: "为 entity 声明编译期参数。", description: "泛型类似模板参数 — 实例化方选择其值，设计据此可缩放（总线宽度、FIFO 深度等）。", example: "generic ( WIDTH : integer := 8 );" },
            map:          { summary: "把实例的端口/泛型连接到周围的信号。", description: "组件实例化时使用 `port map (...)` 与 `generic map (...)`。", example: "u_counter : counter port map (\n  clk => sys_clk,\n  q   => led\n);" },
            in:           { summary: "端口方向 — 仅输入。", description: "设计读取该端口；不能驱动它。" },
            out:          { summary: "端口方向 — 仅输出。", description: "设计驱动该端口；其值不能在同一 entity 内回读（如需回读，可使用 `buffer` 或内部信号）。" },
            inout:        { summary: "双向端口。", description: "在同一物理线上既驱动又读取。同一时刻只能有一个驱动者激活 — 其余必须为 `'Z'`（高阻）。" },
            buffer:       { summary: "可在内部读取的输出。", description: "类似 `out`，但在 entity 内部也可读。被视为过时 — 推荐使用 `out` 配合内部信号。" },

            // --- 信号声明 ---
            signal:       { summary: "声明一个信号（连线）。", description: "**signal** 在时间上传递值。在 process 内，信号赋值（`<=`）会调度更新而不是立即生效。", example: "signal counter : unsigned(7 downto 0) := (others => '0');" },
            variable:     { summary: "声明一个变量（process 局部）。", description: "**variable** 存在于 process 或子程序内。与 signal 不同，`:=` 立即赋值 — 适合 process 内的中间计算。", example: "process\n  variable tmp : integer := 0;\nbegin\n  tmp := tmp + 1;\nend process;" },
            constant:     { summary: "声明一个具名常量。", description: "为固定值起一个易读的名字。可以出现在任何作用域。", example: "constant CLOCK_HZ : integer := 50_000_000;" },
            shared:       { summary: "可被多个进程访问的共享变量。", description: "仅在 VHDL-2008 下有意义。需谨慎使用 — 通常配合受保护类型或整数。" },

            // --- 类型系统 ---
            type:         { summary: "定义一个新类型。", description: "用于枚举、范围、数组、记录。", example: "type state_t is (idle, run, done);" },
            subtype:      { summary: "声明一个对已有类型加约束的子类型。", description: "添加范围或解析约束，但不创建全新类型。", example: "subtype byte is std_logic_vector(7 downto 0);" },
            array:        { summary: "声明一个数组类型。", description: "可以是一维或多维。可以是受约束的（固定大小）或非约束的（在使用时确定大小）。", example: "type memory_t is array(0 to 255) of std_logic_vector(7 downto 0);" },
            record:       { summary: "声明带具名字段的复合类型。", description: "VHDL 中相当于 C 的 `struct`。", example: "type axi_cmd_t is record\n  addr : std_logic_vector(31 downto 0);\n  len  : unsigned(7 downto 0);\nend record;" },
            range:        { summary: "标量类型的范围约束。", description: "通常 `A to B`（升序）或 `A downto B`（降序）。也用于 for 循环。", example: "for i in 0 to 7 loop ... end loop;" },
            downto:       { summary: "降序范围方向。", description: "`MSB downto LSB` — 声明向量下标的标准约定，使第 7 位在左。", example: "std_logic_vector(7 downto 0)" },
            to:           { summary: "升序范围方向。", description: "`A to B` — 用于循环和向上索引的数组。", example: "for i in 0 to 15 loop ... end loop;" },
            others:       { summary: "聚合默认值的通配符。", description: "为聚合中所有未命名位置赋值。", example: "signal zeros : std_logic_vector(7 downto 0) := (others => '0');" },
            units:        { summary: "声明物理类型的单位。", description: "在定义自定义物理类型（如 `time`）时使用。" },
            access:       { summary: "类似指针的访问类型。", description: "通常不可综合 — 在仿真中用于链表数据结构。" },
            file:         { summary: "仿真期间用于 I/O 的文件类型。", description: "不可综合。在测试平台中常用于读取激励文件。" },

            // --- 顺序语句 / 控制流 ---
            process:      { summary: "顺序执行的语句块。", description: "**process** 顺序执行其语句，并在敏感列表中的信号变化时重新运行。时序逻辑的基本单元。", example: "process(clk, reset)\nbegin\n  if reset = '1' then\n    q <= (others => '0');\n  elsif rising_edge(clk) then\n    q <= d;\n  end if;\nend process;" },
            if:           { summary: "条件执行。", description: "process 内的标准 `if … then … elsif … else … end if;` 结构。", example: "if rising_edge(clk) then\n  q <= d;\nend if;" },
            then:         { summary: "在 `if` / `elsif` 中分隔条件与主体。", description: "" },
            elsif:        { summary: "`if` 语句中的额外条件分支。", description: "" },
            else:         { summary: "`if` / `when` 的兜底分支。", description: "" },
            case:         { summary: "基于值的多路分支。", description: "类似 C 的 `switch`。常比一长串 `if`/`elsif` 更清晰。", example: "case state is\n  when idle => …\n  when busy => …\n  when others => null;\nend case;" },
            when:         { summary: "`case` 的分支选择，也用于条件信号赋值。", description: "", example: "q <= '1' when enable = '1' else '0';" },
            select:       { summary: "在选择信号赋值中的备选分支。", description: "并发的 `with … select` 语句，是经典的两种并发条件赋值之一。", example: "with state select\n  q <= a when idle,\n       b when busy,\n       c when others;" },
            loop:         { summary: "通用循环结构。", description: "可以是无条件（无限）、`for` 索引或 `while` 条件循环。", example: "for i in 0 to 7 loop\n  result(i) <= data(7 - i);\nend loop;" },
            for:          { summary: "索引循环。", description: "在范围上迭代。在 process 与 `generate` 块中都有用。", example: "for i in 0 to 15 loop … end loop;" },
            while:        { summary: "基于条件的循环。", description: "当条件依赖于信号时，往往不可综合。" },
            next:         { summary: "跳到循环的下一次迭代。", description: "" },
            exit:         { summary: "跳出循环。", description: "" },
            return:       { summary: "从 function 返回值或退出 procedure。", description: "" },
            wait:         { summary: "挂起 process。", description: "`wait on <信号>` / `wait until <条件>` / `wait for <时间>`。仅在特定形式下可综合（例如 `wait until rising_edge(clk)`）。", example: "wait until rising_edge(clk);" },
            null:         { summary: "空操作语句。", description: "在语法上需要语句的占位符。" },

            // --- 并发 / generate ---
            begin:        { summary: "语句块的起点。", description: "打开 `architecture`、`process`、`function`、`procedure`、`generate` 的主体。" },
            end:          { summary: "结束一个块。", description: "与 `entity`、`architecture`、`process`、`if`、`case`、`loop` 等成对出现。" },
            generate:     { summary: "条件性或循环式地产生硬件。", description: "可以视为编译期的 for 循环或 if，输出复制或条件性的结构。", example: "gen_bits : for i in 0 to 7 generate\n  q(i) <= d(7 - i);\nend generate;" },
            block:        { summary: "带作用域的并发语句区域。", description: "在守卫条件下分组语句。在现代代码中较少使用。" },
            assert:       { summary: "仿真期间的检查。", description: "若条件为假则触发 `report`，并根据 severity 决定是否停止。不可综合。", example: "assert data'length = 8 report \"宽度错误\" severity error;" },
            report:       { summary: "输出仿真消息。", description: "通常与 `assert` 搭配。Severity: `note`、`warning`、`error`、`failure`。" },
            severity:     { summary: "`assert`/`report` 的严重程度等级。", description: "" },

            // --- 子程序 ---
            function:     { summary: "返回值的纯子程序。", description: "根据参数计算一个值 — 对信号没有副作用。", example: "function max(a, b : integer) return integer is\nbegin\n  if a > b then return a; else return b; end if;\nend function;" },
            procedure:    { summary: "可以驱动信号的子程序。", description: "与 function 不同，procedure 可以拥有 `out`/`inout` 参数。" },
            impure:       { summary: "标记可以依赖外部状态的 function。", description: "允许读取声明在其作用域之外的信号。" },
            pure:         { summary: "标记没有外部依赖的 function。", description: "`function` 的默认值 — 极少显式书写。" },
            open:         { summary: "把端口保持未连接，或打开文件。", description: "在 port map 中: `some_port => open;`。文件 I/O: `file F : text open read_mode is ...;`" },

            // --- 运算符（关键字） ---
            and:          { summary: "逻辑与。", description: "对 std_logic_vector / bit_vector 按位执行。" },
            or:           { summary: "逻辑或。", description: "对向量按位执行。" },
            not:          { summary: "逻辑非。", description: "一元取反。" },
            nand:         { summary: "逻辑与非。", description: "" },
            nor:          { summary: "逻辑或非。", description: "" },
            xor:          { summary: "逻辑异或。", description: "" },
            xnor:         { summary: "逻辑同或。", description: "" },
            mod:          { summary: "取模 (modulo)。", description: "结果与除数同号（与 `rem` 不同）。" },
            rem:          { summary: "取余 (remainder)。", description: "结果与被除数同号。" },
            abs:          { summary: "绝对值。", description: "" },
            sla:          { summary: "算术左移。", description: "" },
            sra:          { summary: "算术右移。", description: "" },
            sll:          { summary: "逻辑左移。", description: "" },
            srl:          { summary: "逻辑右移。", description: "" },
            rol:          { summary: "向左循环移位。", description: "" },
            ror:          { summary: "向右循环移位。", description: "" },

            // --- 杂项 ---
            is:           { summary: "分隔关键字。", description: "出现在 `entity X is`、`architecture Y of X is`、`type T is` 等位置。" },
            of:           { summary: "分隔关键字。", description: "出现在 `architecture A of X`、`array(…) of …`。" },
            all:          { summary: "通配符。", description: "用于 `use x.y.all;`，以及属性/敏感列表场景。" },
            new:          { summary: "为 access 类型（指针）对象分配空间。", description: "通常不可综合。" },
            label:        { summary: "为语句加上名字。", description: "对 process、generate 与循环很有用，方便后续引用。" },
            alias:        { summary: "给对象或对象的一部分起别名。", description: "", example: "alias sign : std_logic is data(7);" },
            attribute:    { summary: "元数据标签或预定义查询。", description: "查询如 `x'length`、`x'range`、`signal'event`。" },
            group:        { summary: "把具名项目分组以便应用属性。", description: "很少见。" },
            bus:          { summary: "过时的信号修饰符。", description: "历史遗留。" },
            register:     { summary: "过时的信号修饰符。", description: "历史遗留。" },
            disconnect:   { summary: "控制守卫信号的断开时间。", description: "历史遗留 / 罕见。" },
            after:        { summary: "传输延时说明符。", description: "`a <= b after 5 ns;` — 仅用于仿真的延时。" },
            reject:       { summary: "惯性延时的拒绝脉冲宽度。", description: "" },
            inertial:     { summary: "惯性延时（默认）。", description: "过滤掉短脉冲。" },
            transport:    { summary: "传输延时（不过滤）。", description: "" },
            guarded:      { summary: "限定一个 block 或信号赋值。", description: "罕见。" },
            linkage:      { summary: "用于跨语言连接的端口方向。", description: "仅在 VHDL/Verilog 混合流程中相关。" },
            postponed:    { summary: "把 process 或断言推迟到 delta 周期末。", description: "仅用于仿真。" },
            literal:      { summary: "与属性相关的关键字。", description: "罕见。" },
            unaffected:   { summary: "条件信号赋值中的“什么也不做”占位。", description: "" },
            until:        { summary: "与 `wait until <条件>` 配合使用。", description: "" },
            with:         { summary: "开始一个选择信号赋值。", description: "`with expr select …`。" },
            on:           { summary: "用于 `wait on <信号列表>`。", description: "" }
        },

        types: {
            std_logic:        { summary: "IEEE 9 值逻辑：一根连线。", description: "取值: `'0'`、`'1'`、`'Z'`（高阻）、`'X'`（未知）、`'U'`（未初始化）、`'W'`、`'L'`、`'H'`、`'-'`（don't-care）。综合中单比特信号的默认类型。", example: "signal clk : std_logic := '0';" },
            std_logic_vector: { summary: "std_logic 的向量 — 一条总线。", description: "用范围声明: `(high downto low)` 是标准约定。适合连线；做算术请转换为 `unsigned`/`signed`。", example: "signal data : std_logic_vector(7 downto 0);" },
            std_ulogic:       { summary: "std_logic 的未解析版本。", description: "只允许一个驱动者。直接使用很少。" },
            std_ulogic_vector:{ summary: "std_ulogic 的未解析向量。", description: "" },
            bit:              { summary: "简单的 2 值逻辑：`'0'` 或 `'1'`。", description: "比 std_logic 更早。综合中应使用 std_logic — bit 无法表示 Z、X 等。" },
            bit_vector:       { summary: "`bit` 的向量。", description: "" },
            boolean:          { summary: "Boolean 类型：`true` / `false`。", description: "用于条件；不能直接综合到一根连线（需转换为 std_logic）。" },
            integer:          { summary: "整数 — 至少 32 位有符号。", description: "适合计数器与下标。用子范围约束（如 `integer range 0 to 255`），让综合工具知道使用多少位。" },
            natural:          { summary: "`integer range 0 to integer'high`。", description: "非负整数。" },
            positive:         { summary: "`integer range 1 to integer'high`。", description: "严格正整数。" },
            real:             { summary: "浮点类型。", description: "不可综合 — 仅用于仿真/常量。" },
            character:        { summary: "ASCII / ISO-8859-1 字符。", description: "" },
            string:           { summary: "character 数组。", description: "主要用于 `report` 消息和文件 I/O。" },
            time:             { summary: "表示时间的物理类型。", description: "单位: `fs`、`ps`、`ns`、`us`、`ms`、`sec`、`min`、`hr`。仅用于仿真。" },
            signed:           { summary: "ieee.numeric_std 的数值向量（有符号）。", description: "用于有符号算术。从 std_logic_vector 通过 `signed()` 转换。", example: "signal x : signed(7 downto 0);" },
            unsigned:         { summary: "ieee.numeric_std 的数值向量（无符号）。", description: "用于无符号算术。通过 `unsigned()` / `std_logic_vector()` 转换。", example: "signal cnt : unsigned(7 downto 0) := (others => '0');" },
            line:             { summary: "对文本行的访问 — 用于文件 I/O。", description: "在测试平台中与 `readline` / `writeline` 配合使用。" },
            text:             { summary: "textio 包中的文件类型。", description: "" },
            severity_level:   { summary: "`assert`/`report` 的严重程度等级。", description: "取值: `note`、`warning`、`error`、`failure`。" },
            side:             { summary: "textio 辅助类型。", description: "" },
            width:            { summary: "textio 辅助类型。", description: "" }
        },

        builtins: {
            rising_edge:    { summary: "时钟从 0→1 跳变时为真。", signature: "rising_edge(s : std_ulogic) return boolean", description: "比 `clk'event and clk = '1'` 更推荐。对 std_logic 与 std_ulogic 都适用。", example: "if rising_edge(clk) then q <= d; end if;" },
            falling_edge:   { summary: "1→0 跳变时为真。", signature: "falling_edge(s : std_ulogic) return boolean", description: "`rising_edge` 的对应函数。" },
            to_integer:     { summary: "把 signed/unsigned 转为 integer。", signature: "to_integer(arg : unsigned) return natural", description: "来自 `ieee.numeric_std`。算术之后的常用转换。", example: "variable n : integer := to_integer(unsigned(data));" },
            to_unsigned:    { summary: "把 integer 转为 unsigned 向量。", signature: "to_unsigned(arg : natural; size : natural) return unsigned", description: "", example: "counter <= to_unsigned(42, 8);" },
            to_signed:      { summary: "把 integer 转为 signed 向量。", signature: "to_signed(arg : integer; size : natural) return signed", description: "" },
            resize:         { summary: "调整 signed/unsigned 向量的位宽。", signature: "resize(arg : unsigned; new_size : natural) return unsigned", description: "unsigned 用零扩展，signed 用符号扩展。" },
            shift_left:     { summary: "逻辑左移。", signature: "shift_left(arg : unsigned; count : natural) return unsigned", description: "右侧补零。" },
            shift_right:    { summary: "对 unsigned 是逻辑右移，对 signed 是算术右移。", signature: "shift_right(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_left:    { summary: "循环左移。", signature: "rotate_left(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_right:   { summary: "循环右移。", signature: "rotate_right(arg : unsigned; count : natural) return unsigned", description: "" },
            conv_integer:   { summary: "旧式整数转换。", signature: "conv_integer(arg : std_logic_vector) return integer", description: "来自 `ieee.std_logic_arith`。新代码请使用 numeric_std 的 `to_integer`。" },
            conv_std_logic_vector: { summary: "旧式向量转换。", signature: "conv_std_logic_vector(arg : integer; size : integer) return std_logic_vector", description: "新代码请使用 `std_logic_vector(to_unsigned(...))`。" },
            now:            { summary: "当前仿真时间。", signature: "now : time", description: "仅用于仿真。" },
            readline:       { summary: "从 textio 文件读取一行。", signature: "readline(file f : text; l : out line)", description: "测试平台 I/O。" },
            writeline:      { summary: "向 textio 文件写入一行。", signature: "writeline(file f : text; l : inout line)", description: "测试平台 I/O。" },
            endline:        { summary: "判断行末。", signature: "endline(l : inout line) return boolean", description: "textio 辅助函数。" },
            endfile:        { summary: "判断文件末尾。", signature: "endfile(file f : text) return boolean", description: "" },
            write:          { summary: "textio 写入原语。", signature: "write(l : inout line; value : … )", description: "" },
            read:           { summary: "textio 读取原语。", signature: "read(l : inout line; value : out … )", description: "" }
        },

        snippets: {
            entity: {
                label: "entity（骨架）",
                summary: "插入最小的 entity 声明",
                insertText: "entity ${1:name} is\n\tport (\n\t\t${2:clk}   : in  std_logic;\n\t\t${3:reset} : in  std_logic;\n\t\t${4:q}     : out std_logic_vector(${5:7} downto 0)\n\t);\nend entity ${1:name};\n\narchitecture behavioral of ${1:name} is\nbegin\n\t$0\nend architecture behavioral;"
            },
            process_clocked: {
                label: "process（同步，异步 reset）",
                summary: "标准的同步 process 与异步 reset",
                insertText: "process(${1:clk}, ${2:reset})\nbegin\n\tif ${2:reset} = '1' then\n\t\t$3 <= (others => '0');\n\telsif rising_edge(${1:clk}) then\n\t\t$0\n\tend if;\nend process;"
            },
            process_combinational: {
                label: "process（组合）",
                summary: "带敏感列表的组合 process",
                insertText: "process(${1:all})\nbegin\n\t$0\nend process;"
            },
            if_else: {
                label: "if … elsif … else",
                summary: "三分支条件",
                insertText: "if ${1:condition} then\n\t$2\nelsif ${3:other} then\n\t$4\nelse\n\t$5\nend if;"
            },
            case_stmt: {
                label: "case … when … end case",
                summary: "case 语句骨架",
                insertText: "case ${1:expr} is\n\twhen ${2:value} => $3\n\twhen others    => $0\nend case;"
            },
            for_loop: {
                label: "for i in 0 to N-1 loop",
                summary: "索引循环",
                insertText: "for ${1:i} in 0 to ${2:N}-1 loop\n\t$0\nend loop;"
            },
            signal: {
                label: "signal 声明",
                summary: "声明一个信号",
                insertText: "signal ${1:name} : ${2:std_logic_vector}(${3:7} downto 0) := (others => '0');"
            },
            counter: {
                label: "计数器模板",
                summary: "环回计数器",
                insertText: "signal cnt : unsigned(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk}, ${3:reset})\nbegin\n\tif ${3:reset} = '1' then\n\t\tcnt <= (others => '0');\n\telsif rising_edge(${2:clk}) then\n\t\tcnt <= cnt + 1;\n\tend if;\nend process;\n${4:q} <= std_logic_vector(cnt);"
            },
            shift_register: {
                label: "移位寄存器模板",
                summary: "N 位移位寄存器",
                insertText: "signal sr : std_logic_vector(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk})\nbegin\n\tif rising_edge(${2:clk}) then\n\t\tsr <= sr(${1:7}-1 downto 0) & ${3:d};\n\tend if;\nend process;\n${4:q} <= sr;"
            },
            library_ieee: {
                label: "library ieee + use 子句",
                summary: "标准 IEEE 导入",
                insertText: "library ieee;\nuse ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;\n\n$0"
            }
        },

        ui: {
            declaredOnLine: "声明于第",
            kindSignal:     "信号",
            kindVariable:   "变量",
            kindConstant:   "常量",
            kindPort:       "端口",
            categoryKeyword:  "VHDL 关键字",
            categoryType:     "类型",
            categoryBuiltin:  "IEEE 函数",
            categoryUser:     "用户声明",
            categorySnippet:  "代码片段",
            translationPending: "⚠ 翻译未完成 — 描述以英文显示。"
        }
    };
})();
