# Comment un CPU exécute une instruction

Regarde dans n'importe quel CPU — d'un microcontrôleur 8 bits minuscule à un cœur de shader GPU moderne — et tu trouveras les mêmes cinq étages, faisant les mêmes cinq boulots :

```mermaid
flowchart LR
    IF["IF<br/>fetch"] --> ID["ID<br/>decode"] --> EX["EX<br/>execute"] --> MEM["MEM<br/>memory"] --> WB["WB<br/>writeback"]
```

## Les cinq étages classiques

**IF (Instruction Fetch).** Lis l'instruction suivante depuis la mémoire programme à l'adresse du Program Counter (PC). Incrémente le PC pour qu'au prochain cycle on pointe sur l'instruction d'après.

**ID (Decode).** Décompose le mot d'instruction. Sépare-le en un **opcode** (quelle opération ?) et des **champs d'opérandes** (quels registres ? quelle valeur immédiate ?).

**EX (Execute).** Envoie les opérandes dans l'ALU. Pour `ADD R0, R1`, EX lit R0 et R1 du register file, dit à l'ALU « add », et latche le résultat. Pour `JUMP addr`, EX calcule le nouveau PC.

**MEM (Memory).** Si l'instruction est un load ou un store, c'est ici que tu parles à la mémoire de données. Pour les instructions registre-à-registre, l'étage est un pass-through — mais il est toujours là pour que toutes les instructions aient la même profondeur de pipeline.

**WB (Writeback).** Le résultat atterrit enfin à sa destination — un registre pour l'arithmétique, le PC pour un saut, une cellule mémoire pour un store.

## Monocycle vs multicycle vs pipeliné

Les mêmes cinq tâches peuvent être arrangées de trois façons dans le silicium :

**Monocycle.** Les cinq étages s'exécutent en combinatoire dans une seule période d'horloge. Pour : simple. Contre : l'horloge doit être assez lente pour que l'instruction la plus longue (souvent un load) propage à travers *tout* en un tick. Tous les exemples MIPS de cours sont monocycle.

**Multicycle.** Un étage par cycle. L'instruction attend dans un registre d'état pendant qu'elle traverse les étages. Pour : horloge plus rapide qu'en monocycle (chaque étage a son propre chemin critique court). Contre : cinq cycles par instruction.

**Pipeliné.** Cinq instructions en vol simultanément, chacune dans un étage différent. Pour : une nouvelle instruction sort à chaque cycle, donc débit ≈ monocycle avec une horloge bien plus rapide. Contre : aléas (données, contrôle, structurels) auxquels le reste d'un cours d'architecture est consacré. Cœurs RISC-V, ARM Cortex-M, AMD Zen — tous pipelinés.

## Le projet CPU de ce cursus

Ton premier CPU sera **monocycle** pour rester maniable. Tout se passe en combinatoire dans une période d'horloge :

- PC adresse une ROM de 16 octets.
- L'octet récupéré est à la fois le nibble d'opcode et le nibble d'immédiat.
- Decode + execute + writeback se passent avant le prochain front montant, qui capture le nouveau PC et le nouveau R0.

## L'ISA qu'on implémente

Quatre instructions, opcode 4 bits + immédiat 4 bits :

| Opcode | Mnémonique    | Sémantique                   |
|--------|---------------|------------------------------|
| `0000` | `LOAD R0, #i` | `R0 <= zero_extend(i)`       |
| `0001` | `ADD  R0, #i` | `R0 <= R0 + zero_extend(i)`  |
| `0010` | `JUMP addr`   | `PC <= addr`                 |
| `1111` | `HALT`        | gèle PC et R0                |

Un programme que tu peux déjà faire tourner à la main :

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## Ce que ça te donne

Une fois le squelette qui marche, tu as une **machine programmable**. Change les octets dans la ROM — change le programme que le CPU exécute. C'est l'astuce fondamentale : même matériel, contenu mémoire différent, comportement différent.

À partir d'ici tu peux grossir :

- Ajouter plus de registres (R0..R3).
- Ajouter des sauts conditionnels (`JZ`, `JNZ`) pour que tes programmes branchent.
- Ajouter loads/stores depuis un module RAM (réutilise celui du module Mémoires).
- Pipeliner le tout.

Le chemin de cette machine 4-instructions à un cœur RV32I RISC-V complet est long, mais chaque étape n'est que plus du même motif que tu t'apprêtes à construire.
