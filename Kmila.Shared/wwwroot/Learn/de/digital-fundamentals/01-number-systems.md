# Zahlensysteme: Bits, Hex, Zweierkomplement

Digitale Schaltungen wissen nichts von "sieben" — sie kennen nur Reihen von Schaltern, die entweder an oder aus sind. Um mit ihnen zu entwerfen, musst du dich sicher zwischen der Darstellung, die Menschen mögen (Dezimal), und der, die Silizium versteht (Binär), bewegen können, mit Hexadezimal als nützlicher Kurzschreibweise dazwischen.

## Binär (Basis 2)

Ein **Bit** ist ein einzelner An/Aus-Wert, geschrieben als `0` oder `1`. Ein **Byte** sind acht Bits.

Stapel von Bits kodieren ganze Zahlen positional, genau wie im Dezimalsystem — jede Stelle ist eine Zweierpotenz:

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

32 Bits in Binärform zu schreiben ergibt eine 32-Zeichen-Suppe, die niemand Korrektur lesen kann:

```
00000000 00000000 11001010 11111110
```

Hex gruppiert die Bits zu viert. Jede Hex-Ziffer deckt genau ein Nibble (4 Bits) ab, sodass aus 32 Bits ein ordentlicher 8-Zeichen-String wird:

```
0x0000CAFE
```

Die Zuordnung ist fest:

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
signal addr : std_logic_vector(7 downto 0) := x"CA";  -- 8 Bits: 11001010
```

## Zweierkomplement (vorzeichenbehaftete Ganzzahlen)

Binär, wie oben gezeigt, kodiert nur **nicht-negative** Zahlen. Um negative Zahlen in dasselbe Bitmuster zu packen, verwenden Digitaldesigner das **Zweierkomplement**:

1. Schreibe die positive Zahl in Binärform.
2. Invertiere jedes Bit.
3. Addiere 1.

Um `-5` in 8 Bits darzustellen: `+5 = 00000101`, invertieren → `11111010`, plus 1 → `11111011`.

Warum dieses Schema? Weil die Addition unabhängig vom Vorzeichen identisch funktioniert — du brauchst nicht zwei Addierer. `-5 + 7` mit 8-Bit-Zweierkomplement:

```
  11111011   (-5)
+ 00000111   ( 7)
-----------
 100000010
```

Das neunte Bit läuft oben über, und die unteren 8 sind `00000010 = 2`. Was die richtige Antwort ist.

## Was zu merken ist

- Bits sind die Atome; Bytes sind Gruppen von 8.
- Binär und Hex sind **dieselbe Zahl** in unterschiedlicher Haut. Hex ist leichter zu lesen.
- Zweierkomplement kodiert Negative durch Invertieren-und-plus-eins — und es ist der Grund, warum `A + B` in deiner ALU unabhängig vom Vorzeichen funktioniert.
