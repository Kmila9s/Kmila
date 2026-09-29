# Signal vs. Variable

Das ist die am meisten missverstandene Unterscheidung in VHDL. Studierende lesen „eine Variable ist wie eine lokale Variable in C“ und spurten in die falsche Richtung. Wir bringen es auf den Boden.

## Zwei Arten von Namen in VHDL

VHDL hat **Signale** (auf Architektur-Ebene mit `signal` deklariert) und **Variablen** (innerhalb eines Prozesses mit `variable` deklariert). Beide können Werte halten. Beide können zugewiesen werden. Sie sind **nicht** austauschbar.

## Die goldene Regel

- **Signal-Zuweisungen passieren am Ende des aktuellen Simulationsticks.**
- **Variablen-Zuweisungen passieren sofort, im Prozess.**

Das war's. Jedes „Gotcha“ zwischen Signalen und Variablen ist eine Neuformulierung dieser Regel.

## Die Demo

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- Anfrage: setze s auf 1 (noch nicht sichtbar)
      s <= 2;           -- Anfrage: setze s auf 2 (überschreibt die erste)
      -- was ist s hier? Immer noch 0. Keine Zuweisung ist wirksam.
    end if;
  end process;
end architecture demo;
```

Nach Abschluss der Taktflanke wird `s` zu **2** (nur die letzte Zuweisung „gewinnt“, wenn mehrere im selben Tick passieren). Innerhalb des Prozesses, nach beiden Zuweisungen, ist `s` immer noch `0` — weil der neue Wert sich nicht ausbreitet, bis der Prozess fertig ist.

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
      -- v ist hier 2, genau jetzt, während dieses Ticks
    end if;
  end process;
end architecture demo;
```

Beachte zwei Dinge:

- Variablen nutzen `:=`; Signale nutzen `<=`.
- Variablen sind lokal zu einem einzelnen Prozess.

## Warum das für Hardware wichtig ist

Ein Signal **modelliert einen Draht oder ein Flip-Flop**. Du kannst den Wert eines Drahts nicht zweimal im selben Augenblick ändern — was auch immer den Draht während eines Ticks treibt, pegelt sich am Ende dieses Ticks auf einen Wert ein.

Eine Variable **modelliert eine temporäre Berechnung** in einem Prozess. Der Synthesizer kann sie als kombinatorische Logik implementieren oder ganz wegoptimieren.

## Wann was nehmen

- **Signale** — der Default. Jeder Port, jedes Register, jeder Bus, den du auf dem Wellenform-Viewer siehst: Signal.
- **Variablen** — für lokale Schleifenzähler oder Zwischenergebnisse in einem Prozess, wo du C-artige Semantik im Prozess willst.

## Eine häufige Falle

Manche Studierende schreiben:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- Register 1
    b <= a;    -- Register 2 — „tauscht“ a und b
  end if;
end process;
```

Sie erwarten, dass dies `a` und `b` tauscht. Weil Signal-Zuweisungen am Ende des Ticks passieren, **tut** es das: an der Flanke bekommt `a` den **alten** Wert von `b`, und `b` bekommt den **alten** Wert von `a`. Das ist tatsächlich der idiomatische Weg, zwei Register in einem Zyklus zu tauschen.

Aber wenn du zu Variablen wechselst, ändert sich die Semantik:

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

Kein Tausch. Nur beide gleich dem alten `b`.

## Was du behalten solltest

- `signal`: aktualisiert am Ende des Ticks. `<=`-Zuweisung. Modelliert Drähte und Register.
- `variable`: aktualisiert sofort. `:=`-Zuweisung. Lokal zu einem Prozess. Modelliert Zwischenberechnung.
- Im Zweifel — nimm ein Signal.
