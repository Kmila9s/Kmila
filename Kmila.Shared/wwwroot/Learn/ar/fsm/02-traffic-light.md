# مثال: FSM لإشارة المرور

إشارة المرور هي FSM-Moore الكلاسيكي: ثلاث حالات (`RED`، `GREEN`، `YELLOW`)، خرج لكل لون، ومؤقِّت يقول «أبقِ كل حالة لبضع دورات قبل الانتقال».

## البنية

يتَّبع المثال **نمط المعالجَين**:

- المعالج 1 هو **سجل الحالة** — قلَّاب D كلاسيكي يحفظ `state` ويعدُّ عدَّاد الانتظار.
- المعالج 2 هو **منطق الحالة التالية** — توافقي محض، يُقرِّر ما `next_state` بناءً على الحالة الحاليَّة.
- إسنادات الخرج (أسفل الهيكليَّة) خرجات Moore توافقيَّة — قيمها تعتمد على `state` فقط.

## استعراض الشفرة

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 تكَّات لكل لون
```

`state_t` **تعداد** — يختار المُركِّب الترميز الثنائي الفعلي، نحن نشير إلى حالات مُسمَّاة. `count` يتتبَّع مدَّة بقائنا في الحالة الحاليَّة. `HOLD` زمن البقاء.

سجل الحالة:

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    state <= S_RED;
    count <= (others => '0');
  elsif rising_edge(clk) then
    if count = HOLD then
      state <= next_state;
      count <= (others => '0');
    else
      count <= count + 1;
    end if;
  end if;
end process;
```

منطق الحالة التالية:

```vhdl
process(state)
begin
  case state is
    when S_RED    => next_state <= S_GREEN;
    when S_GREEN  => next_state <= S_YELLOW;
    when S_YELLOW => next_state <= S_RED;
  end case;
end process;
```

الخرجات (Moore محض):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## ماذا يفعل المثال

تشتغل الساعة نحو 400 µs. كل 8 دورات (7 انتظار + 1 انتقال) تتقدَّم الـ FSM. سترى ثلاث موجات مربَّعة تدور تتابعيًّا: `red` يصير مرتفعًا 8 تكَّات، ثم `green` 8 تكَّات، ثم `yellow`، ثم العودة إلى `red`.

## دورك

- **أضف دخلًا لطلب المشاة.** حين `ped = '1'` والحالة `S_GREEN`، أجبر `next_state <= S_YELLOW` (تقصر الدورة للسماح للمشاة بالعبور). هذا يحوِّل آلة Moore إلى شيء شبيه بـ Mealy — يعتمد خرج `yellow` على `state` و`ped` معًا.
- **أصدر إشارة `walk`** ترتفع فقط حين `state = S_RED`. هذا خرج Moore جديد.
- **اجعل `HOLD` بارامتريًّا** generic — 7 جيد للمحاكاة، لكن على FPGA بـ 50 ميجاهرتز ستحتاج بضع مئات الملايين من الدورات لتُحقِّق ثانية واحدة.
