## Taktteiler durch 4

Erzeuge ein Ausgangstaktsignal mit einem Viertel der Eingangsfrequenz. Einfachste Methode: steigende Flanken zählen und das höchste Bit eines 2-Bit-Zählers ausgeben — es kippt alle 4 Eingangstakte.

### Spezifikation

- Intern: `signal cnt : unsigned(1 downto 0) := (others => '0');`.
- Bei jeder Flanke: `cnt <= cnt + 1;`.
- Ausgabe: `clk_out <= cnt(1);`.

### Hinweis

`cnt(1)` kippt nach `00 → 01 → 10 → 11`, also alle 4 Takte — `clk/4`.
