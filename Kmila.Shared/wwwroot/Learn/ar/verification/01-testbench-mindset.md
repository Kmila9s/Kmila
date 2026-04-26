# عقلية بنك الاختبار

في كل درس حتى هنا، كان Kmila بنك الاختبار لديك — تسحب الإثارة إلى تبويب Ports، وزر Run يُشغّل التصميم، وأنت تنظر إلى الموجة. هذا مقبول للتعلّم. لكنه ليس كيف يعمل التصميم الرقمي المهني.

## ما هو بنك الاختبار الحقيقي

**بنك الاختبار** (testbench) هو كيان VHDL آخر مهمته الوحيدة هي استجواب *التصميم قيد الاختبار* (DUT). لا منافذ له، لأن لا شيء يقوده؛ يُصرِّح بـ DUT كمكوّن، ويوصِّل إشاراته الداخلية بمنافذ DUT، ثم يستخدم **عملية** ليخطو عبر تسلسلات إثارة في الزمن.

هيكل:

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

ثلاث أفكار جديدة:

1. **`wait`** — لا يمرّ الزمن إلا حين تقول عملية إنه يمر.
2. **`assert … report … severity`** — الفحص الذاتي. يحوّل موجة من 1000 سطر إلى "نجاح" أو "فشل".
3. **كيان بلا منافذ** — بنوك الاختبار مكتفية ذاتيًا؛ يُنشئها المحاكي مباشرة.

## لماذا يهم هذا

- **الانحدار**: بنك اختبار كتبته أمس لا يزال يعمل اليوم. موجة حدّقت بها أمس ذهبت.
- **التغطية**: يمكنك قيادة أنماط إثارة لا تصل إليها جلسة تفاعلية أبدًا (مدخلات عشوائية، حالات حرجة، ترددات زاوية).
- **الأتمتة**: نظام CI يُشغّل بنك اختبارك على كل commit. الموجات لا تتسع في CI.

## الحدود الحالية لـ Kmila

محاكي Kmila يُشغّل بنوك الاختبار بسلاسة إن كتبتها ككيانات عادية — الشيء الوحيد المفقود اليوم هو استخلاص نجاح/فشل مؤتمت من عبارات `assert` إلى سجل Output. هذا ضمن خارطة الطريق.

*مزيد من الدروس قادمة — هذه الوحدة قيد التأليف النشط.*
