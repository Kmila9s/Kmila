## CORDIC — Drehrichtung verkehrt

Eine CORDIC-Iteration soll bei `dir = '1'` gegen den Uhrzeigersinn drehen:
* `x' = x - y`
* `y' = y + x`

Und im Uhrzeigersinn bei `dir = '0'` (Vorzeichen getauscht).

Der Starter verdrahtet `dir` falsch — `dir = '1'` ergibt CW-Drehung. Finde und korrigiere die Bedingung, die die Formeln auswählt.
