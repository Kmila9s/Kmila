## Codeur Manchester

Encode un flux série `din` pour que chaque période de bit contienne une transition (0→1 pour `din=0`, 1→0 pour `din=1`).

### Spécification

- `phase` interne bascule à chaque coup d'horloge.
- Reset met `phase = '0'`.
- Sortie : `m <= din xor phase;`
- Résultat : avec `din=0`, `m` produit `0,1,0,1,...` → motif `'01'`. Avec `din=1`, motif `'10'`.

### Indice

Une bascule qui toggle plus un xor — le code-ligne le plus simple.
