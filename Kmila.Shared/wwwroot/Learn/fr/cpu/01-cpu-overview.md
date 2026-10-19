# Comment un CPU exécute une instruction

Regardez dans n'importe quel CPU — d'un microcontrôleur 8 bits minuscule à un cœur de shader GPU moderne — et vous trouverez les mêmes cinq étages, faisant les mêmes cinq boulots :

```mermaid
flowchart LR
    IF["IF<br/>fetch"] --> ID["ID<br/>decode"] --> EX["EX<br/>execute"] --> MEM["MEM<br/>memory"] --> WB["WB<br/>writeback"]
```

## Les cinq étages classiques

**IF (Instruction Fetch).** Lisez l'instruction suivante depuis la mémoire programme à l'adresse du Program Counter (PC). Incrémentez le PC pour qu'au prochain cycle on pointe sur l'instruction d'après.

**ID (Decode).** Décomposez le mot d'instruction. Séparez-le en un **opcode** (quelle opération ?) et des **champs d'opérandes** (quels registres ? quelle valeur immédiate ?).

**EX (Execute).** Envoyez les opérandes dans l'ALU. Pour `ADD R0, R1`, EX lit R0 et R1 du register file, dit à l'ALU « add », et latche le résultat. Pour `JUMP addr`, EX calcule le nouveau PC.

**MEM (Memory).** Si l'instruction est un load ou un store, c'est ici que vous parlez à la mémoire de données. Pour les instructions registre-à-registre, l'étage est un pass-through — mais il est toujours là pour que toutes les instructions aient la même profondeur de pipeline.

**WB (Writeback).** Le résultat atterrit enfin à sa destination — un registre pour l'arithmétique, le PC pour un saut, une cellule mémoire pour un store.

## Monocycle vs multicycle vs pipeliné

Les mêmes cinq tâches peuvent être arrangées de trois façons dans le silicium :

**Monocycle.** Les cinq étages s'exécutent en combinatoire dans une seule période d'horloge. Pour : simple. Contre : l'horloge doit être assez lente pour que l'instruction la plus longue (souvent un load) propage à travers *tout* en un tick. Tous les exemples MIPS de cours sont monocycle.

**Multicycle.** Un étage par cycle. L'instruction attend dans un registre d'état pendant qu'elle traverse les étages. Pour : horloge plus rapide qu'en monocycle (chaque étage a son propre chemin critique court). Contre : cinq cycles par instruction.

**Pipeliné.** Cinq instructions en vol simultanément, chacune dans un étage différent. Pour : une nouvelle instruction sort à chaque cycle, donc débit ≈ monocycle avec une horloge bien plus rapide. Contre : aléas (données, contrôle, structurels) auxquels le reste d'un cours d'architecture est consacré. Cœurs RISC-V, ARM Cortex-M, AMD Zen — tous pipelinés.

## RISC, CISC, et pourquoi les étages sont si nets

Ces cinq étages bien propres ne sont pas un hasard — c'est ce qu'on obtient quand le jeu d'instructions est conçu pour être régulier. C'est toute la philosophie **RISC** (Reduced Instruction Set Computer) autour de laquelle le cursus ESCOM est construit :

- **Instructions à largeur fixe** (chaque instruction fait le même nombre de bits), donc fetch et decode deviennent triviaux.
- **Architecture load/store** — seules des instructions dédiées de load et de store touchent la mémoire ; l'arithmétique travaille de registre à registre. C'est pour ça que MEM est un étage séparé bien net.
- **Beaucoup de registres généraux**, pour que les compilateurs gardent les valeurs dans le register file au lieu de les déverser en mémoire.

Le **CISC** (Complex Instruction Set Computer, par exemple x86) fait le pari inverse : instructions à longueur variable, opérations qui lisent la mémoire *et* calculent en une seule instruction, et des centaines de modes d'adressage. Puissant par instruction, mais bien plus dur à pipeliner proprement. Les puces x86 modernes décomposent d'ailleurs en interne chaque instruction complexe en micro-opérations façon RISC.

Une dernière distinction à nommer : les machines **Von Neumann** gardent programme et données dans une seule mémoire (un bus, simple, mais le code et les données se la disputent) ; les machines **Harvard** utilisent des mémoires d'instructions et de données séparées, ce qui est exactement ce qui permet à un cœur monocycle ou pipeliné de récupérer une instruction *et* d'accéder aux données dans le même tick. Le CPU que vous allez construire est de fait une Harvard — une ROM pour le code, un chemin séparé pour les données.

## Formats d'instruction

Le decode n'est facile que parce que les champs de chaque instruction sont à des positions fixes. Le RISC classique (façon MIPS, le modèle qu'utilise l'ESCOM) a trois formats :

- **Type R** — opérations ALU registre-à-registre : `opcode | rs | rt | rd | shamt | funct`. Deux registres sources, un de destination, une quantité de décalage, et un sélecteur de fonction.
- **Type I** — immédiat / load / store / branchement : `opcode | rs | rt | immediate`. L'immédiat de 16 bits est une constante ou un offset d'adresse.
- **Type J** — sauts : `opcode | address`. Un grand champ d'adresse pour atteindre des cibles lointaines.

Le petit ISA jouet ci-dessous est un cousin à deux champs de cette idée : un opcode de 4 bits et un immédiat de 4 bits. Même principe, en plus petit.

## Le projet CPU de ce cursus

Votre premier CPU sera **monocycle** pour rester maniable. Tout se passe en combinatoire dans une période d'horloge :

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

Un programme que vous pouvez déjà faire tourner à la main :

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## Ce que ça vous donne

Une fois le squelette qui marche, vous avez une **machine programmable**. Changez les octets dans la ROM — changez le programme que le CPU exécute. C'est l'astuce fondamentale : même matériel, contenu mémoire différent, comportement différent.

À partir d'ici vous pouvez grossir :

- Ajouter plus de registres (R0..R3).
- Ajouter des sauts conditionnels (`JZ`, `JNZ`) pour que vos programmes branchent.
- Ajouter loads/stores depuis un module RAM (réutilise celui du module Mémoires).
- Pipeliner le tout.

Le chemin de cette machine 4-instructions à un cœur RV32I RISC-V complet est long, mais chaque étape n'est que plus du même motif que vous vous apprêtez à construire.

## Qui pilote tout ça : l'unité de contrôle

Le chemin de données (PC, ROM, register file, ALU, mémoire) est la tuyauterie ; l'**unité de contrôle** est ce qui ouvre les bonnes vannes à chaque cycle. Elle lit l'opcode et pilote les signaux de contrôle : quelle opération ALU, si le register file écrit ce cycle-ci, si la mémoire lit ou écrit, d'où vient le prochain PC. Deux façons de la construire :

- **Contrôle câblé** — l'opcode alimente de la logique combinatoire (ou une petite machine à états) qui produit directement les signaux. Rapide, et le choix naturel pour un RISC propre. C'est ce que fait votre squelette avec son `case` sur l'opcode.
- **Contrôle microprogrammé** — chaque instruction est un petit programme de *microinstructions* stocké dans une ROM de microcode, qui déroule les signaux de contrôle. Flexible, et historiquement la façon dont les machines CISC ont dompté leur complexité, au prix de la vitesse. Le programme ESCOM nomme les deux (microinstrucciones, memorias de microcódigo) pour que vous sachiez les distinguer.

À côté se trouve le **registre d'état** — les flags produits par votre ALU (zéro, retenue, débordement, négatif). Les branchements conditionnels ne sont que l'unité de contrôle qui teste un de ces flags pour décider du prochain PC.

## À retenir

- Tout CPU, grand ou petit, fait tourner la même boucle : fetch, decode, execute, memory, writeback.
- Ces étages peuvent être monocycle (simple, horloge lente), multicycle (horloge plus rapide, plusieurs cycles chacun), ou pipelinés (recouverts, débit maximal).
- Le RISC garde des instructions à largeur fixe et load/store pour que decode et pipeline restent propres ; le CISC troque ça contre de la puissance par instruction.
- Le chemin de données déplace les données ; l'unité de contrôle — câblée ou microprogrammée — décide de ce qui va où, à partir de l'opcode et des flags d'état.
