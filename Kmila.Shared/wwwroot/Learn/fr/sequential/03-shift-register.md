# Exemple : registre à décalage 8 bits

Un registre à décalage est une rangée de flip-flops chaînés entre eux. Chaque front d'horloge décale les bits stockés d'une position, et un nouveau bit entre par une extrémité. Ils sont au cœur des UART, des contrôleurs SPI, des moteurs CRC et des générateurs de nombres aléatoires à base de LFSR.

## Ce que fait cet exemple

`shift8` est un registre à décalage **entrée série, sortie parallèle** :

- `din` est le nouveau bit qui arrive à chaque front montant d'horloge.
- `r` est le registre interne 8 bits.
- `q` expose `r` à l'extérieur sous forme d'octet parallèle.

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

`&` est l'opérateur de **concaténation**. `r(6 downto 0)` représente les 7 bits de poids faible du registre courant. On colle `din` à l'extrémité droite, ce qui fait une nouvelle valeur 8 bits, et on l'assigne à `r`.

Comme il s'agit d'une assignation de signal dans un process cadencé, le décalage a lieu à chaque front montant.

## Promenade dans la forme d'onde

La config de sim pilote `din` à travers un motif : `1, 0, 1, 1, 0, 1, 0, 1`. Regarde `q` au fil du temps :

- Après le premier tick : `q = 00000001`
- Après le deuxième : `q = 00000010`
- Après le troisième : `q = 00000101`
- …et ainsi de suite. Le nouveau bit atterrit sur le LSB ; les anciens bits marchent vers le MSB.

Si tu dézoomes, tu verras le bit de poids fort (bit 7) recevoir la première valeur de `din` exactement 8 cycles d'horloge après son arrivée. C'est la latence aller-retour d'un registre à décalage 8 bits.

## À toi

- **Décale à droite.** Inverse la concaténation : `r <= din & r(7 downto 1);`.
- **Chargement parallèle.** Ajoute une entrée `load` et un bus `din_parallel`. Quand `load = '1'` à un front d'horloge, le registre prend tout le bus d'un coup au lieu de décaler.
- **Sortie série.** Ajoute `dout <= r(7);`. Maintenant c'est un registre série-entrée, série-sortie (SISO) — le premier bit que tu as envoyé sur `din` apparaît sur `dout` huit cycles plus tard. C'est comme ça que fonctionne le délai bit-à-bit du RX UART.
