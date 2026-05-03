## Auf-/Abwärtszähler — Richtung ignoriert

Verlangt war ein 4-Bit-Auf-/Abwärtszähler, gesteuert durch `dir`:

* `dir = '1'` → aufwärts: `count <- count + 1`.
* `dir = '0'` → abwärts: `count <- count - 1`.
* Asynchroner High-aktiver `reset` setzt `count` auf 0.

### Was schiefläuft

Der Starter zählt nur hoch — `dir` wird ignoriert. Bei `dir = '0'` läuft die Zählung weiter `0 → 1 → 2 → …` statt `0 → 15 → 14 → …`.

### Hinweis

Setze ein `if dir = '1' then count <= count + 1; else count <= count - 1; end if;` in den Rising-Edge-Zweig.
