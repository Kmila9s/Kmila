# Beispiel: 4-Bit-Ripple-Carry-Addierer

Zwei Binärzahlen zu addieren ist das "Hello World" der digitalen Arithmetik. Dieses Beispiel baut einen 4-Bit-Addierer mit Carry-in und Carry-out — genau die Zelle, die du in einer größeren ALU oder in der Adress-Inkrement-Logik einer CPU kachelst.

## Schnittstelle

```
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## Der Rumpf

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

Was passiert hier:

- `unsigned(a)` und `unsigned(b)` verwandeln die Vektoren in numerische Typen für den `+`-Operator.
- `resize(…, 5)` füllt auf 5 Bits auf, damit die Summe ins oberste Bit überlaufen kann.
- `("0000" & cin)` baut eine 5-Bit-Maske mit nur `cin` im LSB.
- Nach der Addition ist `t(3 downto 0)` die Summe und `t(4)` der Carry-out.

## Warum 5 Bits?

Die Addition zweier 4-Bit-Zahlen mit einem Carry-in kann ein Ergebnis von bis zu `1111 + 1111 + 1 = 11111` erzeugen — fünf Bits. Rechne die Addition in einem 4-Bit-Container und du verlierst das oberste Bit stillschweigend. `resize` ist die sichere Redewendung.

## Was das Beispiel macht

Die Sim-Konfiguration treibt `a` und `b` durch vier Muster (`3+5 = 8`, dann ein Carry-erzeugendes `10 + 6 = 16`, dann `10 + 15 = 25`…). Die Waveform zeigt `sum` und `cout`, die sich kombinatorisch ändern — es wird kein Takt benötigt, weil dieses Design keine Register hat.

## Du bist dran

- **Mach es parametrisch.** Füge ein `generic (N : integer := 4)` zur Entity hinzu und verwende `N` überall dort, wo die 4 vorkommt. Jetzt hast du einen `adder_N`, den du in beliebiger Breite instanziieren kannst.
- **Baue einen Subtrahierer.** Invertiere `b` und addiere ein weiteres `+1` — das ist Zweierkomplement-Subtraktion. Keine zusätzliche Hardware außer einem NOT pro Bit.
- **Verschalte zwei davon.** Platziere einen für das untere Nibble und einen für das obere, und du hast einen 8-Bit-Addierer mit Ripple-Carry zwischen ihnen.
