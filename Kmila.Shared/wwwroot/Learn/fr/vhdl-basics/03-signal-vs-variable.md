# Signal vs. variable

C'est la distinction la plus mal comprise du VHDL. Les étudiants lisent « une variable c'est comme une variable locale en C » et partent dans la mauvaise direction. Recadrons.

## Deux types de noms en VHDL

VHDL a des **signaux** (déclarés au niveau de l'architecture avec `signal`) et des **variables** (déclarées dans un process avec `variable`). Les deux peuvent contenir des valeurs. Les deux peuvent être assignés. Ils ne sont **pas** interchangeables.

## La règle d'or

- **Les affectations de signal se passent à la fin du tick de simulation courant.**
- **Les affectations de variable se passent immédiatement, dans le process.**

Voilà. Tout « piège » signal vs variable est une reformulation de cette règle.

## La démo

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- demande : mettre s à 1 (pas encore visible)
      s <= 2;           -- demande : mettre s à 2 (écrase la première)
      -- combien vaut s ici ? Toujours 0. Aucune affectation n'a pris effet.
    end if;
  end process;
end architecture demo;
```

Après que le front d'horloge soit terminé, `s` devient **2** (seule la dernière affectation « gagne » quand plusieurs ont lieu dans le même tick). À l'intérieur du process, après les deux affectations, `s` est toujours `0` — parce que la nouvelle valeur ne se propage pas tant que le process n'a pas fini.

Maintenant avec une variable :

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- variable vit dans le process
  begin
    if rising_edge(clk) then
      v := 1;           -- prend effet immédiatement
      v := 2;           -- prend effet immédiatement
      -- v vaut 2 ici, tout de suite, pendant ce tick
    end if;
  end process;
end architecture demo;
```

Notez deux choses :

- Les variables utilisent `:=` ; les signaux utilisent `<=`.
- Les variables sont locales à un seul process.

## Pourquoi ça compte pour le matériel

Un signal **modélise un fil ou une bascule**. Vous ne pouvez pas changer la valeur d'un fil deux fois au même instant — ce qui pilote le fil pendant un tick se stabilise à une valeur à la fin de ce tick.

Une variable **modélise un calcul temporaire** à l'intérieur d'un process. Le synthétiseur est libre de l'implémenter en combinatoire, ou de l'optimiser complètement.

## Quand utiliser quoi

- **Signaux** — par défaut. Chaque port, chaque registre, chaque bus que vous verrez sur le visualiseur de chronogrammes : signal.
- **Variables** — pour des compteurs de boucle locaux ou des résultats arithmétiques intermédiaires dans un process, où vous voulez la sémantique style C à l'intérieur de ce process.

## Un piège fréquent

Certains étudiants écrivent :

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- registre 1
    b <= a;    -- registre 2 — « échange » a et b
  end if;
end process;
```

Ils s'attendent à ce que ça échange `a` et `b`. Comme les affectations de signal ont lieu à la fin du tick, **c'est** ce que ça fait : au front, `a` reçoit l'**ancienne** valeur de `b`, et `b` reçoit l'**ancienne** valeur de `a`. C'est en fait la façon idiomatique d'échanger deux registres en un cycle.

Mais si vous passez aux variables, la sémantique change :

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a égale maintenant b
    b := a;    -- b reçoit le NOUVEAU a — donc b garde son ancienne valeur, et a change
  end if;
end process;
```

Pas d'échange. Juste les deux égaux à l'ancien `b`.

## À retenir

- `signal` : mis à jour en fin de tick. Affectation `<=`. Modélise les fils et registres.
- `variable` : mis à jour immédiatement. Affectation `:=`. Locale à un process. Modélise les calculs intermédiaires.
- Dans le doute — prenez un signal.
