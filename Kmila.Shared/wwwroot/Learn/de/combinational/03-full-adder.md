# Beispiel: 4-Bit-Ripple-Carry-Addierer

Zwei Binärzahlen zu addieren ist das „hello world“ der digitalen Arithmetik. Dieses Beispiel baut einen 4-Bit-Addierer mit Carry-in und Carry-out — genau die Zelle, die du in einer größeren ALU oder in der Adress-Inkrement-Logik einer CPU kacheln würdest. Es ist aus gutem Grund die erste Schaltung in der Kombinatorik-Einheit („circuito sumador/restador“): Sobald du addieren kannst, folgen Subtraktion, Vergleich und Zählung von selbst.

## Starte bei einem Bit: der Volladdierer

Vor der 4-Bit-Version solltest du den einzelnen **Volladdierer** (full adder) kennen, aus dem er besteht. Für Bit `i`, mit Carry-in `cin`:

```
sum  = a xor b xor cin
cout = (a and b) or (cin and (a xor b))
```

Die Summe ist das XOR aller drei Eingänge (ungerade Parität); der Carry ist „zwei oder mehr der drei Eingänge sind 1“. Kette vier davon — jeweils den `cout` des einen in den `cin` des nächsten — und du hast einen **Ripple-Carry**-Addierer. Der Name ist wörtlich: Der Carry muss vom LSB ganz bis zum MSB durchrippeln, bevor das oberste Summenbit endgültig ist, weshalb diese Struktur einfach, aber nicht die schnellste ist.

## Schnittstelle

```vhdl
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## Der Body

Statt vier Volladdierer von Hand zu verdrahten, lass `numeric_std` die Arithmetik machen und fang nur den Carry ab:

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

Was passiert:

- `unsigned(a)` und `unsigned(b)` interpretieren die Vektoren als numerische Typen für den `+`-Operator neu.
- `resize(…, 5)` erweitert auf 5 Bit mit Nullen, damit die Summe Platz hat, in das oberste Bit überzulaufen.
- `("0000" & cin)` baut einen 5-Bit-Wert mit nur `cin` im LSB, sodass er sich sauber mitaddiert.
- Nach der Addition ist `t(3 downto 0)` die Summe und `t(4)` der Carry-out.

> Die beiden Stile — vier explizite Volladdierer vs. ein `+` auf resized `unsigned` — synthetisieren zur *gleichen* Ripple-Carry-Hardware (ein modernes Tool kann sie sogar zu einer schnelleren Carry-Struktur hochstufen). Die `+`-Version ist das, was du tatsächlich schreiben wirst; die Gatter-Version ist das, was du *erklären* können solltest.

## Warum 5 Bit?

Zwei 4-Bit-Zahlen mit einem Carry-in addiert können ein Ergebnis so groß wie `1111 + 1111 + 1 = 11111` erzeugen — fünf Bit. Würdest du die Rechnung in einem 4-Bit-Container machen, würdest du das oberste Bit lautlos verlieren. Auf ein zusätzliches Bit zu resizen ist das sichere Idiom, und Bit 4 *ist* dein Carry-out.

## Einen Subtrahierer bauen

Der Kurs paart den Addierer mit einem Subtrahierer, und im Zweierkomplement sind sie fast dieselbe Schaltung. Um `a − b` zu berechnen, addiere das bitweise Komplement von `b` und zwinge den Carry-in auf 1:

```vhdl
t <= resize(unsigned(a), 5) + resize(unsigned(not b), 5) + "00001";
```

Das ist `a + (¬b) + 1 = a − b`. Ein echter Addierer/Subtrahierer fügt ein XOR pro Bit hinzu (gesteuert von einer `add_sub`-Leitung) und führt dieselbe Leitung in `cin` — ein Steuerbit kippt die ganze Einheit zwischen den beiden Operationen.

## Was das Beispiel tut

Die Sim-Config treibt `a` und `b` durch mehrere Muster (`3+5 = 8`, dann einen carry-erzeugenden `10 + 6 = 16`, dann `10 + 15 = 25` …). Die Wellenform zeigt `sum` und `cout` **kombinatorisch** wechselnd — kein Takt nötig, weil dieses Design keine Register hat.

## Du bist dran

- **Mach es parametrisch.** Füge der Entity ein `generic (N : integer := 4)` hinzu und nutze `N`, wo immer die `4` vorkommt. Jetzt hast du einen `adder_N`, den du auf jeder Breite instanziieren kannst.
- **Bau den Addierer/Subtrahierer** von oben mit einem `add_sub`-Steuereingang.
- **Verdrahte zwei davon.** Einer für das untere Nibble, einer für das obere, mit dem ersten `cout`, der den zweiten `cin` speist — und du hast einen 8-Bit-Ripple-Carry-Addierer und ein Gefühl dafür, warum die Carry-Verzögerung mit der Breite wächst.

## Was du behalten solltest

- Ein Volladdierer ist `sum = a xor b xor cin`, `cout = majority(a, b, cin)`; kette sie, um breite Zahlen zu addieren.
- Gib der Arithmetik immer ein zusätzliches Bit (`resize`), damit der Carry nicht verloren geht.
- Subtraktion ist Addition des Zweierkomplements: `a − b = a + not b + 1`.
- Ripple-Carry ist einfach, aber seine Verzögerung wächst mit der Anzahl der Bits.
