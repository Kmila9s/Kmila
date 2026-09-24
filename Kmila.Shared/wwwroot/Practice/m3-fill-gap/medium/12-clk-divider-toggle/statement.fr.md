## Diviseur d'horloge — remplis la bascule

Un diviseur d'horloge 4 bits est **presque** prêt : le compteur
modulo 10 (`TOP = 9`), le reset et la sortie `clk_out <= clk_reg`
sont déjà câblés. Quand le compteur atteint `TOP`, il revient à `0`.

Tu dois implémenter ce qui arrive à `clk_reg` **au même instant**
que le débordement du compteur : il doit basculer. En dehors de cet
événement, `clk_reg` conserve sa valeur.

### Spécification

* Reset asynchrone : `count ← 0`, `clk_reg ← '0'`.
* Sur `rising_edge(clk)` :
  * si `count = TOP` → `count ← 0`, `clk_reg ← not clk_reg`
  * sinon → `count ← count + 1`

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le compteur,
le reset et la sortie sont verrouillés.

### Astuce

`clk_reg <= not clk_reg;`
