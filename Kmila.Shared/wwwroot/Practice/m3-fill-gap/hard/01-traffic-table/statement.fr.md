## Table de feu tricolore — complète le trou

Un contrôleur à 3 états est **presque** terminé. Le registre
d'état, le reset asynchrone et l'affectation `state <= cur;`
sont déjà câblés.

Ton rôle est d'écrire le **CASE combinatoire d'état suivant**
qui choisit `nxt` à partir de `cur`.

### Spécification

* La FSM parcourt trois états codés :
  * `00` → `01` → `10` → `00` → …
* Utilise `case cur is … end case;` avec des branches explicites
  pour `"00"`, `"01"`, `"10"` et un `when others =>` qui retourne
  à `"00"`.
* Le bloc est purement combinatoire — n'ajoute **pas** de front
  d'horloge.

### Où écrire

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont
évaluées. Tout ce qui est en dehors (entité, registre, reset,
sortie) est verrouillé.
