## Inverter — Bug finden und beheben

Der Autor sollte ein NICHT-Gatter (Inverter) schreiben: `y` soll
das **logische Komplement** von `a` sein. Aktuell wird `a`
einfach an `y` durchgereicht — es verhält sich wie ein Puffer.

Finde den Fehler und mache daraus einen echten Inverter.

### Spezifikation

| `a` | `y` |
|:---:|:---:|
|  0  |  1  |
|  1  |  0  |

### Hinweis

Ein einziger VHDL-Operator erledigt alles.
