## CORDIC — sens de rotation inversé

Une itération CORDIC doit tourner anti-horaire quand `dir = '1'` :
* `x' = x - y`
* `y' = y + x`

Et horaire quand `dir = '0'` (signes inversés).

Le starter câble `dir` à l'envers : `dir = '1'` produit une rotation horaire. Trouve et corrige le conditionnel qui sélectionne les formules.
