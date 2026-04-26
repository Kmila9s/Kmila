# Beispiel: deine erste Entity (2-zu-1-MUX)

Ein **Multiplexer** ist das einfachste kombinatorische Bauteil, das eine "Wahl" trifft. Die 2-zu-1-Version wählt zwischen zwei Eingängen basierend auf einem `sel`-Bit aus:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

Das VHDL dafür ist eine Zeile:

```vhdl
y <= a when sel = '0' else b;
```

## Was das Beispiel macht

Öffne das Beispiel, und du bekommst ein Projekt namens **Your first entity** mit:

- `mux21.vhd` (dem Design oben)
- Einer begleitenden Simulationskonfiguration, die `sel`, `a` und `b` über 40 Mikrosekunden antreibt

Wenn du Run klickst, zeigt der Waveforms-Tab vier Zeilen — die drei Eingänge und den Ausgang. Beobachte, wie `y` zwischen `a` und `b` wechselt, während `sel` kippt.

## Dinge, die auffallen

Die Zuweisung `y <= a when sel = '0' else b;` ist eine **nebenläufige Anweisung**: Sie lebt außerhalb eines jeden Prozesses und beschreibt eine permanente Verdrahtung. Es gibt kein "wann wird diese Zeile ausgeführt?" — sie ist einfach.

Die Bedingung innerhalb eines `when/else` kann jeder boolesche Ausdruck sein:

```vhdl
y <= a when sel = '0' else
     b when enable = '1' else
     'Z';
```

Ketten wie die werden zu einem Prioritäts-Multiplexer synthetisiert (der oberste Fall gewinnt bei Gleichstand).

## Du bist dran: erweitere es

Sobald du das Beispiel hast laufen sehen:

1. Ändere die Entity zu einem **4-zu-1-MUX** — `sel` wird zu 2 Bits, und du wählst zwischen `a`, `b`, `c`, `d`.
2. Füge einen fünften Port `output_enable` hinzu. Wenn er `'0'` ist, treibe `y <= 'Z'` (hochohmig) unabhängig von `sel`.
3. Vergleiche `when/else` mit der `with/select`-Version, die du im nächsten Modul kennenlernen wirst.

Kmilas Editor hebt die VHDL-Schlüsselwörter hervor und kennt `std_logic` — halte die Maus über `when`, um die eingebaute Dokumentation zu sehen.
