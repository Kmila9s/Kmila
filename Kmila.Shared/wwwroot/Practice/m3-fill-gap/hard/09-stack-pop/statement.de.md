## Stack-Pop mit Empty-Schutz — Lücke füllen

Ein einfacher Stack-Pointer ist **fast** fertig. Reset, **Push**-
Zweig und der kombinatorische `empty`-Ausgang sind bereits
verdrahtet.

Deine Aufgabe: schreibe den **Pop**-Zweig mit Underflow-Schutz.

### Spezifikation

* Innerhalb von `rising_edge(clk)`, nach dem Push-Zweig:
  ```vhdl
  if pop = '1' and sp /= 0 then
      sp <= sp - 1;
  end if;
  ```
* Pops auf leeren Stack werden **stillschweigend ignoriert**.
* Push-Zweig und Empty-Flag nicht ändern.

### Wo schreiben

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden
bewertet.
