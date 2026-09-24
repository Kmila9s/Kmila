## Détecteur de front descendant SPI — complète le trou

Un front-end SPI courant doit détecter le **front descendant**
du `sclk` lent depuis le domaine rapide `clk`. Pour cela on
mémorise un registre d'historique `sclk_prev` et on le compare
au `sclk` courant.

Le registre, le reset asynchrone et le `process(clk, reset)`
sont déjà câblés. Tu n'écris que l'**expression de front** qui
produit le strobe `sample`.

### Spécification

* Front descendant : `sclk_prev = '1'` ET `sclk = '0'`.
* Une seule affectation concurrente :
  ```vhdl
  sample <= sclk_prev and (not sclk);
  ```
* Le strobe dure un cycle de `clk`.

### Où écrire

Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est
évaluée.
