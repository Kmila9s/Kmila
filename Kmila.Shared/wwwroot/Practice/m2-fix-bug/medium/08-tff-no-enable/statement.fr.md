## Bascule T — entrée d'autorisation ignorée

Une bascule T avec **enable de toggle** `t` était demandée.

### Spécification

* `reset` asynchrone actif haut met `q` à `'0'`.
* Sur `rising_edge(clk)` :
  * Si `t = '1'`, bascule : `q <- not q`.
  * Si `t = '0'`, conserve la valeur.

### Ce qui cloche

Le code bascule `q` à chaque front sans tenir compte de `t`. Le comportement est celui d'un diviseur d'horloge, pas d'une bascule T contrôlable.

### Indice

L'inversion doit être encadrée par un `if t = '1' then ...` à l'intérieur de la branche front montant.
