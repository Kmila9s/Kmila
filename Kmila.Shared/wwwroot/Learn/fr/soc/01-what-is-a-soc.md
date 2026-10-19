# Qu'est-ce qu'un System-on-Chip ?

Votre CPU 8 bits du parcours précédent fonctionne en autonomie. Il calcule, mais ne parle pas au monde. Un **System-on-Chip** est l'échelle supérieure : un CPU plus une poignée de périphériques plus le tissu qui leur permet de se parler, le tout sur le même die.

C'est toute la prémisse du cours *Sistemas en Chip* — au lieu de câbler un processeur à des puces mémoire séparées et des puces d'E/S séparées sur une carte, vous intégrez le processeur, sa mémoire et ses périphériques dans un seul composant et vous programmez le résultat.

## Trois pièces obligatoires

Tout SoC, du Kmila CPU étendu jusqu'à un Apple A-series à 5 millions de dollars, se résume à :

1. **Un ou plusieurs éléments de calcul** — CPU, DSP, GPU, accélérateurs.
2. **Mémoire** — SRAM on-chip, block RAM, parfois DDR externe.
3. **Une interconnexion** — les fils + l'arbitrage qui permettent à (1) de parler à (2) et aux périphériques.

L'interconnexion est la partie qui cesse d'être « du simple RTL » et commence à sentir l'architecture. Elle impose qui peut parler quand, combien de transactions peuvent être en vol, et à quelle vitesse une requête donnée peut se compléter.

## L'élément de calcul : RISC et Harvard

La plupart des cœurs de SoC que vous croiserez — PIC, AVR, ARM Cortex-M, RISC-V — sont **RISC** (Reduced Instruction Set Computer). RISC veut dire un petit jeu d'instructions simples, à largeur fixe, qui font chacune une seule chose, pour que le matériel qui les décode et les exécute reste petit et rapide. Le compromis, c'est que les opérations complexes prennent plusieurs instructions au lieu d'une.

Ces cœurs sont généralement d'architecture **Harvard** : mémoire programme et mémoire de données sont sur des bus *séparés*. Comme les deux bus sont indépendants, le cœur peut récupérer l'instruction suivante en même temps qu'il lit ou écrit un opérande de données — deux fois la bande passante mémoire d'un unique bus partagé. Un joli effet de bord : le mot d'instruction n'a pas à faire la même largeur que le mot de données. Un cœur PIC de milieu de gamme, par exemple, fait tourner des données 8 bits mais des instructions 14 bits, donc chaque instruction tient dans un seul mot.

(Une machine **von Neumann** pure partage un seul bus pour code et données — plus simple, mais les fetchs de code et de données se le disputent. Le 8051 est un compromis « Harvard modifié ».)

## Pipelining

Un cœur recouvre le travail pour terminer plus d'instructions par seconde. La forme la plus simple est un **pipeline** à deux étages : pendant que l'instruction *N* s'exécute, le cœur récupère déjà l'instruction *N+1*.

```
  cycle :  1        2        3        4
  fetch:  I1       I2       I3       I4
  exec :           I1       I2       I3
```

Le hic, ce sont les **branchements**. Quand un `GOTO`, `CALL` ou `RETURN` change le program counter, l'instruction déjà récupérée derrière lui est fausse et doit être jetée — le pipeline *se vide* (flush) et se remplit à nouveau, coûtant un cycle. C'est pourquoi les boucles serrées et le code plein de sauts tournent un peu plus lentement que le code en ligne droite.

## Mémoire programme, mémoire de données et modèle de programmation

L'unité ESCOM divise la carte mémoire en deux rôles :

- **La mémoire programme** contient vos instructions (souvent flash/ROM). Non volatile, donc le composant démarre le même programme à chaque mise sous tension.
- **La mémoire de données** contient les variables, la pile et les registres d'E/S (SRAM). Volatile, effacée au reset.

Posé par-dessus, il y a le **modèle de programmation** : les registres et la carte d'adresses que le programmeur voit réellement. Il définit les registres généraux, les registres à fonction spéciale (drapeaux d'état, le program counter, le pointeur de pile) et — crucial pour un SoC — les **registres de périphériques mappés en mémoire**. Dans un design mappé en mémoire, un périphérique ressemble à quelques adresses : écrivez un octet à une adresse et l'UART transmet ; lisez une autre adresse et vous obtenez le résultat de l'ADC. Le CPU n'apprend jamais une nouvelle instruction pour chaque périphérique ; il ne fait que des loads et des stores.

## Protocoles d'interconnexion courants

L'industrie s'est calée sur une poignée de standards pour ce tissu de load/store :

| Protocole        | Fournisseur | Où le voir                                        |
|------------------|-------------|---------------------------------------------------|
| AXI4 / AXI4-Lite | ARM         | Cœurs RISC-V, frontière PS/PL Xilinx, la plupart des nouvelles IP |
| AHB / APB        | ARM         | Anciens cœurs ARM, périphériques lents            |
| Avalon MM / ST   | Intel       | NIOS-II, SoCs générés par Qsys                     |
| Wishbone         | OpenCores   | OpenRISC, beaucoup d'IP open-source               |

Tous se réduisent aux mêmes voies : un **canal d'adresse**, un **canal d'écriture de données** et un **canal de lecture de données**. Les variations portent sur le protocole de handshake, le support out-of-order et la sémantique des bursts.

## Ce qu'est vraiment AXI4-Lite

Le membre le plus simple de la famille AXI. Enlevez les bursts, l'out-of-order et la QoS, et vous obtenez cinq canaux indépendants, chacun avec un handshake `valid`/`ready` :

```
  AW  (adresse écriture)
  W   (données écriture)
  B   (réponse écriture)
  AR  (adresse lecture)
  R   (données lecture)
```

Chaque transfert est un beat. Chaque canal s'arrête quand l'autre côté n'est pas `ready`. C'est tout — ce qui est exactement pourquoi c'est la bonne taille à construire à la main.

## Le plan du cursus Kmila

Les leçons suivantes construisent une interface registre AXI4-Lite depuis zéro, puis la câblent entre votre CPU 8 bits et un UART mappé en mémoire pour que le CPU puisse « imprimer » en écrivant à une adresse. Ça vous donne le modèle mental de ce qui se passe à l'intérieur de tout SoC moderne, à une taille que vous pouvez vraiment tenir en tête.

## À retenir

- Un SoC = élément(s) de calcul + mémoire + une interconnexion, le tout sur un seul die.
- La plupart des cœurs de SoC sont **RISC** avec un découpage **Harvard**, donc code et données circulent sur des bus séparés en même temps.
- Un court **pipeline** recouvre fetch et execute ; **les branchements le vident**.
- La mémoire programme est du code non volatil ; la mémoire de données est de la RAM volatile ; le **modèle de programmation** est la carte des registres et des adresses que le programmeur voit.
- Les périphériques sont **mappés en mémoire** — le CPU les atteint avec de simples loads et stores sur une interconnexion standard comme AXI4-Lite.
