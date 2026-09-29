# Corrigez : le verrou accidentel

C'est l'un des deux ou trois bugs que tout étudiant VHDL écrit au moins une fois. Le code a l'air correct sur le papier, compile sans erreur, simule avec une sortie plausible — et quand vous synthétisez, l'outil vous prévient qu'il a inféré un **verrou** que vous n'aviez jamais demandé.

## Le starter

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- Manquant : que vaut y quand enable = '0' ?
  end process;
end architecture rtl;
```

## Ce qui ne va pas

Dans un process purement combinatoire, toute sortie doit être affectée **sur chaque chemin** à travers le process. Celui-ci n'affecte `y` que lorsque `enable = '1'`. Quand `enable = '0'`, le process ne dit rien sur `y`.

La solution de VHDL à « vous ne m'avez pas dit quoi faire de ce signal » : **garder la valeur précédente**. C'est un verrou — un élément mémoire transparent. La synthèse en infère un, votre design a un cauchemar de chemin de timing, et la carte glitchera aux transitions de signaux.

## Votre mission

Ouvrez l'exemple et rendez le design **purement combinatoire** — pas de verrou, pas de bascule, juste des portes. Deux corrections idiomatiques :

**Correction 1 : affectation par défaut en tête**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- défaut
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Un signal peut être assigné plusieurs fois dans un process — seule la dernière compte. Le défaut en tête veut dire « sauf si quelqu'un m'écrase, je vaux `'0'` ».

**Correction 2 : complète le `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';
  end if;
end process;
```

Les deux sont justes. Les deux produisent un matériel identique. Choisissez ce qui se lit mieux pour votre design.

## Vérifiez la correction

Après votre fix, lancez la sim. Dans le chronogramme :

- Pour `15–25 µs` (quand `enable = '1'` et `a=b='1'`) : `y = '1'`.
- À tout autre moment : `y = '0'`.

Avant le fix, `y` restait à `'1'` quand `enable` retombait à `'0'` — c'est le verrou qui tient sa valeur précédente, exactement ce que vous cherchez à éviter.

## La règle

Dans un process combinatoire, **toute sortie doit être affectée sur chaque chemin**. Si vous ne pouvez pas satisfaire ça, soit vous ajoutez un défaut, soit vous voulez en fait une bascule — dans ce cas passez à un process cadencé et assumez cette décision.
