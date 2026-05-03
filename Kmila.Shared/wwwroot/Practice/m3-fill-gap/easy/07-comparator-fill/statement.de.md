## 4-Bit-Gleichheitskomparator — fülle die Lücke

Implementiere einen 4-Bit-Gleichheitskomparator. Der Ausgang `eq`
ist `1`, wenn die beiden 4-Bit-Vektoren `a` und `b` bitweise gleich
sind, sonst `0`.

### Verhalten

* `eq <= '1'` wenn `a = b`
* `eq <= '0'` sonst

### Tipp

Nutze eine nebenläufige `when ... else`-Zuweisung mit dem Operator
`=` auf den vollständigen Vektoren.

### Wo schreiben

Entity, Ports und Architektur-Gerüst sind bereits vorhanden. Nur
die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT` wird bewertet —
alles andere ist gesperrtes Gerüst.
