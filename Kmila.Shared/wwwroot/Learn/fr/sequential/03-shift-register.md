# Exemple : registre à décalage 8 bits

Un registre à décalage est une rangée de bascules chaînées ensemble. Chaque front d'horloge décale les bits stockés d'une position, et un nouveau bit entre par une extrémité. Ils sont au cœur des UART, des contrôleurs SPI, des moteurs CRC et des générateurs de nombres aléatoires à base de LFSR — la récompense pratique des bascules de la leçon précédente.

## Ce que fait cet exemple

`shift8` est un registre à décalage **série-entrée, parallèle-sortie** (SIPO) :

- `din` est le nouveau bit qui arrive à chaque front montant d'horloge.
- `r` est le registre interne de 8 bits.
- `q` expose `r` vers l'extérieur sous forme d'octet parallèle.

« Série en entrée » parce que les bits entrent un à la fois ; « parallèle en sortie » parce que vous pouvez lire les huit d'un coup. C'est exactement ce que fait un récepteur UART : il fait entrer les bits un par un depuis le fil, puis remet l'octet assemblé au reste du système.

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

`&` est l'opérateur de **concaténation**. `r(6 downto 0)` est les 7 bits de poids faible du registre actuel. On colle `din` à l'extrémité droite, ce qui fabrique une nouvelle valeur de 8 bits, et on la réaffecte à `r`. Comme c'est une affectation de signal à l'intérieur d'un process cadencé, le décalage a lieu à chaque front montant — et comme les signaux se mettent à jour à la fin du pas, le membre droit utilise l'*ancien* `r`, ce qui est exactement ce qu'un décalage exige.

## Ce ne sont que huit bascules en chaîne

Ne laissez pas le corps d'une ligne masquer le matériel. `r`, ce sont huit bascules D. L'affectation câble la sortie de chaque bascule vers l'entrée de la suivante, avec `din` qui alimente la première et le bit 7 qui tombe par le bout (ici jeté). Cette chaîne *est* le registre à décalage — la concaténation n'est qu'une façon compacte de décrire le câblage.

## Parcours du chronogramme

La config de sim pilote `din` selon un motif : `1, 0, 1, 1, 0, 1, 0, 1`. Observez `q` au fil du temps :

- Après le premier tick : `q = 00000001`
- Après le deuxième : `q = 00000010`
- Après le troisième : `q = 00000101`
- … et ainsi de suite. Le nouveau bit atterrit dans le LSB ; les bits plus anciens progressent vers le MSB.

Si vous dézoomez, vous verrez la première valeur de `din` atteindre le bit de poids fort (bit 7) exactement 8 cycles d'horloge après son arrivée. C'est la latence aller-retour d'un registre à décalage 8 bits — et la raison pour laquelle une liaison série a un délai fixe et prévisible.

## Un écueil : décaler dans le mauvais sens

Les débutants inversent souvent le sens et perdent un bit. Si vous écrivez `r <= r(7 downto 1) & din;` vous avez gardé les sept bits du *haut* et jeté le bit 0 — c'est un décalage vers la *droite*, pas vers la gauche. Décidez par quelle extrémité les nouvelles données entrent et par laquelle elles tombent, puis faites correspondre la tranche. Les deux formes correctes sont :

```vhdl
r <= r(6 downto 0) & din;   -- décalage à GAUCHE : din -> LSB, MSB jeté
r <= din & r(7 downto 1);   -- décalage à DROITE : din -> MSB, LSB jeté
```

## À vous de jouer

- **Décalez vers la droite à la place.** Utilisez la seconde forme ci-dessus et confirmez que le bit mobile se déplace maintenant dans l'autre sens.
- **Chargement parallèle (PISO/universel).** Ajoutez une entrée `load` et un bus `din_par`. Quand `load = '1'` au front d'horloge, `r <= din_par` (prend tout le bus d'un coup) ; sinon, décaler. C'est le cœur d'un émetteur *parallèle-entrée, série-sortie*.
- **Sortie série.** Ajoutez `dout <= r(7);`. Maintenant c'est série-entrée, série-sortie (SISO) — le premier bit que vous avez envoyé sur `din` apparaît sur `dout` huit cycles plus tard, le délai sur lequel compte un RX d'UART.
- **Fabriquez un LFSR.** Renvoyez `r(7) xor r(5) xor r(4) xor r(3)` dans l'entrée à la place de `din`. Vous avez maintenant un générateur de séquence pseudo-aléatoire — la même structure utilisée pour les CRC et les motifs de test.

## À retenir

- Un registre à décalage est une chaîne de bascules D ; chaque front déplace chaque bit d'une place et admet un nouveau bit à une extrémité.
- `r <= r(6 downto 0) & din;` décale à gauche (nouveau bit dans le LSB) ; inversez la tranche et l'ordre pour décaler à droite.
- Ajouter un chargement parallèle le transforme en convertisseur SIPO/PISO — le cœur des liaisons série comme l'UART et le SPI.
- Renvoyez des prises (taps) sélectionnées à travers un XOR et il devient un LFSR pour les CRC et les séquences pseudo-aléatoires.
