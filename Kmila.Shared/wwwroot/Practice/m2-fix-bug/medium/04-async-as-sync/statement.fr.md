## Bascule D — un reset asynchrone qui ne l'est pas

Une bascule D avec **reset asynchrone actif haut** était demandée.

### Spécification

* Sur `rising_edge(clk)` : `q <- d`.
* Quand `reset = '1'` : `q` passe à `'0'` immédiatement, sans attendre l'horloge.

### Ce qui cloche

Si tu actives `reset` entre deux fronts, `q` ne se remet pas à zéro tout de suite : le simulateur ne réévalue le process que lorsque `clk` change. Le reset est en fait devenu **synchrone**, ce qui viole la spec.

### Indice

La liste de sensibilité détermine quand un process se réévalue. Un reset asynchrone doit y figurer à côté de `clk`.
