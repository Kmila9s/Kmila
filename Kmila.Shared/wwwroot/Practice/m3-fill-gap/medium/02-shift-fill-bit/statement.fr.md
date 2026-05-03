## Sélecteur de bit de remplissage — remplis le trou

Un registre à décalage 8 bits est déjà câblé : le reset le vide et à
chaque front montant le registre décale et accepte un bit de
remplissage `feed` à droite.

Tu dois implémenter le choix de `feed` :

* `mode = '1'` → `feed` suit `din`
* `mode = '0'` → `feed` est forcé à `'0'`

### Spécification

* Reset asynchrone : `sreg ← 0000_0000`
* Sur `rising_edge(clk)` : `sreg ← sreg(6..0) & feed`
* `q` reflète `sreg` en permanence.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le processus
de décalage, le reset et la sortie sont verrouillés.

### Astuce

Une affectation concurrente `when … else` suffit.
