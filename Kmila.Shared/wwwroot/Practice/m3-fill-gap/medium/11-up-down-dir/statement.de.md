## Auf-/Abwärts-Zähler — Richtungs-Zeile füllen

Ein synchroner 4-Bit-Zähler ist **fast** fertig. Reset,
Flankenerkennung und `q <= std_logic_vector(count)` sind bereits
verdrahtet.

Implementiere die Richtungssteuerung im steigenden Taktzweig:

* `dir = '1'` → `count ← count + 1`
* `dir = '0'` → `count ← count - 1`

### Spezifikation

* Asynchroner Reset: `count ← 0000`.
* Der Zähler läuft frei über (4-Bit-Unsigned).

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Reset und
Ausgabe sind gesperrt.

### Hinweis

Kurzes `if dir = '1' then … else … end if;` reicht.
