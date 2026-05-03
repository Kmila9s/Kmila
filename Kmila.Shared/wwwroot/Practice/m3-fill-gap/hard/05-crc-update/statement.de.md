## CRC-8-Update-Schritt — Lücke füllen

Ein serielles CRC-8-Register ist **fast** fertig. Das 8-Bit-
Register `r`, die Polynomkonstante `POLY = x"07"`, der
asynchrone Reset und die Ausgabe `crc <= r;` sind verdrahtet.
Im Prozess ist `fb : STD_LOGIC` schon als Variable deklariert.

Deine Aufgabe: den **Ein-Bit-CRC-Schritt** auf jeder steigenden
Flanke schreiben.

### Spezifikation

* Feedback-Bit berechnen:
  ```vhdl
  fb := r(7) xor din;
  ```
* Wenn `fb = '1'`, schiebe `r` um eins nach links (LSB wird
  `'0'`) und XOR-verknüpfe mit `POLY`. Sonst nur schieben:
  ```vhdl
  if fb = '1' then
      r <= (r(6 downto 0) & '0') xor POLY;
  else
      r <= r(6 downto 0) & '0';
  end if;
  ```

### Wo schreiben

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden
bewertet.
