# Projet : ALU 8 bits avec drapeaux

Une **Unité Arithmétique et Logique** (ALU) est le cœur battant de tout CPU. Donnez-lui deux opérandes et un opcode ; elle vous rend un résultat et une poignée de drapeaux qui décrivent ce qui s'est passé (y a-t-il eu débordement ? le résultat est-il nul ? etc.).

## Ce que vous construisez

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
- `carry` — la retenue sortante d'ADD/SUB ; pilotez `'0'` pour tout le reste

Dans le cours ESCOM *Arquitectura de Computadoras*, c'est le thème 3.1, le premier bloc fonctionnel du processeur RISC monocycle. Construisez-le proprement maintenant et vous le laisserez tomber directement dans le chemin de données plus tard.

## L'additionneur en dessous

ADD et SUB ressemblent à une ligne de VHDL (`a + b`), mais le matériel que le synthétiseur construit pour ce `+` est là où vit l'ingénierie intéressante. Le cours vous demande de comparer deux structures classiques.

**Additionneur à propagation de retenue (ripple-carry).** Chaînez huit additionneurs complets, en injectant la retenue sortante de chaque étage dans la retenue entrante du suivant. C'est minuscule et évident, mais la retenue doit *se propager* du bit 0 jusqu'au bit 7 avant que le bit de somme du haut ne soit valide — donc le délai du pire cas croît linéairement avec la largeur du mot. Pour un additionneur 8 bits, c'est huit délais d'additionneur complet empilés bout à bout.

**Additionneur à anticipation de retenue (CLA).** Au lieu d'attendre la propagation, calculez chaque retenue directement à partir des entrées. Définissez deux signaux par bit :

```
generate:  g(i) = a(i) and b(i)      -- ce bit crée une retenue tout seul
propagate: p(i) = a(i) xor b(i)      -- ce bit laisse passer une retenue entrante
```

Alors chaque retenue vaut `c(i+1) = g(i) or (p(i) and c(i))`, développée pour que les retenues soient produites en parallèle plutôt que l'une après l'autre. Le délai devient à peu près constant avec la largeur, au prix de plus de portes. C'est l'« acarreo anticipado » (retenue anticipée) que le programme oppose au design « acarreo en cascada » à retenue cascadée.

Pour ce projet vous n'avez à construire ni l'un ni l'autre à la main — écrire `a + b` laisse les outils FPGA choisir une chaîne de retenue optimisée (le tissu Artix-7 a même une logique de retenue rapide dédiée). Le but est de *comprendre* quel compromis ce `+` représente : surface contre vitesse.

## Bien gérer les drapeaux

Les drapeaux sont là où les débutants perdent discrètement des points, parce que **retenue** et **débordement** sont deux choses différentes :

- **La retenue (C)** est le bit qui tombe du haut d'une addition *non signée*. C'est le 9e bit de `a + b`.
- **Le débordement (V)** signale qu'un résultat *signé* (complément à deux) ne tient plus dans 8 bits. La règle propre : `V = retenue_entrante_du_MSB xor retenue_sortante_du_MSB`. De façon équivalente, il y a débordement si deux opérandes de même signe produisent un résultat de signe opposé.

Une astuce VHDL soignée pour les deux à la fois est d'additionner dans un espace de 9 bits :

```vhdl
signal ext : unsigned(8 downto 0);
...
ext   <= ('0' & unsigned(a)) + ('0' & unsigned(b));  -- addition 9 bits
result <= std_logic_vector(ext(7 downto 0));
cout   <= ext(8);                                     -- le drapeau de retenue
```

Pour SUB, souvenez-vous que la soustraction est `a + (not b) + 1` en complément à deux, donc `15 - 15` génère en interne une retenue sortante de `1` même si le résultat est nul — ce qui est exactement pourquoi beaucoup d'ISA traitent la retenue de soustraction comme un *emprunt* (C = not borrow). Gardez votre convention cohérente avec ce que le banc de test compagnon attend.

## Ce que le squelette vous donne

Le starter déclare l'entity, les signaux `result` et `cout`, et un squelette `process(op, a, b)` qui gère déjà ADD et SUB. Votre boulot : remplir les six autres branches du `case`.

## Indices

- Pour **AND / OR / XOR**, c'est une ligne chacune — affectez juste le résultat bit-à-bit :

  ```vhdl
  when "010" => result <= a and b;
  ```

- Pour **SHL** et **SHR**, complétez avec `'0'` :

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Pour **PASS_A**, copiez simplement `a` dans `result`.

Pensez à piloter `cout <= '0'` pour les ops logiques — vous le faites déjà une fois en haut du process, ce qui veut dire que toute branche qui ne le surcharge pas hérite de ce défaut. Pratique, mais ça veut dire qu'il faut être explicite quand une branche **veut** une retenue non nulle.

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

- Ajoutez un drapeau **signed** (résultat négatif pour SUB en complément à deux).
- Ajoutez un opcode **rotate** — comme SHL mais le bit de poids fort revient au bit 0.
- Ajoutez un opcode **multiply** produisant un résultat 16 bits sur deux ports de sortie séparés. La plupart des petits FPGA ont des blocs DSP dédiés pour ça — surveillez votre utilisation lors d'une nouvelle synthèse.

Cette ALU est une brique directe pour le projet CPU au niveau Expert — gardez ce design propre — vous le brancherez là-bas.

## À retenir

- Une ALU, c'est opérandes-en-entrée, résultat-et-drapeaux-en-sortie. L'opcode sélectionne quelle opération ce bloc unique exécute.
- Le `+` que vous écrivez cache un vrai choix : ripple-carry (petit, lent, délai qui croît avec la largeur) contre carry-lookahead (plus gros, rapide, délai à peu près constant) à base de termes generate/propagate.
- **Retenue ≠ débordement.** La retenue est le 9e bit non signé ; le débordement est `retenue_entrante(MSB) xor retenue_sortante(MSB)` et ne compte que pour les valeurs signées.
- Mettez un défaut sensé à chaque drapeau en haut du process, puis ne le surchargez que là où une branche en a vraiment besoin — c'est comme ça qu'on évite les latchs et les cas oubliés.
