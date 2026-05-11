## Round-Robin-Arbiter — Grant-Rotation

Ein 4-Eingang-Round-Robin-Arbiter merkt sich `last_grant` (Index des zuletzt Bedienten). Bei jedem Takt erhält der nächste Anforderer in zyklischer Reihenfolge nach `last_grant` den Grant. Ohne `req`-Bit kein Grant.

Implementiere die `case`-Anweisung, die `nxt_grant` aus `last_grant` und `req` bildet. Die Zustandsmaschine und das `last_grant`-Register sind außerhalb der Markierungen bereits verdrahtet.

Nur die Zeilen zwischen `-- BEGIN_EDIT` und `-- END_EDIT` werden bewertet.
