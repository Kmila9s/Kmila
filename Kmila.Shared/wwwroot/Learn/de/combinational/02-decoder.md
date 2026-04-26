# Beispiel: 3-zu-8-Decoder

Ein Decoder nimmt einen kleinen Binärcode und verwandelt ihn in einen **One-Hot**-Ausgang: genau ein Ausgang ist hoch, und seine Position entspricht dem Code.

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

Decoder stecken in jedem Speichersystem — die Adressleitungen treiben einen Decoder, und der "Row-Match"-Ausgang aktiviert genau eine Zeile. Wann immer du "Adress-Decoder" hörst, ist das, was darunter steckt.

## Der VHDL-Code

Ein Decoder ist ein Lehrbuchfall für `with/select`:

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

Beachte `when others` am Ende. `sel` ist ein `std_logic_vector(2 downto 0)` — 3 Bits — es gibt also 2³ = 8 Binärwerte; aber weil `std_logic` neun Zustände hat, braucht der Compiler das `others`-Glied, um jede Kombination abzudecken (es wird zum selben wie `"111"` synthetisiert).

## Was das Beispiel macht

Das Beispiel durchläuft `sel` durch alle acht 3-Bit-Werte, jeweils 10 µs pro Wert. Auf dem Waveforms-Tab siehst du, wie die 8 Bits von `y` zu einer wandernden `'1'` werden — ein digitales "Lauflicht".

## Du bist dran

Sobald es läuft:

- Verbreitere den Decoder zu einer 4-zu-16-Version. Du kannst es von Hand machen, oder du schreibst den Rumpf um als `y <= std_logic_vector(shift_left(to_unsigned(1, 16), to_integer(unsigned(sel))));`. Beide synthetisieren identisch — wähle, was dir lesbarer erscheint.
- Füge einen `enable`-Eingang hinzu. Wenn `enable = '0'`, sollen alle Ausgänge unabhängig von `sel` niedrig sein.
