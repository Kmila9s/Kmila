## Comparateur PWM — complète le trou

Un compteur libre 4 bits est déjà câblé. Il s'incrémente à
chaque coup d'horloge et boucle naturellement de `1111` à
`0000` (période PWM de 16 pas).

Tu dois ajouter le **comparateur** qui transforme le compteur
en signal PWM : `pwm_out` doit être **haut** tant que
`count < duty`, et **bas** sinon.

### Spécification

* `count` est `unsigned(3 downto 0)`.
* `duty` est `STD_LOGIC_VECTOR(3 downto 0)` — convertis-le avec
  `unsigned(duty)`.
* Utilise une affectation conditionnelle concurrente :
  `pwm_out <= '1' when count < unsigned(duty) else '0';`
* Si `duty = 0` la sortie reste basse ; si `duty = 15` elle est
  haute 15 ticks sur 16.

### Où écrire

Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est
évaluée. Ne modifie pas le processus du compteur, il est
verrouillé.
