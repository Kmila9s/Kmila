## FSM 3 états — état suivant

Une FSM Moore à 3 états est partiellement écrite. Le registre d'état,
le reset et les transitions depuis `"00"` et `"10"` sont déjà en place.

Tu dois compléter l'**affectation de l'état suivant pour l'état `"01"`** :

* si `x = '1'` → `nxt ← "10"`
* si `x = '0'` → `nxt ← "00"`

### Spécification

* États : `"00"`, `"01"`, `"10"` (encodés directement).
* Le reset asynchrone place la FSM en `"00"`.
* `state` reflète l'état courant.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Les autres
branches du `case` et le registre d'état sont verrouillés.

### Astuce

Un court `if x = '1' then … else … end if;` suffit.
