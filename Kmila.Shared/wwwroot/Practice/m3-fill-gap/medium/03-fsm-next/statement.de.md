## 3-Zustands-FSM — nächster Zustand

Eine Moore-FSM mit drei Zuständen ist teilweise geschrieben. Das
Zustandsregister, der Reset und die Übergänge aus `"00"` und `"10"`
sind bereits vorhanden.

Du musst die **Nächst-Zustands-Zuweisung für `"01"`** vervollständigen:

* falls `x = '1'` → `nxt ← "10"`
* falls `x = '0'` → `nxt ← "00"`

### Spezifikation

* Zustände: `"00"`, `"01"`, `"10"` (direkt kodiert).
* Asynchroner Reset versetzt die FSM nach `"00"`.
* `state` gibt den aktuellen Zustand aus.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Die anderen
`case`-Zweige und das Zustandsregister sind gesperrt.

### Hinweis

Ein kurzes `if x = '1' then … else … end if;` reicht.
