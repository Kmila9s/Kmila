# Signal vs. Variable

Das ist die am meisten missverstandene Unterscheidung in VHDL. Studierende lesen "eine Variable ist wie eine lokale Variable in C" und laufen in die falsche Richtung davon. Lass uns das erden.

## Zwei Arten von Namen in VHDL

VHDL hat **Signale** (auf Architektur-Ebene mit `signal` deklariert) und **Variablen** (innerhalb eines Prozesses mit `variable` deklariert). Beide können Werte halten. Beide können zugewiesen werden. Sie sind **nicht** austauschbar.

## Die goldene Regel

- **Signalzuweisungen geschehen am Ende des aktuellen Simulations-Ticks.**
- **Variablenzuweisungen geschehen sofort, innerhalb des Prozesses.**

Das war's. Jede "Falle" mit Signalen vs. Variablen ist eine Neufassung dieser Regel.

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
      -- was ist s hier? Immer noch 0. Keine Zuweisung ist wirksam geworden.
    end if;
  end process;
end architecture demo;
```

Nachdem die Taktflanke abgeschlossen ist, wird `s` zu **2** (nur die letzte Zuweisung "gewinnt", wenn mehrere im selben Tick passieren). Innerhalb des Prozesses, nach beiden Zuweisungen, ist `s` immer noch `0` — denn der neue Wert propagiert erst, wenn der Prozess fertig ist.

Jetzt mit einer Variable:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- Variable lebt innerhalb des Prozesses
  begin
    if rising_edge(clk) then
      v := 1;           -- wird sofort wirksam
      v := 2;           -- wird sofort wirksam
      -- v ist hier 2, gerade jetzt, während dieses Ticks
    end if;
  end process;
end architecture demo;
```

Beachte zwei Dinge:

- Variablen verwenden `:=`; Signale verwenden `<=`.
- Variablen sind lokal zu einem einzelnen Prozess.

## Warum das für Hardware wichtig ist

Ein Signal **modelliert einen Draht oder ein Flipflop**. Du kannst den Wert eines Drahtes nicht zweimal im selben Augenblick ändern — was auch immer den Draht während eines Ticks treibt, legt sich am Ende dieses Ticks auf einen Wert fest.

Eine Variable **modelliert eine temporäre Berechnung** innerhalb eines Prozesses. Der Synthesizer darf sie als kombinatorische Logik implementieren oder ganz wegoptimieren.

## Wann welche verwenden

- **Signale** — der Standard. Jeder Port, jedes Register, jeder Bus, den du im Waveform-Viewer sehen wirst: Signal.
- **Variablen** — für lokale Schleifenzähler oder Zwischenergebnisse der Arithmetik innerhalb eines Prozesses, wo du C-artige Semantik in diesem Prozess möchtest.

## Eine häufige Falle

Manche Studierende schreiben:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- Register 1
    b <= a;    -- Register 2 — "tauscht" a und b
  end if;
end process;
```

Sie erwarten, dass das `a` und `b` vertauscht. Weil Signalzuweisungen am Ende des Ticks passieren, tut es das **tatsächlich**: An der Flanke bekommt `a` den **alten** Wert von `b`, und `b` bekommt den **alten** Wert von `a`. Das ist tatsächlich die idiomatische Art, zwei Register in einem Zyklus zu vertauschen.

Aber wenn du zu Variablen wechselst, ändert sich die Semantik:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a ist jetzt gleich b
    b := a;    -- b bekommt das NEUE a — also behält b seinen alten Wert, und a ändert sich
  end if;
end process;
```

Kein Tausch. Beide sind einfach gleich dem alten `b`.

## Was zu merken ist

- `signal`: aktualisiert am Ende des Ticks. `<=`-Zuweisung. Modelliert Drähte und Register.
- `variable`: aktualisiert sofort. `:=`-Zuweisung. Lokal zu einem Prozess. Modelliert Zwischenberechnung.
- Im Zweifelsfall — verwende ein Signal.
