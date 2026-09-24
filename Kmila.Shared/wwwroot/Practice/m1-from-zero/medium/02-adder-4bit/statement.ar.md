## جامع 4 بتات

نفّذ جامعًا بعرض 4 بتات مع حامل إدخال `cin`. احسب `sum = a + b + cin` وحامل الإخراج `cout`.

### المواصفات

- المداخل: `a` و`b` متجهان `STD_LOGIC_VECTOR` بعرض 4 بتات؛ `cin` بت واحد.
- المخارج: `sum` بعرض 4 بتات؛ `cout` بت واحد.

### تلميح

استخدم `IEEE.NUMERIC_STD.ALL` ومتغيرًا مساعدًا `unsigned` بعرض 5 بتات:

```vhdl
signal s : unsigned(4 downto 0);
s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
sum  <= std_logic_vector(s(3 downto 0));
cout <= s(4);
```
