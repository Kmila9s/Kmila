# Exemple : votre première entity (MUX 2 vers 1)

Un **multiplexeur** est le dispositif combinatoire le plus simple qui comporte un « choix ». La version 2 vers 1 sélectionne entre deux entrées selon un seul bit `sel` :

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

Le VHDL correspondant tient sur une ligne :

```vhdl
y <= a when sel = '0' else b;
```

## Le fichier complet

Pour que vous puissiez voir les quatre sections des leçons précédentes réunies au même endroit :

```vhdl
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux21 is
  port (
    a   : in  std_logic;
    b   : in  std_logic;
    sel : in  std_logic;
    y   : out std_logic
  );
end entity mux21;

architecture rtl of mux21 is
begin
  y <= a when sel = '0' else b;
end architecture rtl;
```

## Ce que fait l'exemple

Ouvrez l'exemple et vous obtenez un projet nommé **Votre première entity** avec :

- `mux21.vhd` (le design ci-dessus)
- Une config de simulation compagnon qui pilote `sel`, `a` et `b` sur 40 microsecondes

Quand vous cliquez sur Exécuter, l'onglet Formes d'onde affiche quatre lignes — les trois entrées et la sortie. Regardez `y` basculer entre `a` et `b` au gré des changements de `sel`.

## Choses à remarquer

L'affectation `y <= a when sel = '0' else b;` est une **instruction concurrente** : elle vit en dehors de tout process et décrit un câblage permanent. Il n'y a pas de « quand cette ligne s'exécute-t-elle ? » — elle *est*, tout simplement. Si `a`, `b` ou `sel` change, `y` se re-stabilise après un délai de propagation.

La condition à l'intérieur d'un `when/else` peut être n'importe quelle expression booléenne, et vous pouvez les enchaîner :

```vhdl
y <= a   when sel = '0' else
     b   when enable = '1' else
     'Z';
```

Des chaînes comme celle-ci se synthétisent en multiplexeur **prioritaire** (la condition la plus haute l'emporte en cas d'égalité).

## La théorie en dessous : l'expansion de Shannon

Cette seule ligne n'est pas qu'une syntaxe commode — c'est la forme matérielle d'un théorème d'algèbre de Boole que le cours appelle le **théorème d'expansion de Shannon**. Toute fonction d'une variable `s` peut se découper ainsi :

```
f(s, …) = (s' · f(0, …)) + (s · f(1, …))
```

Un mux 2 vers 1 calcule exactement `y = sel'·a + sel·b`. Autrement dit, un multiplexeur est la réalisation physique de l'expansion de Shannon autour du bit de sélection — c'est pourquoi des fonctions plus grandes se construisent si souvent en empilant des muxes. Gardez cette idée en poche ; c'est elle qui sous-tend l'unité multiplexeur/démultiplexeur plus loin dans le cours.

## À vous de jouer : étendez-le

Une fois que vous avez vu l'exemple fonctionner :

1. Transformez l'entity en un **MUX 4 vers 1** — `sel` devient un `std_logic_vector` de 2 bits, et vous choisissez entre `a`, `b`, `c`, `d`. (Une instruction `with sel select` se lit très bien ici — vous la rencontrerez dans le module combinatoire.)
2. Ajoutez un cinquième port `output_enable`. Quand il vaut `'0'`, pilotez `y <= 'Z'` (haute impédance) quel que soit `sel`.
3. Construisez un **démultiplexeur** — l'image miroir. Une entrée, `sel`, et plusieurs sorties ; l'entrée est aiguillée vers une seule sortie tandis que les autres restent à `'0'`.

L'éditeur de Kmila met en évidence les mots-clés VHDL et connaît `std_logic` — survolez `when` pour voir la documentation intégrée.

## À retenir

- Une **entity** nomme l'interface ; l'**architecture** donne le comportement. Le mux est un exemple complet et minimal des deux.
- `y <= a when sel = '0' else b;` est une affectation conditionnelle concurrente — un câblage permanent, pas une ligne qui « s'exécute ».
- Un multiplexeur, c'est l'expansion de Shannon gravée dans le silicium : `y = sel'·a + sel·b`. Cette seule idée se généralise à tous les muxes plus larges que vous construirez.
