## Compteur synchrone 4 bits

Construis un compteur 4 bits qui s'incrémente à chaque front montant. Un `reset` asynchrone actif haut remet la valeur à `0000`.

### Spécification

- Interne : `count : unsigned(3 downto 0) := (others => '0')`.
- À `rising_edge(clk)` : `count <= count + 1;`.
- Sortie : `q <= std_logic_vector(count);`.

### Indice

Utilise `IEEE.NUMERIC_STD.ALL` pour que `+ 1` fonctionne sur `unsigned`.
