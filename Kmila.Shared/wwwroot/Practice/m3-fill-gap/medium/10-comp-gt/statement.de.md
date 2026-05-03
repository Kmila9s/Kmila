## 4-Bit-„Größer als"-Komparator — Lücke füllen

Die Entity liefert zwei 4-Bit-Operanden `a` und `b` sowie einen
Ausgang `gt`. Die Architektur ist außer dem editierbaren Bereich leer.

Du musst `gt` treiben:

* `gt = '1'` wenn `a > b` (unsigned)
* `gt = '0'` sonst

### Spezifikation

* `a` und `b` als **unsigned** behandeln.
* Rein kombinatorisch — kein Takt.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Die Entity
ist gesperrt.

### Hinweis

`gt <= '1' when unsigned(a) > unsigned(b) else '0';`
