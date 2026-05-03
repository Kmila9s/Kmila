## T-Flipflop

Implementiere ein Toggle-(T)-Flipflop. Bei steigender Taktflanke wird `q` invertiert, falls `t = '1'`. Bei `t = '0'` bleibt `q` stehen. Ein asynchrones, aktiv-hohes `reset` setzt `q` auf `0`.

### Spezifikation

- Intern: `signal qi : std_logic := '0';`.
- Prozessrumpf: `if t = '1' then qi <= not qi;`.
- Ausgabe: `q <= qi;`.

### Hinweis

Treibe `q` aus einem internen `qi`, da Ausgänge in VHDL innerhalb eines Prozesses nicht gelesen werden dürfen.
