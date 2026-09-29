# Qu'est-ce qu'un System-on-Chip ?

Votre CPU 8 bits du parcours précédent fonctionne en autonomie. Il calcule, mais ne parle pas au monde. Un **System-on-Chip** est l'échelle supérieure : un CPU plus une poignée de périphériques plus le tissu qui leur permet de se parler, le tout sur le même die.

## Trois pièces obligatoires

Tout SoC, du Kmila CPU étendu jusqu'à un Apple A-series à 5 millions de dollars, se résume à :

1. **Un ou plusieurs éléments de calcul** — CPU, DSP, GPU, accélérateurs.
2. **Mémoire** — SRAM on-chip, block RAM, parfois DDR externe.
3. **Une interconnexion** — les fils + l'arbitrage qui permettent à (1) de parler à (2) et aux périphériques.

L'interconnexion est la partie qui cesse d'être « du simple RTL » et commence à sentir l'architecture. Elle décide qui peut parler quand, combien de transactions peuvent être en vol, et à quelle vitesse une requête donnée peut se compléter.

## Protocoles d'interconnexion courants

L'industrie s'est calée sur une poignée :

| Protocole       | Fournisseur | Où le voir                                              |
|-----------------|-------------|---------------------------------------------------------|
| AXI4 / AXI4-Lite| ARM         | Cœurs RISC-V, frontière PS/PL Xilinx, la plupart des IP |
| AHB / APB       | ARM         | Anciens cœurs ARM, périphériques lents                  |
| Avalon MM / ST  | Intel       | NIOS-II, SoCs générés par Qsys                          |
| Wishbone        | OpenCores   | OpenRISC, beaucoup d'IP open-source                     |

Tous se réduisent aux mêmes trois voies : un **canal d'adresse**, un **canal d'écriture de données** et un **canal de lecture de données**. Les variations portent sur le protocole de handshake, le support out-of-order et la sémantique des bursts.

## Ce qu'est vraiment AXI4-Lite

Le membre le plus simple de la famille AXI. Enlevez tout ce qui touche aux bursts, out-of-order et QoS, et vous obtenez cinq canaux indépendants, chacun avec un handshake `valid`/`ready` :

```
  AW  (adresse écriture)
  W   (données écriture)
  B   (réponse écriture)
  AR  (adresse lecture)
  R   (données lecture)
```

Chaque transfert est un beat. Chaque canal s'arrête quand l'autre n'est pas `ready`. C'est tout.

## Le plan du cursus Kmila

Les leçons suivantes de ce module construisent une interface registre AXI4-Lite depuis zéro, puis la branchent entre votre CPU 8 bits et un UART memory-mapped pour que le CPU puisse « imprimer » en écrivant à une adresse. Ça vous donne le modèle mental de ce qui se passe dans tout SoC moderne, à une taille que vous pouvez vraiment tenir en tête.

*D'autres leçons à venir — ce module est en cours d'écriture.*
