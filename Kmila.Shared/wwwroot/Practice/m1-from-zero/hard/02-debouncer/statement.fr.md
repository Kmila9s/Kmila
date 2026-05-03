## Anti-rebond de bouton

Filtre une entrée bruitée. La sortie `clean` ne change qu'après que `raw` est resté stable pendant **N = 4** cycles consécutifs.

### Spécification

- Mémorise la dernière valeur de `raw` et un compteur 2 bits.
- À chaque coup d'horloge : si `raw == last`, incrémente ; à saturation `"11"`, copie `raw` vers la sortie.
- Si `raw` change, remets le compteur à zéro et met à jour `last`.
- Reset : tout est remis à zéro, `clean = '0'`.

### Indice

Utilise `unsigned(1 downto 0)` pour le compteur et un `STD_LOGIC` pour le niveau stable.
