# Projet : émetteur UART

Un UART (Universal Asynchronous Receiver / Transmitter) est le premier vrai protocole que la plupart des concepteurs embarqués construisent. Point à point, full duplex, il ne nécessite que trois fils (TX, RX, GND). Chaque adaptateur USB-série FTDI, chaque `Serial.print` Arduino, chaque vieux terminal est un UART au fond.

Ce projet construit le côté **émission**. Le côté réception est un excellent projet suivant une fois celui-ci terminé.

## Asynchrone : il n'y a pas de fil d'horloge

Le « A » d'UART est tout le défi. Contrairement à SPI ou I2C, les deux bouts ne partagent **aucune ligne d'horloge**. Le récepteur doit récupérer le timing à partir des données elles-mêmes. Ça ne marche que si les deux bouts se mettent d'accord à l'avance sur le **débit en bauds** (bits par seconde) et le **format de trame**. Trompez-vous sur l'un ou l'autre et vous recevez du charabia — le symptôme classique d'un désaccord de bauds.

Dans le cours ESCOM, ça se range sous le standard RS-232. Deux choses qu'on confond souvent et qu'il vaut la peine de séparer : l'**UART** est la logique numérique qui sérialise les octets (ce que vous construisez ici, à des niveaux logiques 0 V / 3,3 V), tandis que **RS-232** est le standard électrique qui décale ces niveaux à environ ±12 V avec une polarité *inversée* pour piloter un câble. Un driver de ligne façon MAX232 se trouve entre votre broche UART et un vrai connecteur DB-9. Votre VHDL ne traite jamais que du côté propre, à niveaux logiques.

## Format de trame (sans parité, 8 bits de données, 1 stop)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    la ligne est normalement haute ('1') ; le bit de start est un front descendant.
```

C'est le « 8N1 » : **8** bits de données, **N**o parity (pas de parité), **1** bit de stop — 10 bits sur le fil par octet. Les règles qui comptent :

- **Idle** est haut. Une ligne coupée ou débranchée se lit comme idle, pas comme un flot de zéros.
- Le bit de **start** est bas. Son front descendant est la seule référence de timing que le récepteur obtienne.
- Les **données** sortent **LSB en premier** — le bit 0 avant le bit 7.
- Le bit de **stop** est haut, ce qui garantit aussi qu'un front descendant frais est disponible pour la trame suivante.

Chaque bit est maintenu sur `tx` pendant exactement `clk_freq / baud` cycles d'horloge. Pour une horloge à 50 MHz et 115 200 bauds, ça fait 50 000 000 / 115 200 ≈ 434 cycles par bit.

## Votre boulot

1. Construisez un compteur d'horloge qui produit un `baud_tick` d'un cycle toutes les ~434 cycles.
2. Sur `send = '1'`, chargez `data` dans un registre à décalage 10 bits : `{ stop_bit, data[7..0], start_bit } = 1 & data & 0`. Pilotez `busy <= '1'`.
3. À chaque `baud_tick`, décalez le registre à droite (le start sort en premier, puis LSB→MSB des données, puis le stop).
4. Après le 10e tick, baissez `busy` et remettez `tx` au idle haut.

## Squelette

L'entity et l'état idle sont fournis ; `tx` est haut par défaut, `busy` est bas par défaut. Tout ce qu'il y a entre ces défauts est votre responsabilité.

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## Structure suggérée

Deux compteurs + un registre à décalage :

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- Quand `send = '1'` et `sending = '0'` :
  - chargez `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- Tant que `sending = '1'` :
  - `clk_count <= clk_count + 1;`
  - si `clk_count = BAUD_PERIOD - 1` :
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — décale à droite, complète en haut par idle
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - si `bit_count = 10` :
    - `sending <= '0';`

Faites de `BAUD_PERIOD` un generic pour pouvoir le réduire en simulation (par exemple 5 cycles/bit) tout en gardant la même logique fonctionnelle à pleine vitesse sur le matériel.

## Quelle erreur de timing vous pouvez vous permettre

Comme il n'y a pas d'horloge partagée, le récepteur échantillonne près du centre de chaque bit et dérive un peu au fil de la trame. Sur 10 périodes-bit, l'erreur accumulée doit rester sous un demi-bit, donc l'erreur de bauds de chaque bout doit rester sous environ ±2–3 %. Les diviseurs entiers tombent rarement pile : à 50 MHz, 434 cycles/bit donnent 115 207 bauds — une erreur de 0,006 %, complètement sûre. Une horloge peu commode peut mal s'arrondir, c'est pourquoi beaucoup de vrais UART ajoutent un diviseur fractionnaire. Le côté réception gagne de la marge en **suréchantillonnant** à 16× le débit en bauds : trouvez le front de start, puis échantillonnez au compte 8 (le centre du bit) et toutes les 16 ticks ensuite.

## Que tester

Pilotez `send = '1'` avec `data = "01001000"` (ASCII `'H'`). Après la fin de la trame, le chronogramme `tx` devrait montrer exactement 10 périodes-bit d'activité :

1. Start (bas)
2. Bit 0 des données (le LSB : `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Stop (haut)

Puis retour au idle haut. Zoomez dans l'onglet Formes d'onde et vérifiez le motif bit par bit.

## Pièges courants

- **Mauvais ordre des bits.** Envoyer le MSB en premier est le bug le plus courant à lui seul. Le `'1' & data & '0'` du squelette plus un décalage à droite donne le LSB-en-premier gratuitement — ne le « corrigez » pas.
- **Erreur d'un sur le compte de bits.** Le 8N1 fait *10* ticks, pas 8. Oublier le bit de start ou de stop tronque la trame.
- **Re-déclenchement en pleine trame.** N'honorez `send` que quand `sending = '0'`, sinon une seconde requête corrompra celle en cours. C'est à ça que sert `busy`.
- **Désaccord de bauds.** Si le récepteur montre le bon nombre d'octets mais des valeurs brouillées, soupçonnez le diviseur avant la logique.

## Pour aller plus loin

- Ajoutez une **FIFO** pour pouvoir mettre en file plusieurs octets ; `send` devient « push ».
- Construisez le côté **RX** : suréchantillonnez à 16×, trouvez le front de start, échantillonnez au centre du bit.
- Paramétrez la trame : parité, données 7 vs 8 bits, 1 vs 2 bits de stop.

## À retenir

- L'UART est **asynchrone** : pas de fil d'horloge, donc les deux bouts doivent s'accorder d'avance sur le débit en bauds et le format de trame.
- Le 8N1, c'est **10 bits** sur le fil — idle haut, start bas, 8 bits de données **LSB en premier**, stop haut.
- Un bit dure `clk_freq / baud` cycles ; un `baud_tick` d'un cycle cadence le registre à décalage.
- Gardez l'erreur de bauds de chaque bout sous ~2 % ; le côté RX s'achète de la marge en **suréchantillonnant et en échantillonnant au centre du bit**.
- **UART** est la logique ; **RS-232** est la couche électrique ±12 V qu'un driver de ligne ajoute par-dessus.
