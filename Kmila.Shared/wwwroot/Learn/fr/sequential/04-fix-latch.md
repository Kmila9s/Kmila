# À corriger : le latch accidentel

C'est l'un des deux ou trois bugs que tout étudiant VHDL écrit au moins une fois. Le code a l'air correct sur le papier, se compile sans erreur, simule avec une sortie plausible — et quand tu le synthétises, l'outil t'avertit qu'il a inféré un **latch** que tu n'as jamais demandé.

## Le starter

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- Manquant : que doit valoir y quand enable = '0' ?
  end process;
end architecture rtl;
```

## Ce qui cloche

Dans un process purement combinatoire, chaque sortie doit être assignée **sur chaque chemin** à travers le process. Celui-ci n'assigne `y` que quand `enable = '1'`. Quand `enable = '0'`, le process ne dit rien à propos de `y`.

La solution de VHDL à « tu ne m'as pas dit quoi faire avec ce signal » : **se souvenir de la valeur précédente**. C'est un latch — un élément mémoire transparent. La synthèse va en inférer un, ton design aura un cauchemar de chemin temporel, et la carte va glitcher sur les transitions de signal.

## Ta mission

Ouvre l'exemple et rends le design **purement combinatoire** — pas de latch, pas de flip-flop, juste des portes. Deux corrections idiomatiques :

**Fix 1 : assignation par défaut en tête**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- défaut
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Un signal peut être assigné plusieurs fois dans un process — seule la dernière assignation prend effet. Le défaut en haut veut dire « à moins que quelque chose ne me remplace, vaux `'0'` ».

**Fix 2 : compléter le `if`**

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

Les deux sont corrects. Les deux produisent un matériel identique. Choisis celui qui se lit le mieux pour le design sur lequel tu travailles.

## Vérifie la correction

Après ta correction, lance la sim. Dans la forme d'onde :

- De `15–25 µs` (quand `enable = '1'` et `a=b='1'`) : `y = '1'`.
- À tout autre moment : `y = '0'`.

Avant la correction, `y` restait à `'1'` quand `enable` retombait à `'0'` — c'est le latch qui conservait sa valeur précédente, ce que tu cherches précisément à éviter.

## La règle

Dans un process combinatoire, **chaque sortie doit être assignée sur chaque chemin**. Si tu ne peux pas satisfaire ça, soit ajoute un défaut, soit tu veux en réalité un flip-flop — et dans ce cas, passe à un process cadencé et assume cette décision.
