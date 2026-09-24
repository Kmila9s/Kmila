## Booth-Multiplikator — falsche Verschiebung

Eine Booth-Iteration muss eine **arithmetische** Rechtsverschiebung des Akkumulators durchführen (Vorzeichenerweiterung). Der Starter benutzt eine **logische** Verschiebung (Nullen), wodurch negative Werte verfälscht werden.

Finde die Zeile, die vor dem Shift in `unsigned` konvertiert, und ersetze sie durch `shift_right(signed(...), 1)`, damit das Vorzeichenbit weitergetragen wird.
