# Corrige : le verrou accidentel

C'est l'un des deux ou trois bugs que tout étudiant VHDL écrit au moins une fois. Le code a l'air correct sur le papier, compile sans erreur, simule avec une sortie plausible — et quand tu synthétises, l'outil te prévient qu'il a inféré un **verrou** que tu n'avais jamais demandé.

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

La solution de VHDL à « tu ne m'as pas dit quoi faire de ce signal » : **garder la valeur précédente**. C'est un verrou — un élément mémoire transparent. La synthèse en infère un, ton design a un cauchemar de chemin de timing, et la carte glitchera aux transitions de signaux.

## Ta mission

Ouvre l'exemple et rends le design **purement combinatoire** — pas de verrou, pas de bascule, juste des portes. Deux corrections idiomatiques :

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

Les deux sont justes. Les deux produisent un matériel identique. Choisis ce qui se lit mieux pour ton design.

## Vérifie la correction

Après ton fix, lance la sim. Dans le chronogramme :

- Pour `15–25 µs` (quand `enable = '1'` et `a=b='1'`) : `y = '1'`.
- À tout autre moment : `y = '0'`.

Avant le fix, `y` restait à `'1'` quand `enable` retombait à `'0'` — c'est le verrou qui tient sa valeur précédente, exactement ce que tu cherches à éviter.

## La règle

Dans un process combinatoire, **toute sortie doit être affectée sur chaque chemin**. Si tu ne peux pas satisfaire ça, soit tu ajoutes un défaut, soit tu veux en fait une bascule — dans ce cas passe à un process cadencé et assume cette décision.
