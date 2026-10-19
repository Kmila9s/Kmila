# Le matériel est parallèle

Les étudiants venant de C, Python ou JavaScript ont tous intériorisé le même modèle mental : « le code s'exécute de haut en bas, une instruction à la fois ». VHDL vous punira pour cette hypothèse jusqu'à ce que vous construisiez le bon modèle mental. C'est le plus grand saut conceptuel de toute l'unité HDL, alors ça vaut la peine de ralentir ici.

## Le grand basculement

Regardez cette architecture :

```vhdl
architecture rtl of demo is
begin
  y1 <= a and b;
  y2 <= a or  b;
  y3 <= y1 xor y2;
end architecture rtl;
```

Dans un langage logiciel, vous liriez ça comme trois étapes séquentielles. En VHDL, les trois lignes décrivent **des fils qui existent simultanément**. Elles ne s'exécutent pas dans un ordre — elles existent.

Le synthétiseur construit :

```mermaid
flowchart LR
    A([a]) --> AND["AND"]
    B([b]) --> AND
    A --> OR["OR"]
    B --> OR
    AND -- y1 --> XOR["XOR"]
    OR  -- y2 --> XOR
    XOR --> Y3([y3])
```

Les trois portes sont alimentées en même temps. Quand `a` ou `b` change, `y3` se stabilise à sa nouvelle valeur après un petit délai de propagation par le chemin le plus long. Il n'y a pas de « premier » ni de « second ».

## Pourquoi l'ordre n'a pas d'importance

Vous pourriez écrire les trois lignes dans n'importe quel ordre :

```vhdl
y3 <= y1 xor y2;   -- « référence avant déclaration » ?
y1 <= a and b;
y2 <= a or  b;
```

C'est toujours du VHDL valide et ça produit le circuit identique. Les trois affectations décrivent trois fils, et les fils se moquent de l'ordre dans lequel vous avez dessiné le schéma. L'outil lit les trois et les relie entre eux.

## Le moteur événementiel derrière tout ça

Alors comment un ordinateur *séquentiel* simule-t-il tout ça « d'un coup » ? Chaque affectation concurrente se comporte comme un petit process doté d'une liste de sensibilité implicite : elle est **réévaluée dès qu'un signal de son membre droit change**. Le simulateur tient une file d'événements en attente ; quand `a` change, il réveille chaque affectation qui lit `a`, celles-ci recalculent et peuvent planifier de nouveaux événements, et ainsi de suite jusqu'à ce que le circuit soit calme. Cette « stabilisation » est pourquoi la concurrence est déterministe plutôt que chaotique — un point que la leçon suivante (les delta-cycles) rend précis.

## Là où le « séquentiel » revient en douce

À l'intérieur d'un `process`, les choses **sont** séquentielles — au sein de ce process, les instructions s'exécutent une à la fois, de haut en bas. Mais un process *pris dans son ensemble* n'est jamais qu'une unité concurrente de plus, assise à côté de toutes les autres.

```vhdl
architecture rtl of demo is
begin

  -- Concurrent 1
  y1 <= a and b;

  -- Concurrent 2 (un process, séquentiel à l'intérieur)
  process(clk)
  begin
    if rising_edge(clk) then
      counter <= counter + 1;
    end if;
  end process;

  -- Concurrent 3
  y2 <= a or b;

end architecture rtl;
```

Les trois unités concurrentes sont vivantes en même temps. Chacune s'exécute dès que ses entrées changent. À l'intérieur du process, le corps est lu dans l'ordre, mais c'est une propriété locale du process, pas de l'architecture. En fait, une affectation de signal concurrente est *exactement* un raccourci pour un process d'une seule ligne — les deux sont interchangeables.

## Une conséquence concrète : les boucles combinatoires

Ce design a un bug subtil :

```vhdl
architecture wrong of demo is
  signal a, b : std_logic;
begin
  a <= not b;
  b <= not a;
end architecture wrong;
```

Lu comme du logiciel, ça ressemble à « mets `a` à not-b, puis mets `b` à not-a » — aucun problème. Lu comme du matériel, c'est deux inverseurs câblés en boucle sans registre pour la rompre. C'est une **boucle combinatoire** : un anneau de rétroaction qui ne peut jamais se stabiliser. Selon les délais, soit il oscille, soit il se fige dans un état imprévisible. La simulation va tourner à travers les delta-cycles (et peut signaler une erreur de limite d'itérations), les outils de synthèse avertiront d'une boucle combinatoire, et la carte glitchera.

> Le remède est toujours le même : rompre la rétroaction avec un registre. Si vous *voulez* de la mémoire, faites passer la boucle par un process cadencé (c'est comme ça qu'on construit une bascule). Sinon, vous avez une erreur de design.

## Comptez le matériel

Une habitude utile pendant que vous recâblez votre cerveau : lisez une architecture et comptez les portes. Le premier exemple fait 3 portes (AND, OR, XOR). Celui-ci —

```vhdl
y <= (a and b) or (c and d) or (e and f);
```

— ce sont trois portes AND qui alimentent une porte OR à 3 entrées, toutes vivantes en même temps. Vous ne comptez plus des *étapes* ; vous comptez des *pièces*.

## Le recâblage dans votre tête

- Arrêtez de penser « qu'est-ce qui s'exécute en premier ? »
- Commencez à penser « quels fils existent, et qu'est-ce qui alimente quoi ? »
- À l'intérieur d'un process, le vieux modèle « un à la fois » revient — mais seulement à l'intérieur de ce process.
- Ne refermez jamais un chemin combinatoire sur lui-même sans registre.

Une fois que ça clique, VHDL arrête de se battre contre vous. Vous lirez une architecture et verrez immédiatement le schéma qu'elle représente, pas l'« exécution de programme » que vous imaginiez avant.

## À retenir

- Les instructions concurrentes décrivent des fils qui existent tous en même temps ; leur ordre dans le fichier est sans importance.
- Le simulateur simule ça avec une file d'événements — les affectations se réexécutent dès qu'une entrée change.
- Un `process` est une unité concurrente ; ce n'est que *dedans* que le code est séquentiel.
- Une boucle de rétroaction combinatoire sans registre est un bug — rompez-la avec un process cadencé.
