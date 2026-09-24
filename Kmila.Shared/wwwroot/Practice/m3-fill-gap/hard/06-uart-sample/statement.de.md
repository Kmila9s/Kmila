## UART-Bitmittensampling — Lücke füllen

Bei einem 16×-überabgetasteten UART-Empfänger fällt die Bit-
Entscheidung **in der Mitte des Bitfensters** — also wenn der
Oversample-Zähler den Wert 8 erreicht. Taktflanken-Erkennung,
asynchroner Reset und Ausgang `sample_now` sind bereits
verdrahtet.

Deine Aufgabe: schreibe den Komparator, der aus dem laufenden
`bit_count` den **Sample-Now-Strobe** erzeugt.

### Spezifikation

* `bit_count` ist `STD_LOGIC_VECTOR(3 downto 0)` (Index 0..15).
* Setze `sample_now <= '1'` genau dann, wenn `bit_count = "1000"` (8).
* Sonst `sample_now <= '0'`.
* Der Block ist innerhalb von `rising_edge(clk)`; die Entscheidung
  ist daher einen Takt später sichtbar.

### Hinweis

```vhdl
if unsigned(bit_count) = "1000" then
    sample_now <= '1';
else
    sample_now <= '0';
end if;
```

### Wo schreiben

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden
bewertet.
