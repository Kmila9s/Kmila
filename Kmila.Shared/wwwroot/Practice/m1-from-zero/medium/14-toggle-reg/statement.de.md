## Toggle-Register mit Enable

Baue ein 1-Bit-Toggle-Register. Bei `en = '1'` und steigender Taktflanke wird `q` invertiert. Bei `en = '0'` bleibt `q` stehen. Ein asynchrones, aktiv-hohes `reset` setzt `q` auf `0`.

### Spezifikation

- Intern: `signal qi : std_logic := '0';`.
- Prozess: `if en = '1' then qi <= not qi;`.
- Ausgabe: `q <= qi;`.

### Hinweis

Fast identisch zu einem T-Flipflop. Das Enable wirkt wie das Toggle-Eingangssignal.
