## Anstiegsflankenerkenner

Erzeuge einen Ein-Takt-Impuls auf `pulse`, sobald `d` von `0` auf `1` wechselt. Ein asynchrones, aktiv-hohes `reset` löscht den internen Zustand.

### Spezifikation

- Intern: `signal d_prev : std_logic := '0';`.
- Bei jeder Flanke: `d_prev <= d;`.
- Kombinatorischer Ausgang: `pulse = '1'`, wenn `d = '1' and d_prev = '0'`.

### Hinweis

Vergleiche den aktuellen `d` mit dem zuvor gespeicherten Wert, um die Anstiegsflanke zu erkennen.
