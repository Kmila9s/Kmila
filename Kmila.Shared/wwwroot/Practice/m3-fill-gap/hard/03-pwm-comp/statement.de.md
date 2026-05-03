## PWM-Komparator — Lücke füllen

Ein freilaufender 4-Bit-Zähler ist bereits verdrahtet. Er
zählt mit jedem Takt hoch und läuft von `1111` zurück zu
`0000` (PWM-Periode mit 16 Schritten).

Du fügst den **Komparator** hinzu, der aus dem Zähler ein
PWM-Signal macht: `pwm_out` ist **'1'**, solange
`count < duty`, sonst **'0'**.

### Spezifikation

* `count` ist `unsigned(3 downto 0)`.
* `duty` ist `STD_LOGIC_VECTOR(3 downto 0)` — wandle ihn mit
  `unsigned(duty)`.
* Nutze eine bedingte konkurrente Zuweisung:
  `pwm_out <= '1' when count < unsigned(duty) else '0';`
* Bei `duty = 0` bleibt der Ausgang `0`; bei `duty = 15` ist er
  in 15 von 16 Ticks `1`.

### Wo schreiben

Nur die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT` wird
bewertet. Den Zähler-Prozess nicht ändern.
