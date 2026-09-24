## FSM à 3 états — état inatteignable

Une machine à états avec `S0` (`"00"`), `S1` (`"01"`), `S2` (`"10"`) était demandée.

### Spécification

* Reset asynchrone actif haut → `S0`.
* Sur `rising_edge(clk)` :
  * `S0 -> S1` toujours.
  * `S1 -> S2` quand `x = '1'`, sinon reste en `S1`.
  * `S2 -> S0` toujours.
* `state` sort le codage de l'état courant.

### Ce qui cloche

La transition `S1 -> S2` ne se déclenche jamais. Peu importe combien de temps `x` reste haut, la FSM reste en `S1` indéfiniment — `S2` est **inatteignable**. Le bug est dans la condition qui garde cette transition.

### Indice

Un signal ne peut pas valoir deux valeurs différentes en même temps. Regarde l'expression booléenne : c'est une contradiction.
