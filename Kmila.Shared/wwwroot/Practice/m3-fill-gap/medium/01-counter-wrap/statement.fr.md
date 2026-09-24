## Compteur 3 bits avec wrap — remplis le trou

Un compteur synchrone 3 bits est **presque** prêt. Le reset, la
détection du front d'horloge et l'affectation de `q` sont déjà câblés.

Implémente le corps de la branche `rising_edge(clk)` pour que le
compteur parcoure `000 → 001 → … → 111 → 000` — il revient à zéro
après sept.

### Spécification

* Reset asynchrone : `count ← 000`
* Sur `rising_edge(clk)` :
  * si `count = 111` → `count ← 000`
  * sinon → `count ← count + 1`
* `q` reflète `count` en permanence.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le reste est
un échafaudage verrouillé attendu tel quel par le correcteur.

### Astuce

Compare `count` à `to_unsigned(7, 3)` (ou `"111"`) puis choisis entre
`(others => '0')` et `count + 1`.
