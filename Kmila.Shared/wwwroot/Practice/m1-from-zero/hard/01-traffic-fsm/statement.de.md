## Ampel-FSM

Implementiere einen 3-Zustand-Ampelregler. Bei jeder steigenden Taktflanke durchlaufe `"00"` (rot) → `"01"` (grün) → `"10"` (gelb) → zurück zu `"00"`.

### Spezifikation

- Reset asynchron: `if reset='1'` setzt `state` auf `"00"`.
- Ein Schritt pro Takt; kein Frequenzteiler — der Testbench taktet die FSM bei jeder Flanke weiter.
- `state` ist ein 2-Bit `STD_LOGIC_VECTOR`.

### Hinweis

Halte den Zustand in einem `unsigned(1 downto 0)` Signal und wähle den Folgezustand mit `if/elsif`.
