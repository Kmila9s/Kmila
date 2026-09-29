# Beispiel: 4-Bit-Ripple-Carry-Addierer

Zwei Binärzahlen zu addieren ist das „hello world“ der digitalen Arithmetik. Dieses Beispiel baut einen 4-Bit-Addierer mit Carry-in und Carry-out — genau die Zelle, die du in einer größeren ALU oder in der Adress-Inkrement-Logik einer CPU kachelst.

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

## Der Body

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

Was passiert:

- `unsigned(a)` und `unsigned(b)` machen aus den Vektoren numerische Typen für `+`.
- `resize(…, 5)` polstert auf 5 Bit, damit die Summe in das oberste Bit überlaufen kann.
- `("0000" & cin)` baut eine 5-Bit-Maske mit nur `cin` im LSB.
- Nach der Addition ist `t(3 downto 0)` die Summe und `t(4)` der Carry-out.

## Warum 5 Bit?

Zwei 4-Bit-Zahlen plus Carry-in addiert ergibt höchstens `1111 + 1111 + 1 = 11111` — fünf Bit. Würdest du die Rechnung in einem 4-Bit-Container machen, würdest du das oberste Bit lautlos verlieren. Resize ist das sichere Idiom.

## Was das Beispiel tut

Die Sim-Config treibt `a` und `b` durch vier Muster (`3+5 = 8`, dann einen carry-erzeugenden `10 + 6 = 16`, dann `10 + 15 = 25` …). Die Wellenform zeigt `sum` und `cout` kombinatorisch wechselnd — kein Takt nötig, weil das Design keine Register hat.

## Du bist dran

- **Mach es parametrisch.** Füge der Entity ein `generic (N : integer := 4)` hinzu und nutze `N` wo immer die 4 vorkommt. Jetzt hast du einen `adder_N`, den du auf jeder Breite instanziieren kannst.
- **Bau einen Subtrahierer.** Flippe `b` und addiere noch eine `+1` — das ist Zweier-Komplement-Subtraktion. Keine Extra-Hardware außer einem NOT pro Bit.
- **Verdrahte zwei davon.** Einer für das untere Nibble, einer für das obere — und du hast einen 8-Bit-Addierer mit Ripple-Carry zwischen ihnen.
