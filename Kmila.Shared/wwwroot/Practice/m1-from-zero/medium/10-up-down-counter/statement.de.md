## 4-Bit-Auf/Ab-Zähler

Implementiere einen 4-Bit-Zähler mit Richtungssteuerung. Bei steigender Taktflanke:

- bei `dir = '1'` aufwärts: `count <= count + 1`
- bei `dir = '0'` abwärts: `count <= count - 1`

Ein asynchrones, aktiv-hohes `reset` setzt auf `0000`.

### Hinweis

Verwende `IEEE.NUMERIC_STD.ALL` und ein internes `unsigned(3 downto 0)`-Signal. Das Zählen läuft modulo 16 um.
