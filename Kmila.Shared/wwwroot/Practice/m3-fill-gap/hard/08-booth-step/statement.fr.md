## Multiplicateur de Booth — ajout/soustraction conditionnels — complète le trou

Un multiplicateur de Booth 4×4 est **presque** complet.
L'accumulateur `acc`, les bits de contrôle `q0` et `q_minus1`,
et le multiplicande étendu sur 8 bits `m_ext` sont déjà mis à
jour à l'activation de `start`. Au début de chaque pas, la
variable `lsb_pair := q0 & q_minus1;` est calculée.

Ta tâche : écrire la **suite conditionnelle ajout/soustraction**
du pas (quand `start = '0'`).

### Spécification

* Examine la paire `lsb_pair = q0 & q_minus1` :
  * `"01"` → ajoute : `acc <= acc + m_ext;`
  * `"10"` → soustrais : `acc <= acc - m_ext;`
  * `"00"` ou `"11"` → rien : `acc <= acc;`
* Utilise `case lsb_pair is … end case;`.
* Ne touche pas à `q0`, `q_minus1` ni `m_ext`.

### Où écrire

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont
évaluées.
