## Compteur up/down — remplis la ligne de direction

Un compteur synchrone 4 bits est **presque** complet. Reset,
détection du front montant et `q <= std_logic_vector(count)` sont déjà
câblés.

Tu dois implémenter le contrôle de direction sur le front montant :

* `dir = '1'` → `count ← count + 1`
* `dir = '0'` → `count ← count - 1`

### Spécification

* Reset asynchrone : `count ← 0000`.
* Le compteur boucle (arithmétique unsigned 4 bits).

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le reset et
la sortie sont verrouillés.

### Astuce

Un court `if dir = '1' then … else … end if;` suffit.
