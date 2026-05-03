## Flankendetektor — falsche Flanke

Verlangt war ein **Steigende-Flanke**-Detektor auf `d`.

### Spezifikation

* `d` wird bei jeder `rising_edge(clk)` in `d_prev` gesampelt.
* `pulse = '1'` für einen Takt, wenn `d` im vorigen Takt `'0'` war und jetzt `'1'` ist.
* Asynchroner High-aktiver `reset` setzt `d_prev` auf `'0'`.

### Was schiefläuft

Der kombinatorische Ausdruck für `pulse` prüft die falsche Polarität: er feuert, wenn `d` von `'1'` auf `'0'` fällt, also auf der **fallenden** Flanke. Wenn `d` steigt (Spec-Ziel), bleibt `pulse` `'0'`, und wenn `d` fällt, pulst es fälschlicherweise.

### Hinweis

Vertausche die beiden Vergleiche in der bedingten Zuweisung an `pulse`.
