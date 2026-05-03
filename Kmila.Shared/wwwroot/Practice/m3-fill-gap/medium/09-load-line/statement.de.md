## 4-Bit-Lade-Register — Lade-Zweig füllen

Ein 4-Bit-Register mit synchronem Laden ist teilweise geschrieben.
Reset, Flankenerkennung und `q <= qi` sind bereits vorhanden.

Implementiere den **Lade-Zweig** im steigenden Taktblock:

* falls `load = '1'` → `qi ← d`
* falls `load = '0'` → `qi` behält seinen Wert

### Spezifikation

* Asynchroner Reset: `qi ← 0000`.
* Bei `rising_edge(clk)`:
  * `load = '1'` übernimmt `d`.
  * `load = '0'` hält.
* `q` spiegelt `qi`.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Reset und
Ausgabe sind gesperrt.

### Hinweis

Ein `if load = '1' then qi <= d; end if;` reicht — ohne Zuweisung
hält VHDL `qi`.
