## Générateur PWM (4 bits)

Implémente un modulateur de largeur d'impulsion. Un compteur libre `cnt` 4 bits s'incrémente à chaque cycle et reboucle à 0 après 15. `pwm_out` est haut tant que `cnt < duty`.

### Spécification

- Compteur `unsigned` 4 bits qui s'incrémente à chaque front montant.
- Reset met le compteur à 0.
- Comparaison combinatoire : `pwm_out = '1' when cnt < duty else '0'`.

### Indice

Utilise une affectation concurrente `when ... else` pour `pwm_out` ; le compteur tourne dans le process cadencé.
