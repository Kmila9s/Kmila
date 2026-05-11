## PWM — off-by-one

Das PWM-Modul soll `pwm_out` auf 1 setzen, solange der frei laufende Zähler **strikt kleiner als `duty`** ist.

Der Starter vergleicht gegen `duty - 1`, also ist jeder Tastgrad einen Tick zu kurz. Finde den Vergleich und entferne das `- 1`.

```vhdl
pwm_out <= '1' when count < unsigned(duty) else '0';
```
