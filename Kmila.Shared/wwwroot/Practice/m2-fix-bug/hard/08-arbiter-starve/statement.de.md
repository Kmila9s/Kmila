## 4-fach-Arbiter — repariere die Verhungerung

Vier Agenten treiben `req(3..0)`. Der Arbiter muss pro Takt genau
ein Grant ausgeben, aber die aktuelle Implementierung ist ein
einfacher Fest-Prioritäts-Encoder: die aktive Anforderung mit dem
kleinsten Index gewinnt immer. Wenn `req(0)` dauernd hoch ist,
**verhungert `req(3)`**.

### Spezifikation

* Synchroner, high-aktiver Reset.
* Pro Takt ist genau ein Bit von `grant` `'1'` (oder `0000`, falls
  `req = 0000`).
* Sind mehrere Anfragen gleichzeitig aktiv, muss der Arbiter
  rotieren — jeder Anforderer kommt in höchstens 4 Takten dran.

### Hinweis

Ein Round-Robin-Arbiter merkt sich den **zuletzt vergebenen
Index** und beginnt seinen Scan bei `last_grant + 1` (mod 4). Die
erste aktive Anforderung in diesem Scan gewinnt.
