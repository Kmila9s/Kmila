# Beispiel: 3-zu-8-Decoder

Ein Decoder nimmt einen kleinen Binärcode und verwandelt ihn in einen **One-Hot**-Ausgang: genau ein Ausgang ist high, und seine Position entspricht dem Code.

```
  sel  │  y
  000  │  00000001
  001  │  00000010
  010  │  00000100
  011  │  00001000
  100  │  00010000
  101  │  00100000
  110  │  01000000
  111  │  10000000
```

Decoder leben in jedem Speichersystem — die Adressleitungen treiben einen Decoder, und der „Row-Match“-Ausgang aktiviert genau eine Zeile. Wann immer du „Adressdecoder“ hörst, das ist es darunter.

## Das VHDL

Ein Decoder ist der Lehrbuchfall für `with/select`:

```vhdl
with sel select
  y <=
    "00000001" when "000",
    "00000010" when "001",
    "00000100" when "010",
    "00001000" when "011",
    "00010000" when "100",
    "00100000" when "101",
    "01000000" when "110",
    "10000000" when others;
```

Beachte `when others` am Ende. `sel` ist ein `std_logic_vector(2 downto 0)` — 3 Bit — also gibt es 2³ = 8 Binärwerte, aber weil `std_logic` neun Zustände hat, braucht der Compiler die `others`-Klausel, um jede Kombination abzudecken (sie wird zum gleichen wie `"111"` synthetisiert).

## Was das Beispiel tut

Das Beispiel zyklt `sel` durch alle acht 3-Bit-Werte, 10 µs pro Wert. Auf dem Wellenform-Tab siehst du die 8 Bits von `y` zu einer wandernden `'1'` werden — ein digitaler „Licht-Chaser“.

## Du bist dran

Sobald es läuft:

- Erweitere den Decoder zu 4-zu-16. Per Hand oder schreib den Body um als `y <= std_logic_vector(shift_left(to_unsigned(1, 16), to_integer(unsigned(sel))));`. Beide synthetisieren identisch — nimm das, was du lesbarer findest.
- Füge einen `enable`-Eingang hinzu. Wenn `enable = '0'`, sind alle Ausgänge low, unabhängig von `sel`.
