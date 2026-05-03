## Registre 4 bits chargeable — remplis la branche de chargement

Un registre 4 bits à chargement synchrone est partiellement écrit.
Le reset, la détection du front montant et la sortie `q <= qi` sont
déjà en place.

Tu dois implémenter la **branche de chargement** dans le front montant :

* si `load = '1'` → `qi ← d`
* si `load = '0'` → `qi` conserve sa valeur

### Spécification

* Reset asynchrone : `qi ← 0000`.
* Sur `rising_edge(clk)` :
  * `load = '1'` capture `d`.
  * `load = '0'` maintient.
* `q` reflète `qi` en permanence.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le reset et
la sortie sont verrouillés.

### Astuce

Un `if load = '1' then qi <= d; end if;` suffit — VHDL conserve `qi`
en l'absence d'affectation.
