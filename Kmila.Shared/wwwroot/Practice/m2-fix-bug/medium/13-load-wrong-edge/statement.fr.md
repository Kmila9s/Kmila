## Registre chargeable — mauvais front d'horloge

Un registre 4 bits à chargement synchrone était demandé.

### Spécification

* Reset asynchrone actif haut met `q` à `"0000"`.
* Sur `rising_edge(clk)`, si `load = '1'` alors `q <- d`. Sinon `q` se maintient.

### Ce qui cloche

Le registre est cadencé sur le front **descendant** au lieu du front montant. Les données arrivent avec un demi-cycle de retard et le modèle temporel est faux.

### Indice

`falling_edge(clk)` et `rising_edge(clk)` sont symétriques. Regarde lequel le process utilise.
