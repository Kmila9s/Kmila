# ذاكرة ROM

**الذاكرة** تخزّن كلمات يمكن قراءتها بالـ**عنوان**. الـROM محتواها ثابت؛ كل عنوان يختار كلمة تظهر على الخرج.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/25-memory.html" title="شكل تفاعلي" loading="lazy"></iframe></div>

مكابس العنوان تختار خلية؛ كلمتها المخزّنة تُضيء مصابيح الخرج، ويعرض الجدول كل عنوان → بيانات.

في VHDL:

```vhdl
type rom_t is array(0 to 3) of unsigned(2 downto 0);
constant ROM : rom_t := ("101","011","110","001");
...
data <= ROM(to_integer(addr));  -- addressed read
```

القراءة توجيه عنوان محض — فالـROM لكل بت خرج هو حرفيًا مازج N-إلى-1. الـRAM تضيف مسار كتابة؛ وblock RAM (BRAM) ذاكرة مخصّصة داخل الـFPGA.
