## Détecteur de front montant

Génère une impulsion d'un cycle sur `pulse` à chaque transition `0→1` de `d`. Un `reset` asynchrone actif haut efface l'état interne.

### Spécification

- Interne : `signal d_prev : std_logic := '0';`.
- À chaque front : `d_prev <= d;`.
- Sortie combinatoire : `pulse = '1'` quand `d = '1' and d_prev = '0'`.

### Indice

Compare l'entrée courante `d` à sa valeur précédente échantillonnée pour détecter le front montant.
