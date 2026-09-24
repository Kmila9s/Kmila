## FIFO-Lesezeiger — Lücke füllen

Das Gerüst einer kleinen FIFO hat **Schreibzeiger**, **Empty-Flag**
und **Reset** bereits verdrahtet. Das Empty-Signal wird so
berechnet: `is_empty <= '1' when wr_ptr = rd_ptr else '0';`.

Deine Aufgabe: den **Lesezeiger** korrekt vorwärtsschalten — er
darf nur dann inkrementieren, wenn der Host `pop = '1'` setzt
**und** die FIFO **nicht leer** ist (kein Pop bei Leer).

### Spezifikation

* Innerhalb des vorhandenen `rising_edge(clk)`-Zweigs:
  ```vhdl
  if pop = '1' and is_empty = '0' then
      rd_ptr <= rd_ptr + 1;
  end if;
  ```
* `wr_ptr` und `is_empty` nicht anfassen.
* Pops, die bei Leer eintreffen, werden **stillschweigend
  ignoriert**.

### Wo schreiben

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden
bewertet.
