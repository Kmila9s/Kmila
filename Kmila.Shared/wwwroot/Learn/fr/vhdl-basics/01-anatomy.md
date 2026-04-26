# Anatomie d'un fichier VHDL

Chaque fichier VHDL a les mêmes quatre sections, toujours dans le même ordre. Une fois que tu reconnais la forme, lire du code inconnu cesse d'être intimidant — tu sais simplement où chercher chaque morceau.

## Les quatre sections

```vhdl
library IEEE;                          -- 1. bibliothèques
use IEEE.STD_LOGIC_1164.ALL;           -- 2. packages utilisés

entity my_design is                    -- 3. entity (l'« en-tête »)
  port (
    a : in  std_logic;
    b : in  std_logic;
    y : out std_logic
  );
end entity my_design;

architecture rtl of my_design is       -- 4. architecture (le « corps »)
begin
  y <= a and b;
end architecture rtl;
```

Passons en revue chacune d'elles.

### 1. `library`

`library IEEE;` importe la bibliothèque standard IEEE. C'est l'équivalent, en design numérique, de `#include <stdio.h>` — presque tous les fichiers que tu écriras commencent par ça.

### 2. `use`

`use IEEE.STD_LOGIC_1164.ALL;` amène tous les noms du package `STD_LOGIC_1164` dans ta portée. C'est ce qui te donne `std_logic`, `std_logic_vector`, `rising_edge()`, et compagnie.

Tu verras souvent un second `use` :

```vhdl
use IEEE.NUMERIC_STD.ALL;
```

Celui-là te donne `unsigned`, `signed`, `to_integer`, `resize`, et l'arithmétique sur les types vectoriels. Ajoute-le dès que tu fais de l'arithmétique sur des vecteurs.

### 3. `entity`

L'entité est l'**interface boîte noire**. Elle dit :

- comment s'appelle le design (`my_design`)
- quels ports il a
- pour chaque port : sa **direction** (`in`, `out`, `inout`, `buffer`) et son **type**

Tout ce qui est dans `port (…)` est ce que le monde extérieur voit. Rien sur le fonctionnement du design n'est déclaré ici.

### 4. `architecture`

L'architecture est l'**implémentation**. Une entité peut avoir plusieurs architectures (pour la même interface) — tu peux avoir `rtl`, `behavioral` et `tb` comme architectures de la même entité `cpu` et en choisir une par configuration. En pratique, la plupart des fichiers en ont exactement une.

Le corps entre `begin` et `end architecture` est là où les signaux et les portes sont réellement décrits.

## Comment Kmila parse ça

Le parseur VHDL de Kmila lit ton fichier de haut en bas et construit un arbre :

```
File
├── library IEEE
├── use IEEE.STD_LOGIC_1164.ALL
└── entity my_design
    ├── port a (in, std_logic)
    ├── port b (in, std_logic)
    ├── port y (out, std_logic)
    └── architecture rtl
        └── concurrent: y <= a and b
```

Quand tu cliques sur **Build**, les ports deviennent les entrées dans l'onglet **Ports** du dock, l'entité devient l'entrée dans **Entities**, et les assignations dans l'architecture deviennent le câblage que le simulateur exécute.

## Conventions de nommage

VHDL est insensible à la casse. `SIGNAL`, `Signal` et `signal` sont le même mot-clé. Mais garde un style unique dans un fichier donné — la plupart des équipes utilisent **minuscules** pour les mots-clés et les noms de signaux, **snake_case** pour les identifiants, et **MAJUSCULES** pour les constantes.

## À retenir

- `library` + `use` — tes imports.
- `entity` — l'interface (ports et directions).
- `architecture` — l'implémentation (ce qui se passe réellement).
- Une entité peut avoir plusieurs architectures ; la plupart des projets en livrent une appelée `rtl`.
