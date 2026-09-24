## Ampel-Tabelle — Lücke füllen

Ein Ampel-Controller mit drei Zuständen ist **fast** fertig.
Das Zustandsregister, der asynchrone Reset und die Ausgabe
`state <= cur;` sind bereits verdrahtet.

Deine Aufgabe ist es, das **kombinatorische Folgezustands-CASE**
zu schreiben, das `nxt` aus `cur` auswählt.

### Spezifikation

* Die FSM durchläuft drei kodierte Zustände:
  * `00` → `01` → `10` → `00` → …
* Verwende `case cur is … end case;` mit expliziten Zweigen für
  `"00"`, `"01"`, `"10"` und einem `when others =>`, der nach
  `"00"` zurückkehrt.
* Der Block ist rein kombinatorisch — **keine** Taktflanke
  hinzufügen.

### Wo schreiben

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden
bewertet. Alles außerhalb (Entity, Register, Reset, Ausgang) ist
gesperrtes Gerüst.
