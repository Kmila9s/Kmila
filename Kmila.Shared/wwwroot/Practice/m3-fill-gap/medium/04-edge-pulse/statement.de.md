## Puls bei steigender Flanke — fülle die Lücke

Ein zweistufiger Sampler ist verdrahtet: In jedem Taktzyklus werden der
vorherige und der aktuelle Wert von `d` als `d_prev` bzw. `d_curr`
gespeichert. Der Reset des Registers ist ebenfalls vorhanden.

Du musst `pulse` erzeugen: Er soll für **genau einen Taktzyklus** auf
`'1'` gehen, wenn `d` von low nach high wechselt, sonst `'0'`.

### Spezifikation

* `pulse` ist eine kombinatorische Funktion von `d_curr` und `d_prev`.
* `pulse = '1'` gdw. `d_curr = '1'` und `d_prev = '0'`.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Sampler und
Reset sind gesperrt.

### Hinweis

`pulse <= d_curr and (not d_prev);`
