## Diviseur d'horloge par 4

Génère une horloge de sortie à un quart de la fréquence d'entrée. La méthode la plus simple : compter les fronts montants et émettre le bit fort d'un compteur 2 bits — il bascule tous les 4 cycles.

### Spécification

- Interne : `signal cnt : unsigned(1 downto 0) := (others => '0');`.
- À chaque front : `cnt <= cnt + 1;`.
- Sortie : `clk_out <= cnt(1);`.

### Indice

`cnt(1)` bascule après `00 → 01 → 10 → 11`, ce qui donne `clk/4`.
