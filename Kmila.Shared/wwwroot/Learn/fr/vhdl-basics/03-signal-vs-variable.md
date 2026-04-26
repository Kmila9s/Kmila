# Signal vs variable

C'est la distinction la plus mal comprise de VHDL. Les étudiants lisent « une variable, c'est comme une variable locale en C » et filent dans la mauvaise direction. Mettons les pieds sur terre.

## Deux types de noms en VHDL

VHDL a des **signaux** (déclarés au niveau de l'architecture avec `signal`) et des **variables** (déclarées à l'intérieur d'un process avec `variable`). Les deux peuvent contenir une valeur. Les deux peuvent être assignés. Ils ne sont **pas** interchangeables.

## La règle d'or

- **Les assignations de signaux ont lieu à la fin du tick de simulation courant.**
- **Les assignations de variables ont lieu immédiatement, à l'intérieur du process.**

C'est tout. Chaque « piège » entre signaux et variables n'est qu'une reformulation de cette règle.

## La démo

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- requête : met s à 1 (pas encore visible)
      s <= 2;           -- requête : met s à 2 (remplace la première)
      -- que vaut s ici ? Toujours 0. Aucune assignation n'a pris effet.
    end if;
  end process;
end architecture demo;
```

Après que le front d'horloge s'achève, `s` devient **2** (seule la dernière assignation « gagne » quand plusieurs arrivent dans le même tick). À l'intérieur du process, après les deux assignations, `s` vaut toujours `0` — parce que la nouvelle valeur ne se propage que quand le process est terminé.

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
      -- v vaut 2 ici, maintenant, pendant ce tick
    end if;
  end process;
end architecture demo;
```

Remarque deux choses :

- Les variables utilisent `:=` ; les signaux utilisent `<=`.
- Les variables sont locales à un seul process.

## Pourquoi ça compte pour le matériel

Un signal **modélise un fil ou un flip-flop**. Tu ne peux pas changer la valeur d'un fil deux fois au même instant — ce qui pilote le fil pendant un tick se stabilise à une seule valeur à la fin de ce tick.

Une variable **modélise un calcul temporaire** à l'intérieur d'un process. Le synthétiseur est libre de l'implémenter en logique combinatoire, ou de l'optimiser complètement.

## Quand utiliser lequel

- **Signaux** — le défaut. Chaque port, chaque registre, chaque bus que tu vas voir sur le visualiseur de formes d'onde : signal.
- **Variables** — pour les compteurs de boucle locaux ou les résultats arithmétiques intermédiaires dans un process, où tu veux une sémantique à la C dans ce process.

## Un piège courant

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

Ils s'attendent à ce que ça échange `a` et `b`. Comme les assignations de signaux ont lieu à la fin du tick, **ça le fait** : sur le front, `a` reçoit l'**ancienne** valeur de `b`, et `b` reçoit l'**ancienne** valeur de `a`. C'est en fait la manière idiomatique d'échanger deux registres en un cycle.

Mais si tu passes aux variables, la sémantique change :

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

Pas d'échange. Juste les deux égaux à l'ancien `b`.

## À retenir

- `signal` : mise à jour à la fin du tick. Assignation `<=`. Modélise fils et registres.
- `variable` : mise à jour immédiate. Assignation `:=`. Locale à un process. Modélise un calcul intermédiaire.
- En cas de doute — utilise un signal.
