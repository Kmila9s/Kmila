## Registre à décalage 8 bits

Construis un registre à décalage 8 bits. À chaque front montant, décale le contenu d'une position vers la gauche et insère l'entrée série `din` au bit 0. Un `reset` asynchrone actif haut remet le registre à zéro.

### Spécification

- Signal interne : `r : std_logic_vector(7 downto 0)`.
- Règle de mise à jour : `r <= r(6 downto 0) & din;`.
- Sortie : `q <= r;`.

### Indice

Utilise `process(clk, reset)` avec `if reset = '1' then ... elsif rising_edge(clk) then ...`.
