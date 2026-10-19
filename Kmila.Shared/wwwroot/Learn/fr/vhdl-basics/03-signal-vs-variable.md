# Signal vs. variable

C'est la distinction la plus mal comprise de tout le VHDL. Les étudiants lisent « une variable, c'est comme une variable locale en C » et filent dans la mauvaise direction. Recadrons les choses, car bien saisir ce point, c'est ce qui sépare le code qui *simule* du code qui *devient le matériel que vous aviez en tête*.

## Deux sortes de noms en VHDL

VHDL a des **signaux** (déclarés au niveau de l'architecture avec `signal`) et des **variables** (déclarées à l'intérieur d'un process avec `variable`). Les deux peuvent contenir une valeur. Les deux peuvent être affectés. Ils ne sont **pas** interchangeables.

| | signal | variable |
|---|---|---|
| Déclaré | architecture (ou package) | dans un process / sous-programme |
| Opérateur d'affectation | `<=` | `:=` |
| Prend effet | à la fin du pas de simulation | immédiatement |
| Portée | partagé dans toute l'architecture | local à son process |
| Modélise | un fil ou un registre | un calcul temporaire |

## La règle d'or

- **Les affectations de signal sont *planifiées* ; la valeur apparaît à la fin du pas de simulation courant.**
- **Les affectations de variable ont lieu immédiatement, là où la ligne se trouve.**

Voilà. Chaque « piège » signal vs variable n'est qu'une reformulation de cette règle.

## La démo

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- planifie : mettre s à 1 (pas encore visible)
      s <= 2;           -- planifie : mettre s à 2 (écrase la première)
      -- combien vaut s ici  ? Toujours 0. Aucune affectation n'a pris effet.
    end if;
  end process;
end architecture demo;
```

Une fois le front d'horloge terminé, `s` devient **2** — quand un signal est planifié plusieurs fois dans une même passe du process, seule la *dernière* affectation l'emporte. À l'intérieur du process, après les deux affectations, `s` vaut toujours `0`, parce que la nouvelle valeur ne se propage pas tant que le process n'a pas fini.

Maintenant avec une variable :

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- la variable vit à l'intérieur du process
  begin
    if rising_edge(clk) then
      v := 1;           -- prend effet immédiatement
      v := 2;           -- prend effet immédiatement
      -- v vaut 2 ici, tout de suite, pendant cette passe
    end if;
  end process;
end architecture demo;
```

Remarquez deux choses :

- Les variables utilisent `:=` ; les signaux utilisent `<=`.
- Les variables sont locales à un seul process — vous ne pouvez pas lire `v` depuis un autre process ni depuis une affectation concurrente.

> Une nuance sur la valeur initiale : `variable v : integer := 0;` n'exécute son `:= 0` qu'**une seule fois**, au démarrage de la simulation — pas à chaque passe du process. Une variable conserve sa valeur entre les passes tant que vous ne la réaffectez pas. C'est ce qui la rend capable de *se souvenir*, et pourquoi une variable lue avant d'être écrite dans un process cadencé peut accidentellement inférer un registre.

## Pourquoi ça compte pour le matériel

Un signal **modélise un fil ou une bascule**. Vous ne pouvez pas changer la valeur d'un fil deux fois au même instant — quoi qu'il pilote le fil pendant un pas, elle se stabilise à une seule valeur à la fin de ce pas.

Une variable **modélise un calcul temporaire** à l'intérieur d'un process. Le synthétiseur l'implémente généralement en logique combinatoire qui alimente (ou qui part de) les registres — ou l'optimise entièrement.

## Un exemple travaillé : compter les bits à 1

Les variables brillent quand vous avez besoin d'un résultat intermédiaire que vous mettez à jour dans une boucle :

```vhdl
process(clk)
  variable ones : integer range 0 to 8;
begin
  if rising_edge(clk) then
    ones := 0;                      -- remet l'accumulateur à zéro à chaque passe
    for i in data'range loop
      if data(i) = '1' then
        ones := ones + 1;           -- mise à jour immédiate, relue à l'itération suivante
      end if;
    end loop;
    popcount <= ones;               -- publie le compte final sur un signal
  end if;
end process;
```

Essayez ça avec un signal au lieu d'une variable et ça casse : `ones <= ones + 1` dans la boucle planifierait huit affectations concurrentes, seule la dernière survit, et vous obtiendriez toujours `0` ou `1`. La boucle a besoin de la sémantique *immédiate* d'une variable.

## Quand utiliser quoi

- **Les signaux** — par défaut. Chaque port, chaque registre, chaque bus que vous verrez sur le visualiseur de chronogrammes : un signal.
- **Les variables** — pour des compteurs de boucle locaux ou des résultats arithmétiques intermédiaires à l'intérieur d'un seul process, où vous voulez la sémantique à la C : relire ce que vous venez d'écrire.

## Un piège fréquent

Certains étudiants écrivent :

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- registre 1
    b <= a;    -- registre 2
  end if;
end process;
```

Ils craignent que ça n'échange pas `a` et `b`. Comme les affectations de signal sont planifiées, ça **les échange bel et bien** : au front, `a` reçoit l'**ancienne** valeur de `b`, et `b` reçoit l'**ancienne** valeur de `a`, simultanément. C'est en réalité la façon idiomatique d'échanger deux registres en un cycle.

Mais passez aux variables et la sémantique change :

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a vaut maintenant b
    b := a;    -- b reçoit le NOUVEAU a — donc b garde son ancienne valeur, et a change
  end if;
end process;
```

Pas d'échange. Les deux finissent égaux à l'ancien `b`. Un code d'apparence identique, un résultat opposé — tout ça à cause de la règle d'or.

## À retenir

- `signal` : la valeur est planifiée, elle apparaît à la fin du pas ; affectation `<=` ; modélise fils et registres ; partagé dans toute l'architecture.
- `variable` : se met à jour immédiatement ; affectation `:=` ; locale à un process ; modélise un calcul intermédiaire.
- Plusieurs affectations de signal dans une passe → la dernière l'emporte. Plusieurs affectations de variable → elles s'exécutent toutes, dans l'ordre.
- Dans le doute — prenez un signal.
