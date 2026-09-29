// وثائق لغة VHDL — العربية.
// نفس بنية vhdl-data.en.js.

(function () {
    window.kmilaVhdlDocs = window.kmilaVhdlDocs || {};
    window.kmilaVhdlDocs.ar = {

        keywords: {
            // --- إعلانات هيكلية ---
            entity:       { summary: "تعلن عن الواجهة الخارجية لوحدة عتاد.", description: "إن **entity** تسمّي وحدة تصميم وتسرد منافذها (المداخل/المخارج) ومعمّماتها (المعاملات). هي العرض كصندوق أسود — أما architecture فتقدّم التنفيذ.", example: "entity counter is\n  port (\n    clk : in  std_logic;\n    q   : out std_logic_vector(7 downto 0)\n  );\nend entity counter;" },
            architecture: { summary: "جسم تنفيذ entity.", description: "إن **architecture** تقدّم تنفيذًا فعليًا لـ entity. يمكن أن تمتلك entity واحدة عدّة architectures (مثل `behavioral` أو `rtl` أو `structural`).", example: "architecture behavioral of counter is\n  signal cnt : unsigned(7 downto 0) := (others => '0');\nbegin\n  process(clk)\n  begin\n    if rising_edge(clk) then cnt <= cnt + 1; end if;\n  end process;\n  q <= std_logic_vector(cnt);\nend architecture behavioral;" },
            component:    { summary: "نسخة قابلة للتنصيب من واجهة entity.", description: "يسمح إعلان **component** لـ architecture بتنصيب entity أخرى. كان مطلوبًا قبل ظهور التنصيب المباشر للكيانات.", example: "component fifo is\n  port ( clk : in std_logic; din : in std_logic_vector(7 downto 0) );\nend component;" },
            configuration:{ summary: "يربط مكوّنًا بزوج محدد من entity/architecture.", description: "نادرًا ما يُستعمل في الشيفرة القابلة للتركيب؛ غالبًا في المحاكاة عند وجود عدة architectures لـ entity واحدة." },
            package:      { summary: "مجموعة قابلة لإعادة الاستخدام من الأنواع والثوابت والبرامج الفرعية.", description: "إن **package** يجمّع إعلانات يمكن استيرادها إلى وحدات تصميم أخرى عبر `use`.", example: "package my_pkg is\n  constant WIDTH : integer := 8;\n  type state_t is (idle, busy, done);\nend package my_pkg;" },
            body:         { summary: "الجزء التنفيذي من package.", description: "يحتوي `package body` على تنفيذات البرامج الفرعية المُعلَنة في `package`." },

            // --- المكتبات وعبارات use ---
            library:      { summary: "تجعل مكتبة مرئية.", description: "تستورد مكتبة VHDL. غالبًا ما يكون `ieee` ما تريده للحصول على `std_logic_1164` و `numeric_std`.", example: "library ieee;" },
            use:          { summary: "تجعل محتويات package مرئية.", description: "تستورد أسماء من package لاستخدامها بدون تأهيل.", example: "use ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;" },

            // --- المنافذ والمعمّمات ---
            port:         { summary: "تعلن منافذ entity (إشارات تعبر الحدود).", description: "تظهر داخل entity. لكل منفذ اسم واتجاه (`in` أو `out` أو `inout` أو `buffer`) ونوع.", example: "port (\n  clk  : in  std_logic;\n  data : out std_logic_vector(7 downto 0)\n);" },
            generic:      { summary: "تعلن معاملات وقت التصريف لـ entity.", description: "المعمّمات تشبه معاملات القوالب — يختار من ينصّب الكيان قيمتها، ويتقاس التصميم بناءً عليها (عرض الناقل، عمق FIFO، إلخ).", example: "generic ( WIDTH : integer := 8 );" },
            map:          { summary: "تربط منافذ/معمّمات النسخة بالإشارات المحيطة.", description: "تُستخدم في `port map (...)` و `generic map (...)` عند تنصيب المكوّنات.", example: "u_counter : counter port map (\n  clk => sys_clk,\n  q   => led\n);" },
            in:           { summary: "اتجاه منفذ — إدخال فقط.", description: "يقرأ التصميم هذا المنفذ ولا يستطيع قيادته." },
            out:          { summary: "اتجاه منفذ — إخراج فقط.", description: "يقود التصميم هذا المنفذ ولا يمكن قراءة قيمته داخل نفس entity (استخدم `buffer` أو إشارة داخلية إن لزم)." },
            inout:        { summary: "منفذ ثنائي الاتجاه.", description: "يقود ويقرأ من نفس الخط الفيزيائي. يمكن لمصدر قيادة واحد فقط أن يكون نشطًا في وقت ما — على البقية أن تكون `'Z'` (مقاومة عالية)." },
            buffer:       { summary: "خرج يمكن قراءته داخليًا أيضًا.", description: "مثل `out` لكنه قابل للقراءة داخل entity. يُعدّ قديمًا — يُفضّل `out` مع إشارة داخلية." },

            // --- إعلانات الإشارات ---
            signal:       { summary: "تعلن إشارة (سلكًا).", description: "إن **signal** تنقل القيم عبر الزمن. داخل process تجدول الإسنادات (`<=`) التحديث بدلاً من تطبيقه فورًا.", example: "signal counter : unsigned(7 downto 0) := (others => '0');" },
            variable:     { summary: "تعلن متغيرًا (محليًا داخل process).", description: "يعيش **variable** داخل process أو برنامج فرعي. على عكس signal فإن `:=` يُسند فورًا — مفيد للحسابات الوسيطة داخل process.", example: "process\n  variable tmp : integer := 0;\nbegin\n  tmp := tmp + 1;\nend process;" },
            constant:     { summary: "تعلن ثابتًا مُسمّى.", description: "تعطي اسمًا قابلًا للقراءة لقيمة ثابتة. يمكن أن يظهر في أي نطاق.", example: "constant CLOCK_HZ : integer := 50_000_000;" },
            shared:       { summary: "متغير مشترك يمكن لعدة عمليات الوصول إليه.", description: "له معنى فقط في VHDL-2008. استخدمه بحذر — عادةً مع الأنواع المحمية أو الأعداد الصحيحة." },

            // --- نظام الأنواع ---
            type:         { summary: "تُعرّف نوعًا جديدًا.", description: "تُستخدم للتعدادات والمدى والمصفوفات والسجلات.", example: "type state_t is (idle, run, done);" },
            subtype:      { summary: "تعلن نسخة مقيّدة من نوع موجود.", description: "تضيف قيود مدى أو دقّة دون إنشاء نوع جديد بالكامل.", example: "subtype byte is std_logic_vector(7 downto 0);" },
            array:        { summary: "تعلن نوعًا مصفوفيًا.", description: "بُعد واحد أو متعدد. يمكن أن يكون مقيّدًا (حجم ثابت) أو غير مقيّد (يحدَّد الحجم عند الاستخدام).", example: "type memory_t is array(0 to 255) of std_logic_vector(7 downto 0);" },
            record:       { summary: "تعلن نوعًا مركّبًا بحقول مُسمّاة.", description: "نظير `struct` في لغة C داخل VHDL.", example: "type axi_cmd_t is record\n  addr : std_logic_vector(31 downto 0);\n  len  : unsigned(7 downto 0);\nend record;" },
            range:        { summary: "قيد مدى للأنواع السلمية.", description: "عادةً `A to B` (تصاعدي) أو `A downto B` (تنازلي). تُستخدم أيضًا في حلقات for.", example: "for i in 0 to 7 loop ... end loop;" },
            downto:       { summary: "اتجاه مدى تنازلي.", description: "`MSB downto LSB` — العُرف القياسي لإعلان فهارس المتجهات بحيث يكون البِت 7 على اليسار.", example: "std_logic_vector(7 downto 0)" },
            to:           { summary: "اتجاه مدى تصاعدي.", description: "`A to B` — يُستخدم في الحلقات والمصفوفات المفهرسة تصاعديًا.", example: "for i in 0 to 15 loop ... end loop;" },
            others:       { summary: "محرف بدل لقيم افتراضية في تجميعة.", description: "يضبط كل المواقع غير المسمّاة في تجميعة.", example: "signal zeros : std_logic_vector(7 downto 0) := (others => '0');" },
            units:        { summary: "تعلن وحدات نوع فيزيائي.", description: "تُستخدم عند تعريف أنواع فيزيائية مخصّصة مثل `time`." },
            access:       { summary: "نوع وصول شبيه بالمؤشّر.", description: "نادرًا ما يكون قابلاً للتركيب — يُستخدم لهياكل البيانات المربوطة في المحاكاة." },
            file:         { summary: "نوع ملف للإدخال/الإخراج أثناء المحاكاة.", description: "غير قابل للتركيب. شائع في منصّات الاختبار لقراءة ملفات التحفيز." },

            // --- التعليمات التتابعية / تحكم التدفّق ---
            process:      { summary: "كتلة تعليمات تتابعية.", description: "إن **process** ينفّذ تعليماته بالتتابع ويعاد تشغيله عند تغيّر إشارة في قائمة حساسيته. اللبنة الأساسية للمنطق المتزامن مع الساعة.", example: "process(clk, reset)\nbegin\n  if reset = '1' then\n    q <= (others => '0');\n  elsif rising_edge(clk) then\n    q <= d;\n  end if;\nend process;" },
            if:           { summary: "تنفيذ شرطي.", description: "البنية القياسية `if … then … elsif … else … end if;` داخل process.", example: "if rising_edge(clk) then\n  q <= d;\nend if;" },
            then:         { summary: "تفصل الشرط عن الجسم في `if` / `elsif`.", description: "" },
            elsif:        { summary: "فرع شرطي إضافي ضمن `if`.", description: "" },
            else:         { summary: "فرع احتياطي في `if` / `when`.", description: "" },
            case:         { summary: "تفرّع متعدد بناءً على قيمة.", description: "كأمر `switch` في C. عادةً أوضح من سلسلة طويلة من `if`/`elsif`.", example: "case state is\n  when idle => …\n  when busy => …\n  when others => null;\nend case;" },
            when:         { summary: "محدِّد فرع في `case`، ويُستخدم أيضًا في الإسناد الشرطي.", description: "", example: "q <= '1' when enable = '1' else '0';" },
            select:       { summary: "تختار بين بدائل في إسناد إشارة منتقى.", description: "تعليمة متزامنة `with … select`، إحدى الصورتين الكلاسيكيتين للإسناد الشرطي المتزامن.", example: "with state select\n  q <= a when idle,\n       b when busy,\n       c when others;" },
            loop:         { summary: "بنية حلقة عامة.", description: "قد تكون غير شرطية (لانهائية) أو مفهرسة بـ `for` أو مشروطة بـ `while`.", example: "for i in 0 to 7 loop\n  result(i) <= data(7 - i);\nend loop;" },
            for:          { summary: "حلقة مفهرسة.", description: "تتكرّر على مدى. مفيدة في process وفي كتل `generate` على حد سواء.", example: "for i in 0 to 15 loop … end loop;" },
            while:        { summary: "حلقة مبنية على شرط.", description: "نادرًا ما تكون قابلة للتركيب عندما يعتمد الشرط على إشارات." },
            next:         { summary: "ينتقل إلى التكرار التالي في الحلقة.", description: "" },
            exit:         { summary: "يخرج من حلقة.", description: "" },
            return:       { summary: "يُعيد قيمة من function أو يخرج من procedure.", description: "" },
            wait:         { summary: "يعلّق process.", description: "`wait on <إشارات>` / `wait until <شرط>` / `wait for <زمن>`. قابل للتركيب فقط في صور بعينها (مثل `wait until rising_edge(clk)`).", example: "wait until rising_edge(clk);" },
            null:         { summary: "تعليمة لا تُنفذ شيئًا.", description: "محرف بديل حيث تتطلب القواعد وجود تعليمة." },

            // --- متزامن / generate ---
            begin:        { summary: "بداية كتلة تعليمات.", description: "يفتح جسم `architecture` و `process` و `function` و `procedure` و `generate`." },
            end:          { summary: "يُغلق كتلة.", description: "يقابل `entity` و `architecture` و `process` و `if` و `case` و `loop` ونحوها." },
            generate:     { summary: "ينشئ عتادًا بشكل شرطي أو في حلقات.", description: "اعتبره حلقة for أو if في زمن التصريف يُصدر بنية مكرّرة أو شرطية.", example: "gen_bits : for i in 0 to 7 generate\n  q(i) <= d(7 - i);\nend generate;" },
            block:        { summary: "منطقة تعليمات متزامنة بنطاق خاص.", description: "تجمع التعليمات تحت شرط حراسة. نادر في الكود الحديث." },
            assert:       { summary: "فحص في زمن المحاكاة.", description: "يطلق `report` ويتوقف (أو لا، حسب severity) إذا كان الشرط خاطئًا. غير قابل للتركيب.", example: "assert data'length = 8 report \"عرض غير صحيح\" severity error;" },
            report:       { summary: "يصدر رسالة محاكاة.", description: "غالبًا مع `assert`. مستويات: `note` و `warning` و `error` و `failure`." },
            severity:     { summary: "مستوى الشدّة لـ `assert`/`report`.", description: "" },

            // --- البرامج الفرعية ---
            function:     { summary: "برنامج فرعي خالص يُعيد قيمة.", description: "يحسب قيمة من معاملاته — دون أي آثار جانبية على الإشارات.", example: "function max(a, b : integer) return integer is\nbegin\n  if a > b then return a; else return b; end if;\nend function;" },
            procedure:    { summary: "برنامج فرعي قادر على قيادة الإشارات.", description: "بخلاف function يمكن لـ procedure امتلاك معاملات `out` / `inout`." },
            impure:       { summary: "تصف function بأنها قد تعتمد على حالة خارجية.", description: "تسمح بقراءة الإشارات المُعلَنة خارج نطاقها." },
            pure:         { summary: "تصف function بأنها دون تبعيات خارجية.", description: "القيمة الافتراضية لـ `function` — نادرًا ما تُكتب صراحة." },
            open:         { summary: "يترك منفذًا غير موصول، أو يفتح ملفًا.", description: "في port map: `some_port => open;`. لإدخال/إخراج الملفات: `file F : text open read_mode is ...;`" },

            // --- المعاملات (كلمات مفتاحية) ---
            and:          { summary: "AND منطقي.", description: "بِتّ ببِتّ على std_logic_vector / bit_vector." },
            or:           { summary: "OR منطقي.", description: "بِتّ ببِتّ على المتجهات." },
            not:          { summary: "NOT منطقي.", description: "نفي أُحادي." },
            nand:         { summary: "NAND منطقي.", description: "" },
            nor:          { summary: "NOR منطقي.", description: "" },
            xor:          { summary: "XOR منطقي.", description: "" },
            xnor:         { summary: "XNOR منطقي.", description: "" },
            mod:          { summary: "باقي القسمة (modulo).", description: "تأخذ النتيجة إشارة المقسوم عليه (بخلاف `rem`)." },
            rem:          { summary: "باقٍ (remainder).", description: "تأخذ النتيجة إشارة المقسوم." },
            abs:          { summary: "القيمة المطلقة.", description: "" },
            sla:          { summary: "إزاحة حسابية لليسار.", description: "" },
            sra:          { summary: "إزاحة حسابية لليمين.", description: "" },
            sll:          { summary: "إزاحة منطقية لليسار.", description: "" },
            srl:          { summary: "إزاحة منطقية لليمين.", description: "" },
            rol:          { summary: "تدوير لليسار.", description: "" },
            ror:          { summary: "تدوير لليمين.", description: "" },

            // --- متفرقات ---
            is:           { summary: "كلمة مفتاحية فاصلة.", description: "تظهر في `entity X is` و `architecture Y of X is` و `type T is` ونحوها." },
            of:           { summary: "كلمة مفتاحية فاصلة.", description: "تظهر في `architecture A of X` و `array(…) of …`." },
            all:          { summary: "محرف بدل.", description: "يُستخدم في `use x.y.all;` وفي سياقات السمات/قائمة الحساسية." },
            new:          { summary: "يخصّص كائنًا من نوع access (مؤشر).", description: "نادرًا ما يكون قابلاً للتركيب." },
            label:        { summary: "يُسمّي تعليمة باسم.", description: "مفيد لـ process و generate والحلقات للإشارة إليها لاحقًا." },
            alias:        { summary: "يعطي اسمًا بديلًا لكائن أو لجزء منه.", description: "", example: "alias sign : std_logic is data(7);" },
            attribute:    { summary: "وسم وصفي أو استعلام مُعرَّف مسبقًا.", description: "استعلامات مثل `x'length` و `x'range` و `signal'event`." },
            group:        { summary: "يجمّع عناصر مُسمّاة لتطبيق السمات.", description: "نادر." },
            bus:          { summary: "مُعدِّل إشارة قديم.", description: "تاريخي." },
            register:     { summary: "مُعدِّل إشارة قديم.", description: "تاريخي." },
            disconnect:   { summary: "يتحكم في زمن فصل الإشارات المحروسة.", description: "تاريخي / نادر." },
            after:        { summary: "محدِّد تأخير نقل.", description: "`a <= b after 5 ns;` — تأخير للمحاكاة فقط." },
            reject:       { summary: "نبضة رفض لتأخير القصور.", description: "" },
            inertial:     { summary: "تأخير القصور (الافتراضي).", description: "يرشّح النبضات القصيرة." },
            transport:    { summary: "تأخير نقل (دون ترشيح).", description: "" },
            guarded:      { summary: "يُقيّد كتلة أو إسناد إشارة.", description: "نادر." },
            linkage:      { summary: "اتجاه منفذ للربط بين اللغات.", description: "ذو صلة فقط في تدفقات VHDL/Verilog المختلطة." },
            postponed:    { summary: "يُؤجّل process أو تأكيدًا حتى نهاية دورة دلتا.", description: "للمحاكاة فقط." },
            literal:      { summary: "كلمة مفتاحية متعلقة بالسمات.", description: "نادر." },
            unaffected:   { summary: "محرف \"لا تفعل شيئًا\" في إسناد إشارة شرطي.", description: "" },
            until:        { summary: "تُستعمل مع `wait until <شرط>`.", description: "" },
            with:         { summary: "تبدأ إسناد إشارة منتقى.", description: "`with expr select …`." },
            on:           { summary: "تُستعمل في `wait on <قائمة إشارات>`.", description: "" }
        },

        types: {
            std_logic:        { summary: "منطق IEEE بتسع قيم: سلك واحد.", description: "القيم: `'0'` و `'1'` و `'Z'` (مقاومة عالية) و `'X'` (غير معروف) و `'U'` (غير مُهيّأ) و `'W'` و `'L'` و `'H'` و `'-'` (don't-care). الافتراضي للإشارات أحادية البِت في التركيب.", example: "signal clk : std_logic := '0';" },
            std_logic_vector: { summary: "متجه من std_logic — ناقل.", description: "يُعلَن بمدى: `(high downto low)` هو العُرف القياسي. مناسب للتوصيل؛ للحساب حوّل إلى `unsigned`/`signed`.", example: "signal data : std_logic_vector(7 downto 0);" },
            std_ulogic:       { summary: "نسخة غير محلولة من std_logic.", description: "تسمح بقائد واحد فقط. نادرًا ما تُستعمل مباشرة." },
            std_ulogic_vector:{ summary: "متجه غير محلول من std_ulogic.", description: "" },
            bit:              { summary: "منطق ثنائي بسيط: `'0'` أو `'1'`.", description: "أقدم من std_logic. استخدم std_logic في التركيب — لا يستطيع bit تمثيل Z أو X ونحوها." },
            bit_vector:       { summary: "متجه من `bit`.", description: "" },
            boolean:          { summary: "نوع منطقي: `true` / `false`.", description: "يُستعمل في الشروط؛ لا يُركَّب مباشرة على سلك (حوّله إلى std_logic)." },
            integer:          { summary: "عدد صحيح — 32-بِت بإشارة على الأقل.", description: "ممتاز للعدّادات والفهارس. قيّده بمدى جزئي (مثل `integer range 0 to 255`) ليعرف مركّب الأجهزة عدد البِتّات." },
            natural:          { summary: "`integer range 0 to integer'high`.", description: "عدد صحيح غير سالب." },
            positive:         { summary: "`integer range 1 to integer'high`.", description: "عدد صحيح موجب صرف." },
            real:             { summary: "نوع فاصلة عائمة.", description: "غير قابل للتركيب — للمحاكاة/الثوابت فقط." },
            character:        { summary: "محرف ASCII / ISO-8859-1.", description: "" },
            string:           { summary: "مصفوفة من character.", description: "تُستعمل عادةً لرسائل `report` ولإدخال/إخراج الملفات." },
            time:             { summary: "نوع فيزيائي يمثّل الزمن.", description: "الوحدات: `fs` و `ps` و `ns` و `us` و `ms` و `sec` و `min` و `hr`. للمحاكاة فقط." },
            signed:           { summary: "متجه عددي (بإشارة) من ieee.numeric_std.", description: "للحساب بإشارة. التحويل من std_logic_vector عبر صب `signed()`.", example: "signal x : signed(7 downto 0);" },
            unsigned:         { summary: "متجه عددي (بدون إشارة) من ieee.numeric_std.", description: "للحساب بدون إشارة. صب عبر `unsigned()` / `std_logic_vector()`.", example: "signal cnt : unsigned(7 downto 0) := (others => '0');" },
            line:             { summary: "وصول إلى سطر نصّي — لإدخال/إخراج الملفات.", description: "يُستعمل مع `readline` / `writeline` في منصّات الاختبار." },
            text:             { summary: "نوع الملف في حزمة textio.", description: "" },
            severity_level:   { summary: "مستوى الشدّة لـ `assert`/`report`.", description: "القيم: `note` و `warning` و `error` و `failure`." },
            side:             { summary: "نوع مساعد في textio.", description: "" },
            width:            { summary: "نوع مساعد في textio.", description: "" }
        },

        builtins: {
            rising_edge:    { summary: "صحيح عند انتقال 0→1 لساعة.", signature: "rising_edge(s : std_ulogic) return boolean", description: "مفضّل على `clk'event and clk = '1'`. يعمل على std_logic و std_ulogic.", example: "if rising_edge(clk) then q <= d; end if;" },
            falling_edge:   { summary: "صحيح عند انتقال 1→0.", signature: "falling_edge(s : std_ulogic) return boolean", description: "نظير `rising_edge`." },
            to_integer:     { summary: "يحوّل signed/unsigned إلى integer.", signature: "to_integer(arg : unsigned) return natural", description: "من `ieee.numeric_std`. صبّ معتاد بعد العمليات الحسابية.", example: "variable n : integer := to_integer(unsigned(data));" },
            to_unsigned:    { summary: "يحوّل integer إلى متجه unsigned.", signature: "to_unsigned(arg : natural; size : natural) return unsigned", description: "", example: "counter <= to_unsigned(42, 8);" },
            to_signed:      { summary: "يحوّل integer إلى متجه signed.", signature: "to_signed(arg : integer; size : natural) return signed", description: "" },
            resize:         { summary: "يعدّل حجم متجه signed/unsigned.", signature: "resize(arg : unsigned; new_size : natural) return unsigned", description: "تمديد بالأصفار في unsigned، وبامتداد الإشارة في signed." },
            shift_left:     { summary: "إزاحة منطقية لليسار.", signature: "shift_left(arg : unsigned; count : natural) return unsigned", description: "تملأ الجانب الأيمن بالأصفار." },
            shift_right:    { summary: "منطقية لليمين في unsigned / حسابية لليمين في signed.", signature: "shift_right(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_left:    { summary: "تدوير دائري لليسار.", signature: "rotate_left(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_right:   { summary: "تدوير دائري لليمين.", signature: "rotate_right(arg : unsigned; count : natural) return unsigned", description: "" },
            conv_integer:   { summary: "تحويل integer قديم.", signature: "conv_integer(arg : std_logic_vector) return integer", description: "من `ieee.std_logic_arith`. في الكود الجديد فضّل `to_integer` من numeric_std." },
            conv_std_logic_vector: { summary: "تحويل متجه قديم.", signature: "conv_std_logic_vector(arg : integer; size : integer) return std_logic_vector", description: "في الكود الجديد فضّل `std_logic_vector(to_unsigned(...))`." },
            now:            { summary: "زمن المحاكاة الحالي.", signature: "now : time", description: "للمحاكاة فقط." },
            readline:       { summary: "يقرأ سطرًا من ملف textio.", signature: "readline(file f : text; l : out line)", description: "إدخال/إخراج لمنصّة اختبار." },
            writeline:      { summary: "يكتب سطرًا إلى ملف textio.", signature: "writeline(file f : text; l : inout line)", description: "إدخال/إخراج لمنصّة اختبار." },
            endline:        { summary: "تحقق من نهاية السطر.", signature: "endline(l : inout line) return boolean", description: "مساعد textio." },
            endfile:        { summary: "تحقق من نهاية الملف.", signature: "endfile(file f : text) return boolean", description: "" },
            write:          { summary: "أوّلية كتابة في textio.", signature: "write(l : inout line; value : … )", description: "" },
            read:           { summary: "أوّلية قراءة في textio.", signature: "read(l : inout line; value : out … )", description: "" }
        },

        snippets: {
            entity: {
                label: "entity (هيكل)",
                summary: "إدراج إعلان entity أدنى",
                insertText: "entity ${1:name} is\n\tport (\n\t\t${2:clk}   : in  std_logic;\n\t\t${3:reset} : in  std_logic;\n\t\t${4:q}     : out std_logic_vector(${5:7} downto 0)\n\t);\nend entity ${1:name};\n\narchitecture behavioral of ${1:name} is\nbegin\n\t$0\nend architecture behavioral;"
            },
            process_clocked: {
                label: "process (متزامن، reset غير متزامن)",
                summary: "process متزامن قياسي مع reset غير متزامن",
                insertText: "process(${1:clk}, ${2:reset})\nbegin\n\tif ${2:reset} = '1' then\n\t\t$3 <= (others => '0');\n\telsif rising_edge(${1:clk}) then\n\t\t$0\n\tend if;\nend process;"
            },
            process_combinational: {
                label: "process (تجميعي)",
                summary: "process تجميعي بقائمة حساسية",
                insertText: "process(${1:all})\nbegin\n\t$0\nend process;"
            },
            if_else: {
                label: "if … elsif … else",
                summary: "شرط ثلاثي الفروع",
                insertText: "if ${1:condition} then\n\t$2\nelsif ${3:other} then\n\t$4\nelse\n\t$5\nend if;"
            },
            case_stmt: {
                label: "case … when … end case",
                summary: "هيكل لتعليمة case",
                insertText: "case ${1:expr} is\n\twhen ${2:value} => $3\n\twhen others    => $0\nend case;"
            },
            for_loop: {
                label: "for i in 0 to N-1 loop",
                summary: "حلقة مفهرسة",
                insertText: "for ${1:i} in 0 to ${2:N}-1 loop\n\t$0\nend loop;"
            },
            signal: {
                label: "إعلان signal",
                summary: "أعلِن إشارة",
                insertText: "signal ${1:name} : ${2:std_logic_vector}(${3:7} downto 0) := (others => '0');"
            },
            counter: {
                label: "قالب عدّاد",
                summary: "عدّاد بالتفاف",
                insertText: "signal cnt : unsigned(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk}, ${3:reset})\nbegin\n\tif ${3:reset} = '1' then\n\t\tcnt <= (others => '0');\n\telsif rising_edge(${2:clk}) then\n\t\tcnt <= cnt + 1;\n\tend if;\nend process;\n${4:q} <= std_logic_vector(cnt);"
            },
            shift_register: {
                label: "قالب مسجّل إزاحة",
                summary: "مسجّل إزاحة بـ N بِت",
                insertText: "signal sr : std_logic_vector(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk})\nbegin\n\tif rising_edge(${2:clk}) then\n\t\tsr <= sr(${1:7}-1 downto 0) & ${3:d};\n\tend if;\nend process;\n${4:q} <= sr;"
            },
            library_ieee: {
                label: "library ieee + use",
                summary: "استيرادات IEEE القياسية",
                insertText: "library ieee;\nuse ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;\n\n$0"
            }
        },

        ui: {
            declaredOnLine: "معرَّف في السطر",
            kindSignal:     "إشارة",
            kindVariable:   "متغير",
            kindConstant:   "ثابت",
            kindPort:       "منفذ",
            categoryKeyword:  "كلمة مفتاحية VHDL",
            categoryType:     "نوع",
            categoryBuiltin:  "دالة IEEE",
            categoryUser:     "إعلان مستخدم",
            categorySnippet:  "مقتطف",
            translationPending: "⚠ الترجمة معلقة — الوصف معروض بالإنجليزية."
        }
    };
})();
