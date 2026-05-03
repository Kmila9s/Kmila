## Compteur 4 bits — bug du reset asynchrone

Le bloc doit être un compteur 4 bits avec **reset asynchrone actif haut**.

### Spécification

* Sur `rising_edge(clk)` : `count <- count + 1`.
* Lorsque `reset = '1'` : `count` passe immédiatement à `"0000"`, sans attendre l'horloge.
* `q` reflète `count`.

### Ce qui cloche

Si tu pulses `reset` entre deux fronts d'horloge, le compteur **ne** se remet **pas** à zéro immédiatement : il le fait au prochain front. Le corps du process est correct ; le bug se trouve une ligne plus haut.

### Indice

Pour que le reset soit vraiment asynchrone, il doit figurer dans la **liste de sensibilité** du process à côté de l'horloge.
