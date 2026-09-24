## ALU-Linksshift — Lücke füllen

Ein Barrel-Shifter ist **fast** fertig. Die Entity deklariert
bereits den 8-Bit-Eingang `a`, den 3-Bit-Shift-Betrag `sh` und
den Ausgang `y`. Der umschließende `process(a, sh)` ist
ebenfalls vorhanden.

Deine Aufgabe: weise `y` den Wert von `a` zu, **links** um `sh`
Positionen verschoben, mit Null-Auffüllung rechts.

### Spezifikation

* `numeric_std` ist bereits eingebunden.
* Wandle `a` nach `unsigned`, den Betrag mit
  `to_integer(unsigned(sh))`, und nutze `shift_left(...)`.
* Wandle das Ergebnis mit `std_logic_vector(...)` zurück.
* Bits, die über das MSB hinausgeschoben werden, gehen verloren;
  die LSBs werden `0`.

### Hinweis

Eine Zeile reicht:

```vhdl
y <= std_logic_vector(shift_left(unsigned(a), to_integer(unsigned(sh))));
```

### Wo schreiben

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden
bewertet.
