# Zahlensysteme: Bits, Hex, Zweierkomplement

Digitale Schaltungen wissen nichts von „sieben“ — sie kennen Reihen von Schaltern, die entweder an oder aus sind. Um mit ihnen zu entwerfen, musst du sicher zwischen der Darstellung wechseln können, die Menschen mögen (dezimal), und der, die das Silizium versteht (binär), mit Hexadezimal als nützlichem Kürzel dazwischen.

## Binär (Basis 2)

Ein **Bit** ist ein einzelner An/Aus-Wert, geschrieben `0` oder `1`. Ein **Byte** sind acht Bit.

Stapel von Bits codieren Ganzzahlen positionell, genau wie dezimal — jede Stelle ist eine Zweierpotenz:

```
  1 0 1 1 0
  │ │ │ │ └── 2⁰ = 1
  │ │ │ └──── 2¹ = 2
  │ │ └────── 2² = 0
  │ └──────── 2³ = 8
  └────────── 2⁴ = 16

  16 + 0 + 0 + 2 + 0 = 18
```

Also `10110₂ = 18₁₀`.

## Hexadezimal (Basis 16)

32 Bit binär hinzuschreiben ergibt einen 32-Zeichen-Brei, den niemand Korrektur lesen kann:

```
00000000 00000000 11001010 11111110
```

Hex gruppiert Bits zu je vieren. Jede Hex-Ziffer deckt genau ein Nibble (4 Bit) ab, also werden aus 32 Bit ein ordentlicher 8-Zeichen-String:

```
0x0000CAFE
```

Die Abbildung ist fest:

| bin  | hex | bin  | hex |
|------|-----|------|-----|
| 0000 | 0   | 1000 | 8   |
| 0001 | 1   | 1001 | 9   |
| 0010 | 2   | 1010 | A   |
| 0011 | 3   | 1011 | B   |
| 0100 | 4   | 1100 | C   |
| 0101 | 5   | 1101 | D   |
| 0110 | 6   | 1110 | E   |
| 0111 | 7   | 1111 | F   |

In VHDL schreibst du Hex-Literale für Vektoren, deren Länge ein Vielfaches von vier ist:

```vhdl
signal addr : std_logic_vector(7 downto 0) := x"CA";  -- 8 Bit: 11001010
```

## Zweierkomplement (vorzeichenbehaftete Ganzzahlen)

Binär wie oben codiert nur **nicht-negative** Zahlen. Um negative Zahlen ins selbe Bitmuster zu pressen, nutzen Digitalentwickler **Zweierkomplement**:

1. Schreibe die positive Zahl binär.
2. Flippe jedes Bit.
3. Addiere 1.

Um `-5` in 8 Bit darzustellen: `+5 = 00000101`, flippen → `11111010`, plus 1 → `11111011`.

Warum dieses Schema? Weil die Addition identisch funktioniert, ob vorzeichenbehaftet oder nicht — du brauchst keine zwei Addierer. `-5 + 7` in 8-Bit-Zweierkomplement:

```
  11111011   (-5)
+ 00000111   ( 7)
-----------
 100000010
```

Das neunte Bit läuft oben über, und die unteren 8 sind `00000010 = 2`. Das ist die richtige Antwort.

## Was du behalten solltest

- Bits sind die Atome; Bytes sind 8er-Gruppen.
- Binär und Hex sind **dieselbe Zahl** in verschiedener Hülle. Hex liest sich leichter.
- Zweierkomplement codiert negative Zahlen durch Flippen-und-eins-addieren — und ist der Grund, warum `A + B` in deiner ALU vorzeichenunabhängig funktioniert.
