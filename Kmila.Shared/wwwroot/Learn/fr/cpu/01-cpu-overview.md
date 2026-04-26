# Comment un CPU exécute une instruction

Regarde à l'intérieur de n'importe quel CPU — du minuscule microcontrôleur 8 bits au cœur de shader GPU moderne — et tu trouveras les mêmes cinq étages, faisant les mêmes cinq boulots :

```
  ┌─────┐   ┌──────┐   ┌────────┐   ┌─────────┐   ┌──────────┐
  │ IF  │──▶│ ID   │──▶│ EX     │──▶│ MEM     │──▶│ WB       │
  │fetch│   │decode│   │execute │   │ memory  │   │writeback │
  └─────┘   └──────┘   └────────┘   └─────────┘   └──────────┘
```

## Les cinq étages classiques

**IF (Instruction Fetch).** Lit l'instruction suivante depuis la mémoire programme, à l'adresse contenue dans le Program Counter (PC). Incrémente PC pour qu'au cycle suivant on pointe sur l'instruction d'après.

**ID (Decode).** Décortique le mot d'instruction. Le sépare en un **opcode** (quelle opération ?) et des **champs d'opérandes** (quels registres ? quelle valeur immédiate ?).

**EX (Execute).** Fournit les opérandes à l'ALU. Pour une instruction `ADD R0, R1`, EX lit R0 et R1 dans le banc de registres, dit à l'ALU « additionne », et latche le résultat. Pour un `JUMP addr`, EX calcule le nouveau PC.

**MEM (Memory).** Si l'instruction est un load ou un store, c'est ici qu'on parle à la mémoire de données. Pour les instructions registre-à-registre, l'étage est un pass-through — mais l'étage existe toujours, pour que toute instruction ait la même profondeur de pipeline.

**WB (Writeback).** Le résultat atterrit enfin dans sa destination — un registre pour l'arithmétique, le PC pour un saut, une cellule mémoire pour un store.

## Single-cycle vs multi-cycle vs pipeliné

Les mêmes cinq boulots peuvent être arrangés de trois manières différentes en silicium :

**Single-cycle.** Les cinq étages s'exécutent de manière combinatoire en une seule période d'horloge. Pour : simple. Contre : l'horloge doit être assez lente pour que l'instruction la plus longue (généralement un load) se propage à travers *tout* en un seul tick. Tous les exemples pédagogiques MIPS sont en single-cycle.

**Multi-cycle.** Un étage par cycle d'horloge. L'instruction reste dans un registre d'état pendant qu'elle traverse les étages. Pour : horloge plus rapide qu'en single-cycle (chaque étage a son propre chemin critique court). Contre : cinq cycles par instruction.

**Pipeliné.** Cinq instructions en vol en même temps, chacune dans un étage différent. Pour : une nouvelle instruction sort à chaque cycle, donc le débit ≈ single-cycle à une horloge bien plus rapide. Contre : les aléas (données, contrôle, structurels) auxquels le reste d'un cours d'architecture des ordinateurs est consacré. Les cœurs RISC-V, ARM Cortex-M, AMD Zen — tous pipelinés.

## Le projet CPU dans ce programme

Ton premier CPU sera **single-cycle** pour rester abordable. Tout se passe de manière combinatoire pendant une période d'horloge :

- PC adresse une ROM de 16 octets.
- L'octet lu est à la fois le nibble d'opcode et le nibble d'immédiat.
- Décodage + exécution + writeback ont tous lieu avant le front montant d'horloge suivant, qui capture le nouveau PC et le nouveau R0.

## L'ISA qu'on va implémenter

Quatre instructions, opcode 4 bits + immédiat 4 bits :

| Opcode | Mnémonique    | Sémantique                 |
|--------|---------------|----------------------------|
| `0000` | `LOAD R0, #i` | `R0 <= zero_extend(i)`     |
| `0001` | `ADD  R0, #i` | `R0 <= R0 + zero_extend(i)`|
| `0010` | `JUMP addr`   | `PC <= addr`               |
| `1111` | `HALT`        | fige PC et R0              |

Un programme que tu peux déjà exécuter à la main :

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## Ce que ça te donne

Une fois le squelette fonctionnel, tu as une **machine programmable**. Change les octets dans la ROM — change le programme que le CPU exécute. C'est l'astuce fondamentale : le même matériel, des contenus différents en mémoire, un comportement différent.

À partir de là, tu peux grandir :

- Ajouter plus de registres (R0..R3).
- Ajouter des sauts conditionnels (`JZ`, `JNZ`) pour que tes programmes puissent brancher.
- Ajouter des loads/stores depuis un module RAM (réutilise celui du module Mémoires).
- Pipeliner tout ça.

Le chemin de cette machine à 4 instructions jusqu'à un cœur RISC-V RV32I complet est long, mais chaque étape n'est que le même pattern que tu t'apprêtes à construire.
