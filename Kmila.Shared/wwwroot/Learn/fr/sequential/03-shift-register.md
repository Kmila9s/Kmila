# Exemple : registre à décalage 8 bits

Un registre à décalage est une rangée de bascules chaînées. Chaque front d'horloge décale les bits stockés d'une position, et un nouveau bit entre à une extrémité. Ils sont au cœur des UART, contrôleurs SPI, moteurs CRC et générateurs de nombres pseudo-aléatoires LFSR.

## Ce que fait cet exemple

`shift8` est un registre à décalage **série-en, parallèle-out** :

- `din` est le nouveau bit qui arrive à chaque front montant.
- `r` est le registre 8 bits interne.
- `q` expose `r` à l'extérieur comme un octet parallèle.

## Le corps

```vhdl
signal r : std_logic_vector(7 downto 0) := (others => '0');
...
process(clk, reset)
begin
  if reset = '1' then
    r <= (others => '0');
  elsif rising_edge(clk) then
    r <= r(6 downto 0) & din;
  end if;
end process;

q <= r;
```

La magie est sur cette ligne :

```vhdl
r <= r(6 downto 0) & din;
```

`&` est l'opérateur de **concaténation**. `r(6 downto 0)` est les 7 bits de poids faible du registre courant. On colle `din` à droite, pour faire une nouvelle valeur 8 bits, et on l'assigne à `r`.

Comme c'est une affectation de signal dans un process cadencé, le décalage a lieu à chaque front montant.

## Parcours du chronogramme

La config de sim pilote `din` selon un motif : `1, 0, 1, 1, 0, 1, 0, 1`. Regarde `q` au fil du temps :

- Après le premier tick : `q = 00000001`
- Après le deuxième : `q = 00000010`
- Après le troisième : `q = 00000101`
- … et ainsi de suite. Le nouveau bit atterrit en LSB ; les bits plus anciens marchent vers le MSB.

Si tu dézoomes, le bit de poids fort (bit 7) reçoit la première valeur de `din` exactement 8 cycles plus tard. C'est la latence aller-retour d'un registre 8 bits.

## À toi de jouer

- **Décale à droite** au lieu de gauche. Inverse la concaténation : `r <= din & r(7 downto 1);`.
- **Chargement parallèle.** Ajoute une entrée `load` et un bus `din_parallel`. Quand `load = '1'` au front d'horloge, le registre prend tout le bus d'un coup au lieu de décaler.
- **Sortie série.** Ajoute `dout <= r(7);`. C'est devenu un registre série-in, série-out (SISO) — le premier bit envoyé sur `din` apparaît sur `dout` 8 cycles plus tard. C'est comme ça que le délai bit-par-bit du RX UART fonctionne.
