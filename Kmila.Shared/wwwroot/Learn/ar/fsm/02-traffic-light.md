# عيّنة: FSM إشارة مرور

إشارة المرور هي آلة Moore النموذجية: ثلاث حالات (`RED`، `GREEN`، `YELLOW`)، خرج واحد لكل لون، ومؤقت يقول "ابقَ في كل حالة لبضع دورات قبل الانتقال".

## البنية

تتبع العيّنة **نمط العمليتين**:

- العملية 1 هي **سجل الحالة** — قلّاب D كلاسيكي يخزّن `state` ويُزيد عدّاد البقاء.
- العملية 2 هي **منطق الحالة التالية** — توافقي محض، يقرّر ما هي `next_state` بمعرفة الحالة الحالية.
- إسنادات الخرج (أسفل البنية المعمارية) هي خرجات Moore توافقية — قيمها تعتمد على `state` فقط.

## اقرأ الكود

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 نبضات لكل لون
```

`state_t` هو **ترقيم** — يختار المُجمِّع الترميز الثنائي الفعلي، ونحن نشير فقط إلى الحالات بأسمائها. يتتبّع `count` مدة بقائنا في الحالة الحالية. `HOLD` هو زمن المكوث.

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

## ما الذي تفعله العيّنة

تعمل الساعة لنحو 400 ميكروثانية. كل 8 ساعات (7 بقاء + 1 انتقال)، تتقدّم FSM. سترى ثلاث موجات مربّعة تدور بالتتابع: `red` يرتفع لمدة 8 نبضات، ثم `green` لمدة 8 نبضات، ثم `yellow`، ثم عودة إلى `red`.

## دورك

- **أضف مدخل طلب مشاة.** عندما `ped = '1'` والحالة `S_GREEN`، أجبر الحالة التالية أن تكون `S_YELLOW` (فتختصر الدورة للسماح للمشاة بالعبور). يحوّل هذا آلة Moore إلى شيء شبيه بـ Mealy — الخرج `yellow` يعتمد على كل من `state` **و** المدخل `ped`.
- **أطلق إشارة `walk`** تكون مرتفعة فقط عندما `state = S_RED`. هذا خرج Moore جديد.
- **اجعل `HOLD` بارامتريًا** كـ generic — 7 جيد للمحاكاة، لكن على FPGA بتردد 50 ميغاهرتز ستريد بضع مئات الملايين من الدورات للحصول على بقاء بـ 1 ثانية.
