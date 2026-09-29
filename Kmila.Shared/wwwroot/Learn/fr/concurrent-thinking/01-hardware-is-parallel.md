# Le matériel est parallèle

Les étudiants venant de C, Python ou JavaScript ont tous intériorisé le même modèle mental : « le code s'exécute de haut en bas, une instruction à la fois ». VHDL vous punira pour cette hypothèse jusqu'à ce que vous construisiez le bon modèle.

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

En langage logiciel, vous lisez ça comme trois étapes séquentielles. En VHDL, les trois lignes décrivent **des fils qui existent simultanément**. Ils ne s'exécutent pas en ordre — ils existent.

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

Les trois portes sont alimentées en même temps. Quand `a` ou `b` change, la sortie `y3` se stabilise à sa nouvelle valeur après un petit délai de propagation par le chemin le plus long. Il n'y a pas de « premier » ou de « second ».

## Pourquoi l'ordre n'importe pas

Vous pouvez écrire les trois lignes dans n'importe quel ordre :

```vhdl
y3 <= y1 xor y2;   -- référence avant « déclaration » ?
y1 <= a and b;
y2 <= a or  b;
```

C'est toujours du VHDL valide. Les trois affectations décrivent trois fils, et les fils se moquent de l'ordre dans lequel vous avez dessiné le schéma. L'outil lit les trois et câble.

## Là où le « séquentiel » revient en douce

À l'intérieur d'un `process`, les choses **sont** séquentielles — au sein de ce process, une instruction à la fois, de haut en bas. Mais un process dans son ensemble n'est qu'une unité concurrente de plus, à côté des autres.

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

Les trois unités concurrentes vivent en même temps. Chacune s'exécute dès que ses entrées changent. À l'intérieur du process, le corps est lu en ordre, mais c'est une propriété locale au process, pas à l'architecture.

## Une conséquence concrète

Ce design a une course subtile :

```vhdl
architecture wrong of demo is
  signal a, b : std_logic;
begin
  a <= not b;
  b <= not a;
end architecture wrong;
```

Lu comme du logiciel, ça ressemble à « mets `a` à not-b, puis mets `b` à not-a » — pas de souci. Lu comme du matériel, c'est deux inverseurs câblés en boucle. C'est un **oscillateur** — un anneau de feedback qui ne peut jamais se stabiliser. La simulation râle, la synthèse (parfois) l'optimise, et la carte glitche imprévisiblement.

## Le recâblage dans votre tête

- Arrêtez de penser « qu'est-ce qui s'exécute en premier ? »
- Commencez à penser « quels fils existent ? »
- À l'intérieur d'un process, l'ancien modèle « un à la fois » revient — mais seulement là.

Une fois que ça clique, VHDL arrête de se battre contre vous. Vous lirez une architecture et vous verrez tout de suite le schéma qu'elle représente, plus l'« exécution du programme » que vous imaginiez avant.
