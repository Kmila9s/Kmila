# Signal vs. Variable

Das ist die am meisten missverstandene Unterscheidung in VHDL. Studierende lesen „eine Variable ist wie eine lokale Variable in C“ und spurten in die falsche Richtung. Wir bringen es auf den Boden, denn sie richtig zu verstehen ist das, was Code, der nur *simuliert*, von Code trennt, der zu *der Hardware wird, die du gemeint hast*.

## Zwei Arten von Namen in VHDL

VHDL hat **Signale** (auf Architektur-Ebene mit `signal` deklariert) und **Variablen** (innerhalb eines Prozesses mit `variable` deklariert). Beide können Werte halten. Beide können zugewiesen werden. Sie sind **nicht** austauschbar.

| | Signal | Variable |
|---|---|---|
| Deklariert | Architecture (oder Package) | innerhalb eines Prozesses/Unterprogramms |
| Zuweisungsoperator | `<=` | `:=` |
| Wird wirksam | am Ende des Simulationsschritts | sofort |
| Scope | geteilt über die ganze Architecture | lokal zu seinem Prozess |
| Modelliert | einen Draht oder ein Register | eine temporäre Berechnung |

## Die goldene Regel

- **Signal-Zuweisungen werden *eingeplant*; der Wert erscheint am Ende des aktuellen Simulationsschritts.**
- **Variablen-Zuweisungen passieren sofort, genau dort, wo die Zeile steht.**

Das war's. Jedes „Gotcha“ zwischen Signalen und Variablen ist eine Neuformulierung dieser Regel.

## Die Demo

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- einplanen: setze s auf 1 (noch nicht sichtbar)
      s <= 2;           -- einplanen: setze s auf 2 (überschreibt die erste)
      -- was ist s hier? Immer noch 0. Keine Zuweisung ist wirksam.
    end if;
  end process;
end architecture demo;
```

Nachdem die Taktflanke abgeschlossen ist, wird `s` zu **2** — wenn ein Signal in einem Prozessdurchlauf mehrfach eingeplant wird, gewinnt nur die *letzte* Zuweisung. Innerhalb des Prozesses, nach beiden Zuweisungen, ist `s` immer noch `0`, weil der neue Wert sich nicht ausbreitet, bis der Prozess fertig ist.

Jetzt mit einer Variable:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- Variable lebt im Prozess
  begin
    if rising_edge(clk) then
      v := 1;           -- wirkt sofort
      v := 2;           -- wirkt sofort
      -- v ist hier 2, genau jetzt, während dieses Durchlaufs
    end if;
  end process;
end architecture demo;
```

Beachte zwei Dinge:

- Variablen nutzen `:=`; Signale nutzen `<=`.
- Variablen sind lokal zu einem einzelnen Prozess — du kannst `v` nicht aus einem anderen Prozess oder einer nebenläufigen Zuweisung lesen.

> Eine Feinheit zum Anfangswert: `variable v : integer := 0;` führt sein `:= 0` nur **einmal** aus, beim Start der Simulation — nicht bei jedem Prozessdurchlauf. Eine Variable behält ihren Wert zwischen den Durchläufen, solange du sie nicht neu zuweist. Das ist es, was sie *merkfähig* macht, und warum eine Variable, die in einem getakteten Prozess gelesen wird, bevor sie geschrieben wurde, versehentlich ein Register ableiten kann.

## Warum das für Hardware wichtig ist

Ein Signal **modelliert einen Draht oder ein Flip-Flop**. Du kannst den Wert eines Drahts nicht zweimal im selben Augenblick ändern — was auch immer den Draht während eines Schritts treibt, pegelt sich am Ende dieses Schritts auf einen Wert ein.

Eine Variable **modelliert eine temporäre Berechnung** in einem Prozess. Der Synthesizer implementiert sie typischerweise als kombinatorische Logik, die in die Register hinein- (oder aus ihnen heraus-)führt — oder optimiert sie ganz weg.

## Ein durchgerechnetes Beispiel: gesetzte Bits zählen

Variablen glänzen, wenn du ein Zwischenergebnis brauchst, das du in einer Schleife aktualisierst:

```vhdl
process(clk)
  variable ones : integer range 0 to 8;
begin
  if rising_edge(clk) then
    ones := 0;                      -- Akkumulator bei jedem Durchlauf zurücksetzen
    for i in data'range loop
      if data(i) = '1' then
        ones := ones + 1;           -- sofortige Aktualisierung, nächste Iteration liest sie
      end if;
    end loop;
    popcount <= ones;               -- den finalen Zählwert auf einem Signal veröffentlichen
  end if;
end process;
```

Versuch das mit einem Signal statt einer Variable und es bricht: `ones <= ones + 1` in der Schleife würde acht konkurrierende Zuweisungen einplanen, nur die letzte überlebt, und du bekämst immer `0` oder `1`. Die Schleife braucht die *sofortige* Semantik einer Variable.

## Wann was nehmen

- **Signale** — der Default. Jeder Port, jedes Register, jeder Bus, den du im Wellenform-Viewer sehen wirst: Signal.
- **Variablen** — für lokale Schleifenzähler oder arithmetische Zwischenergebnisse in einem Prozess, wo du C-artige „lies, was du gerade geschrieben hast“-Semantik willst.

## Eine häufige Falle

Manche Studierende schreiben:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- Register 1
    b <= a;    -- Register 2
  end if;
end process;
```

Sie sorgen sich, dass dies `a` und `b` nicht tauscht. Weil Signal-Zuweisungen eingeplant werden, **tut** es das: an der Flanke bekommt `a` den **alten** Wert von `b`, und `b` bekommt den **alten** Wert von `a`, gleichzeitig. Das ist tatsächlich der idiomatische Weg, zwei Register in einem Zyklus zu tauschen.

Aber wechsle zu Variablen und die Semantik ändert sich:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a ist jetzt gleich b
    b := a;    -- b bekommt das NEUE a — also behält b seinen alten Wert und a ändert sich
  end if;
end process;
```

Kein Tausch. Beide landen gleich dem alten `b`. Gleich aussehender Code, entgegengesetztes Ergebnis — alles wegen der goldenen Regel.

## Was du behalten solltest

- `signal`: Wert wird eingeplant, erscheint am Ende des Schritts; `<=`-Zuweisung; modelliert Drähte und Register; geteilt über die Architecture.
- `variable`: aktualisiert sofort; `:=`-Zuweisung; lokal zu einem Prozess; modelliert Zwischenberechnung.
- Mehrere Signal-Zuweisungen in einem Durchlauf → die letzte gewinnt. Mehrere Variablen-Zuweisungen → sie laufen alle, der Reihe nach.
- Im Zweifel — nimm ein Signal.
