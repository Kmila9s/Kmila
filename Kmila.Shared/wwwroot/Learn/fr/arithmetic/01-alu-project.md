# Projet : ALU 8 bits avec drapeaux

Une **Unité Arithmétique et Logique** (ALU) est le cœur battant de tout CPU. Donne-lui deux opérandes et un opcode ; elle te rend un résultat et une poignée de drapeaux qui décrivent ce qui s'est passé (débordement ? résultat nul ? etc.).

## Ce que tu construis

Une ALU 8 bits avec les opérations suivantes :

| Opcode | Nom     | Formule                      |
|--------|---------|------------------------------|
| `000`  | ADD     | `y = a + b`                  |
| `001`  | SUB     | `y = a - b`                  |
| `010`  | AND     | `y = a and b`                |
| `011`  | OR      | `y = a or  b`                |
| `100`  | XOR     | `y = a xor b`                |
| `101`  | SHL     | `y = a décalé à gauche de 1` |
| `110`  | SHR     | `y = a décalé à droite de 1` |
| `111`  | PASS_A  | `y = a` (pass-through)       |

Plus deux drapeaux de sortie :

- `zero` — haut quand `y` est entièrement à zéro
- `carry` — la retenue sortante d'ADD/SUB ; pilote `'0'` pour tout le reste

## Ce que le squelette te donne

Le starter déclare l'entity, les signaux `result` et `cout`, et un squelette `process(op, a, b)` qui gère déjà ADD et SUB. Ton boulot : remplir les six autres branches du `case`.

## Indices

- Pour **AND / OR / XOR**, c'est une ligne chacune — affecte juste le résultat bit-à-bit :

  ```vhdl
  when "010" => result <= a and b;
  ```

- Pour **SHL** et **SHR**, complète avec `'0'` :

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Pour **PASS_A**, copie simplement `a` dans `result`.

Pense à piloter `cout <= '0'` pour les ops logiques — tu le fais une fois en haut du process, donc toute branche qui ne le surcharge pas hérite du défaut. Pratique, mais ça veut dire qu'il faut être explicite quand une branche **veut** une retenue non nulle.

## Critères d'acceptation

Avec les stimuli de la config compagnon :

| temps | op    | a          | b          | `y` attendu  | `carry` | `zero` |
|-------|-------|------------|------------|--------------|---------|--------|
|   0   | `000` | `00001111` | `00000101` | `00010100`   | `0`     | `0`    |
|  20   | `001` | `00001111` | `00001111` | `00000000`   | `0`     | `1`    |
|  40   | `010` | `00001111` | `00001111` | `00001111`   | `0`     | `0`    |
|  60   | `101` | `10110000` | n/a        | `01100000`   | `0`     | `0`    |
|  80   | `111` | `10110000` | n/a        | `10110000`   | `0`     | `0`    |

## Pour aller plus loin

- Ajoute un drapeau **signed** (résultat négatif pour SUB en complément à deux).
- Ajoute un opcode **rotate** — comme SHL mais le bit de poids fort revient au bit 0.
- Ajoute un opcode **multiply** produisant un résultat 16 bits sur deux ports de sortie séparés. La plupart des petits FPGAs ont des blocs DSP dédiés pour ça — surveille l'utilisation lors d'une nouvelle synthèse.

Cette ALU est une brique directe pour le projet CPU au niveau Expert — garde le design propre, tu vas le brancher là-bas.
