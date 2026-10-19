# Beispiel: deine erste Entity (2-zu-1-MUX)

Ein **Multiplexer** ist das einfachste kombinatorische Gerät, das eine „Wahl“ in sich hat. Die 2-zu-1-Variante wählt zwischen zwei Eingängen basierend auf einem `sel`-Bit:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

Das VHDL dafür ist eine Zeile:

```vhdl
y <= a when sel = '0' else b;
```

## Die ganze Datei

Damit du alle vier Abschnitte aus den vorherigen Lektionen an einem Ort siehst:

```vhdl
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux21 is
  port (
    a   : in  std_logic;
    b   : in  std_logic;
    sel : in  std_logic;
    y   : out std_logic
  );
end entity mux21;

architecture rtl of mux21 is
begin
  y <= a when sel = '0' else b;
end architecture rtl;
```

## Was das Beispiel tut

Öffne das Beispiel und du bekommst ein Projekt namens **Deine erste Entity** mit:

- `mux21.vhd` (das Design oben)
- einer Begleit-Simulations-Config, die `sel`, `a` und `b` über 40 Mikrosekunden treibt

Wenn du Run drückst, zeigt der Wellenform-Tab vier Zeilen — die drei Eingänge und den Ausgang. Beobachte, wie `y` zwischen `a` und `b` flipt, wenn `sel` togglet.

## Dinge zu beachten

Die Zuweisung `y <= a when sel = '0' else b;` ist eine **nebenläufige Anweisung**: sie lebt außerhalb jedes Prozesses und beschreibt eine permanente Verdrahtung. Es gibt kein „wann wird diese Zeile ausgeführt?“ — sie *ist* einfach. Wenn sich `a`, `b` oder `sel` ändert, pegelt sich `y` nach einer Laufzeitverzögerung neu ein.

Die Bedingung in einem `when/else` kann jeder boolesche Ausdruck sein, und du kannst sie verketten:

```vhdl
y <= a   when sel = '0' else
     b   when enable = '1' else
     'Z';
```

Solche Ketten synthetisieren zu einem **Prioritäts**-Multiplexer (die oberste Bedingung gewinnt bei Gleichstand).

## Die Theorie darunter: Shannons Expansion

Diese eine Zeile ist nicht nur praktische Syntax — sie ist die Hardware-Form eines Theorems aus der booleschen Algebra, das der Kurs **teorema de expansión de Shannon** nennt. Jede Funktion einer Variable `s` kann aufgespalten werden als:

```
f(s, …) = (s' · f(0, …)) + (s · f(1, …))
```

Ein 2-zu-1-MUX berechnet genau `y = sel'·a + sel·b`. Mit anderen Worten: Ein Multiplexer ist die physische Verwirklichung der Shannon-Expansion um das Select-Bit — weshalb größere Funktionen so oft durch Stapeln von MUXes gebaut werden. Behalte das in der Hinterhand; es ist die Idee hinter der Multiplexer/Demultiplexer-Einheit später im Kurs.

## Du bist dran: erweitere es

Sobald du das Beispiel laufen siehst:

1. Ändere die Entity zu einem **4-zu-1-MUX** — `sel` wird zu einem 2-Bit-`std_logic_vector`, und du wählst zwischen `a`, `b`, `c`, `d`. (Eine `with sel select`-Anweisung liest sich hier sauber — du triffst sie im Kombinatorik-Modul.)
2. Füge einen fünften Port `output_enable` hinzu. Wenn er `'0'` ist, treibe `y <= 'Z'` (hochohmig), unabhängig von `sel`.
3. Bau einen **Demultiplexer** — das Spiegelbild. Ein Eingang, `sel`, und mehrere Ausgänge; der Eingang wird auf genau einen Ausgang geroutet, während die anderen auf `'0'` sitzen.

Kmilas Editor hebt die VHDL-Schlüsselwörter hervor und kennt `std_logic` — hover über `when`, um die eingebaute Doku zu sehen.

## Was du behalten solltest

- Eine **Entity** benennt die Schnittstelle; die **Architecture** liefert das Verhalten. Der MUX ist ein vollständiges, minimales Beispiel für beides.
- `y <= a when sel = '0' else b;` ist eine nebenläufige bedingte Zuweisung — permanente Verdrahtung, keine Zeile, die „abläuft“.
- Ein Multiplexer ist Shannons Expansion in Silizium: `y = sel'·a + sel·b`. Diese eine Idee skaliert hoch auf jeden breiteren MUX, den du bauen wirst.
