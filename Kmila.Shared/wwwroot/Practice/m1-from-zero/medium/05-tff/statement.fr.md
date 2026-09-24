## Bascule T

Implémente une bascule T (toggle). Au front montant, si `t = '1'`, la sortie `q` est inversée. Si `t = '0'`, `q` reste. Un `reset` asynchrone actif haut remet `q` à `0`.

### Spécification

- Interne : `signal qi : std_logic := '0';`.
- Corps du process : `if t = '1' then qi <= not qi;`.
- Sortie : `q <= qi;`.

### Indice

Pilote `q` depuis un signal interne `qi` car les sorties VHDL ne peuvent pas être lues dans un process.
