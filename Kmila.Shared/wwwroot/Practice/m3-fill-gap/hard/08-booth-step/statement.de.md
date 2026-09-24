## Booth-Multiplikator — bedingtes Add/Sub — Lücke füllen

Ein 4×4-Booth-Multiplikator ist **fast** komplett. Der
Akkumulator `acc`, die Kontrollbits `q0` und `q_minus1` und der
auf 8 Bit erweiterte Multiplikand `m_ext` werden bei `start`
schon befüllt. Pro Schritt wird die Variable
`lsb_pair := q0 & q_minus1;` berechnet.

Deine Aufgabe: schreibe das **bedingte Add/Sub** des Schritts
(wenn `start = '0'`).

### Spezifikation

* Untersuche das Paar `lsb_pair = q0 & q_minus1`:
  * `"01"` → addiere: `acc <= acc + m_ext;`
  * `"10"` → subtrahiere: `acc <= acc - m_ext;`
  * `"00"` oder `"11"` → nichts: `acc <= acc;`
* Verwende `case lsb_pair is … end case;`.
* `q0`, `q_minus1` und `m_ext` nicht anfassen.

### Wo schreiben

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden
bewertet.
