# Beispiel: deine erste Entity (2-zu-1-MUX)

Ein **Multiplexer** ist das einfachste kombinatorische Gerät, das eine „Wahl" hat. Die 2-zu-1-Variante wählt zwischen zwei Eingängen basierend auf einem `sel`-Bit:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

Das VHDL dafür ist eine Zeile:

```vhdl
y <= a when sel = '0' else b;
```

## Was das Beispiel tut

Öffne das Beispiel und du bekommst ein Projekt namens **Your first entity** mit:

- `mux21.vhd` (das Design oben)
- Eine Begleit-Simulations-Config, die `sel`, `a` und `b` über 40 Mikrosekunden treibt

Wenn du Run drückst, zeigt der Wellenform-Tab vier Zeilen — die drei Eingänge und den Ausgang. Beobachte, wie `y` zwischen `a` und `b` flipt, wenn `sel` togglet.

## Dinge zu beachten

Die Zuweisung `y <= a when sel = '0' else b;` ist eine **nebenläufige Anweisung**: sie lebt außerhalb eines Prozesses und beschreibt eine permanente Verdrahtung. Es gibt kein „wann wird diese Zeile ausgeführt?" — sie existiert einfach.

Die Bedingung in einem `when/else` kann jeder boolesche Ausdruck sein:

```vhdl
y <= a when sel = '0' else
     b when enable = '1' else
     'Z';
```

Solche Ketten synthetisieren zu einem Prioritäts-Multiplexer (oberer Fall gewinnt Gleichstand).

## Du bist dran: erweitere es

Sobald das Beispiel läuft:

1. Ändere die Entity zu einem **4-zu-1-MUX** — `sel` wird zu 2 Bit, und du wählst zwischen `a`, `b`, `c`, `d`.
2. Füge einen fünften Port `output_enable` hinzu. Wenn er `'0'` ist, treibe `y <= 'Z'` (hochohmig), unabhängig von `sel`.
3. Vergleiche `when/else` mit der `with/select`-Variante, die du im nächsten Modul triffst.

Kmilas Editor hebt die VHDL-Schlüsselwörter hervor und kennt `std_logic` — hover über `when`, um die eingebaute Doku zu sehen.
