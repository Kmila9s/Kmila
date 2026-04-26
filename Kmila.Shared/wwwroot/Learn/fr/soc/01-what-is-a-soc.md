# Qu'est-ce qu'un System-on-Chip ?

Ton CPU 8 bits du parcours précédent tourne en autonome. Il calcule, mais il ne parle pas au monde. Un **System-on-Chip** est la taille au-dessus : un CPU plus une poignée de périphériques plus le tissu qui leur permet de se parler, le tout sur la même puce.

## Trois pièces obligatoires

Tout SoC, du Kmila CPU étendu à un Apple série A à 5 M$, se résume à :

1. **Un ou plusieurs éléments de traitement** — CPU, DSP, GPU, accélérateurs.
2. **De la mémoire** — SRAM on-chip, block RAM, parfois DDR off-chip.
3. **Une interconnexion** — les fils + arbitrage qui permettent à (1) de parler à (2) et aux périphériques.

L'interconnexion est la partie qui cesse d'être « juste du RTL » et commence à sentir l'architecture. Elle impose qui peut parler quand, combien de transactions peuvent être en vol, et à quelle vitesse une requête donnée peut se terminer.

## Protocoles d'interconnexion courants

L'industrie s'est stabilisée sur une poignée :

| Protocole        | Fournisseur | Où tu le verras                                       |
|------------------|-------------|-------------------------------------------------------|
| AXI4 / AXI4-Lite | ARM         | Cœurs RISC-V, frontière PS/PL Xilinx, la plupart des IP récentes |
| AHB / APB        | ARM         | Cœurs ARM legacy, périphériques basse vitesse         |
| Avalon MM / ST   | Intel       | NIOS-II, SoC générés par Qsys                         |
| Wishbone         | OpenCores   | OpenRISC, beaucoup d'IP cores open source             |

Tous se résument aux mêmes trois voies : un **canal d'adresse**, un **canal de donnée d'écriture**, et un **canal de donnée de lecture**. Les variations portent sur le protocole de handshake, le support du désordre, et la sémantique de rafales.

## Ce qu'est réellement AXI4-Lite

Le membre le plus simple de la famille AXI. Retire tout ce qui concerne les rafales, le désordre et la QoS, et tu obtiens cinq canaux indépendants, chacun avec un handshake `valid`/`ready` :

```
  AW  (write address)
  W   (write data)
  B   (write response)
  AR  (read address)
  R   (read data)
```

Chaque transfert est un seul beat. Chaque canal se bloque quand l'autre côté n'est pas `ready`. C'est tout.

## Le plan du programme Kmila

Les leçons ultérieures de ce module construisent une interface de registres AXI4-Lite à partir de zéro, puis la câblent entre ton CPU 8 bits et un UART mappé en mémoire pour que le CPU puisse « imprimer » en écrivant à une adresse. Ça te donne le modèle mental de ce qui se passe à l'intérieur de tout SoC moderne, à une taille que tu peux réellement tenir dans ta tête.

*D'autres leçons à venir — ce module est en cours de rédaction active.*
