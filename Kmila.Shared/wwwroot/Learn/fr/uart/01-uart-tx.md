# Projet : émetteur UART

Un UART (Universal Asynchronous Receiver / Transmitter) est le premier vrai protocole que la plupart des concepteurs embarqués construisent. Il est point à point, full duplex, et n'a besoin que de trois fils (TX, RX, GND). Tout adaptateur USB-série FTDI, tout `Serial.print` Arduino, tout terminal legacy utilise un UART en dessous.

Ce projet construit le côté **émission**. Le côté réception est un excellent projet suivant une fois celui-ci terminé.

## Format de trame (sans parité, 8 bits de données, 1 stop)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    la ligne est normalement haute ('1') ; le bit de start est un front descendant.
```

Chaque bit est maintenu sur `tx` pendant exactement `clk_freq / baud` cycles d'horloge. Pour une horloge à 50 MHz et 115 200 baud, c'est ≈ 434 cycles par bit.

## Ton travail

1. Construis un compteur d'horloge qui produit un `baud_tick` d'un cycle tous les ~434 cycles.
2. Sur `send = '1'`, latch `data` dans un registre à décalage 10 bits : `{ stop_bit, data[7..0], start_bit } = 1 & data & 0`. Force `busy <= '1'`.
3. À chaque `baud_tick`, décale le registre à droite (pour que le start bit sorte en premier, puis LSB jusqu'au MSB de data, puis le stop bit).
4. Après le 10ᵉ tick, baisse `busy` et idle `tx` en haut.

## Squelette

L'entité et l'état de repos sont fournis ; `tx` par défaut haut, `busy` par défaut bas. Tout entre ces valeurs par défaut est ta responsabilité.

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
  - charge `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- Tant que `sending = '1'` :
  - `clk_count <= clk_count + 1;`
  - si `clk_count = BAUD_PERIOD - 1` :
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — décale à droite, complète le haut avec idle
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - si `bit_count = 10` :
    - `sending <= '0';`

Fais de `BAUD_PERIOD` un generic pour pouvoir le raccourcir en simulation (par ex. 5 cycles/bit) tout en gardant la même logique à pleine vitesse sur du matériel.

## Ce qu'il faut tester

Pilote `send = '1'` avec `data = "01001000"` (ASCII `'H'`). Une fois la trame terminée, la forme d'onde de `tx` doit montrer exactement 10 périodes de bit d'activité :

1. Start (bas)
2. Bit 0 des données (qui est le LSB : `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Stop (haut)

Puis retour en idle haut. Zoome dans l'onglet Waveforms et vérifie le motif bit par bit.

## Pour aller plus loin

- Ajoute une **FIFO** pour pouvoir mettre plusieurs octets en file d'attente ; `send` devient « push ».
- Construis le côté **RX** : échantillonne chaque bit entrant à 1,5 × période de bit, puis à 1 × ensuite.
- Paramétrise la trame : parité, 7 bits vs 8 bits de données, 1 vs 2 bits de stop.
