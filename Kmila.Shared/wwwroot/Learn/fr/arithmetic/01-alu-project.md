# Projet : ALU 8 bits avec flags

Une **ALU (Arithmetic Logic Unit)** est le cœur battant de tout CPU. Tu lui donnes deux opérandes et un opcode ; elle te rend un résultat et une poignée de flags qui décrivent ce qui s'est passé (y a-t-il eu un dépassement ? le résultat est-il nul ? etc.).

## Ce que tu vas construire

Une ALU 8 bits avec les opérations suivantes :

| Opcode | Nom     | Formule                       |
|--------|---------|-------------------------------|
| `000`  | ADD     | `y = a + b`                   |
| `001`  | SUB     | `y = a - b`                   |
| `010`  | AND     | `y = a and b`                 |
| `011`  | OR      | `y = a or  b`                 |
| `100`  | XOR     | `y = a xor b`                 |
| `101`  | SHL     | `y = a décalé à gauche de 1`  |
| `110`  | SHR     | `y = a décalé à droite de 1`  |
| `111`  | PASS_A  | `y = a` (pass-through)        |

Plus deux sorties de flag :

- `zero` — haut quand `y` ne contient que des zéros
- `carry` — la retenue sortante de ADD/SUB ; mets `'0'` pour tout le reste

## Ce que te donne le squelette

Le code de départ déclare l'entité, les signaux `result` et `cout`, et une coquille `process(op, a, b)` qui gère déjà ADD et SUB. Ton travail est de remplir les six autres branches de la clause `case`.

## Indices

- Pour **AND / OR / XOR**, c'est une ligne chacun — il suffit d'assigner le résultat bit à bit :

  ```vhdl
  when "010" => result <= a and b;
  ```

- Pour **SHL** et **SHR**, complète avec `'0'` :

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Pour **PASS_A**, copie simplement `a` dans `result`.

N'oublie pas de forcer `cout <= '0'` pour les opérations logiques — tu le fais déjà une fois en haut du process, ce qui signifie que toute branche qui ne le redéfinit pas hérite de cette valeur par défaut. C'est bien, mais ça implique d'être explicite quand une branche **veut** une retenue non nulle.

## Critères d'acceptation

Avec les stimuli de la configuration associée :

| temps | op    | a          | b          | `y` attendu  | `carry` | `zero` |
|-------|-------|------------|------------|--------------|---------|--------|
|  0    | `000` | `00001111` | `00000101` | `00010100`   | `0`     | `0`    |
| 20    | `001` | `00001111` | `00001111` | `00000000`   | `0`     | `1`    |
| 40    | `010` | `00001111` | `00001111` | `00001111`   | `0`     | `0`    |
| 60    | `101` | `10110000` | n/a        | `01100000`   | `0`     | `0`    |
| 80    | `111` | `10110000` | n/a        | `10110000`   | `0`     | `0`    |

## Pour aller plus loin

- Ajoute un flag **signed** (résultat négatif en SUB avec complément à deux).
- Ajoute un opcode **rotate** — identique à SHL, mais le bit de poids fort revient en bit 0.
- Ajoute un opcode **multiply** produisant un résultat 16 bits sur deux ports de sortie distincts. La plupart des petits FPGA ont des blocs DSP dédiés pour ça — surveille ton utilisation quand tu resynthétises.

Cette ALU est une brique directe pour le projet de CPU au niveau Expert — garde ce design propre, tu le câbleras là-bas.
