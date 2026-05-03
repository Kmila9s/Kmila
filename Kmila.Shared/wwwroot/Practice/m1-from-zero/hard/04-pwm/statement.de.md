## PWM-Generator (4 Bit Tastgrad)

Implementiere einen Pulsweitenmodulator. Ein freilaufender 4-Bit-Zähler `cnt` zählt mit jedem Takt hoch und springt nach 15 zurück auf 0. `pwm_out` ist hoch, solange `cnt < duty`.

### Spezifikation

- 4-Bit-`unsigned`-Zähler, inkrementiert bei jeder steigenden Flanke.
- Reset setzt den Zähler auf 0.
- Kombinatorischer Vergleich: `pwm_out = '1' when cnt < duty else '0'`.

### Hinweis

Eine nebenläufige `when ... else`-Zuweisung für `pwm_out`, der Zähler läuft im getakteten Prozess.
