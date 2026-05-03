## Registre avec load (4 bits)

Implémente un registre 4 bits avec chargement synchrone. Au front montant, si `load = '1'`, copie `d` dans le registre. Sinon, conserve la valeur précédente. Un `reset` asynchrone actif haut le remet à `0000`.

### Spécification

- Interne : `signal r : std_logic_vector(3 downto 0) := (others => '0');`.
- Process : `if load = '1' then r <= d;`.
- Sortie : `q <= r;`.

### Indice

Bloc de base courant — même forme qu'un compteur, mais la nouvelle valeur vient de `d` au lieu de `count + 1`.
