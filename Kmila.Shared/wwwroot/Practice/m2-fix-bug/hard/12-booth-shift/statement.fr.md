## Multiplicateur Booth — décalage faux

Une itération Booth doit effectuer un décalage droite **arithmétique** sur l'accumulateur (extension de signe). Le starter fait un décalage droite **logique** (remplissage 0), corrompant toute valeur négative.

Trouve la ligne qui convertit en `unsigned` avant le décalage et remplace-la par `shift_right(signed(...), 1)` pour propager le bit de signe.
