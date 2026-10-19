# Corrigez : le verrou accidentel

C'est l'un des deux ou trois bugs que tout étudiant VHDL écrit au moins une fois. Le code a l'air correct sur le papier, compile sans erreur, simule avec une sortie plausible — et quand vous le synthétisez, l'outil vous avertit qu'il a inféré un **verrou** (latch) que vous n'avez jamais demandé.

L'ironie, c'est que l'Unité IV vous apprend à construire des verrous (SR, D) *exprès*. La compétence ici, c'est de reconnaître quand vous en avez construit un **par accident**, à l'intérieur de ce qui était censé être de la logique purement combinatoire.

## Le point de départ

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- Manquant : que devrait valoir y quand enable = '0' ?
  end process;
end architecture rtl;
```

## Ce qui ne va pas

Dans un process purement combinatoire, chaque sortie doit être affectée **sur chaque chemin** à travers le process. Celui-ci n'affecte `y` que lorsque `enable = '1'`. Quand `enable = '0'`, le process ne dit rien à propos de `y`.

La réponse de VHDL à « tu ne m'as pas dit quoi faire de ce signal » est : **garder la valeur précédente**. Le seul matériel capable de garder une valeur précédente est un élément de mémoire — donc la synthèse infère un **verrou transparent**. C'est exactement un latch D : tant que `enable = '1'`, la sortie suit `a and b` ; quand `enable` retombe, la dernière valeur est figée. Vous avez demandé une porte et vous avez eu un verrou avec un enable.

## Pourquoi un verrou accidentel est mauvais

Ce n'est pas que les verrous soient maléfiques — c'est qu'un verrou *non voulu* détruit votre design :

- **Le timing est difficile à fermer.** L'analyse temporelle statique a du mal avec les verrous transparents ; la plupart des flots FPGA supposent des bascules déclenchées par front et traitent les verrous inférés comme un avertissement, à juste titre.
- **Les glitches passent à travers.** Tant qu'il est transparent, tout glitch sur `a` ou `b` file directement vers `y` et peut se retrouver verrouillé.
- **Risque d'écart sim/synthèse.** Le comportement dépend d'une couverture d'affectation subtile, facile à rater.

Donc quand l'outil dit « verrou inféré pour le signal `y` », traitez ça comme une erreur à corriger, pas comme un avertissement à ignorer.

## Votre mission

Ouvrez l'exemple et rendez le design **purement combinatoire** — pas de verrou, pas de bascule, juste des portes. Deux corrections idiomatiques :

**Correction 1 : affectation par défaut en tête**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- défaut : couvre chaque chemin
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Un signal peut être affecté plusieurs fois dans une même passe du process — seule la dernière affectation exécutée prend effet. Le défaut en tête veut dire « sauf si quelque chose en dessous m'écrase, je vaux `'0'` ». C'est l'habitude qui passe à l'échelle : poser les défauts d'abord, puis écraser.

**Correction 2 : compléter le `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';                     -- la branche manquante
  end if;
end process;
```

Les deux sont justes. Les deux produisent un matériel identique (une porte AND à 2 entrées validée par `enable`, sortie à `'0'` quand désactivée). Choisissez celle qui se lit le mieux.

## Le même piège dans un `case`

Les verrous ne sont pas qu'un problème de `if`. Un `case` incomplet, ou une branche qui oublie une sortie, infère aussi un verrou :

```vhdl
-- BUG : 'y' n'est pas affecté pour sel = "10" et "11"
case sel is
  when "00" => y <= a;
  when "01" => y <= b;
  when others => null;   -- null n'affecte rien -> verrou sur y
end case;
```

Même remède : donnez à `y` un défaut avant le `case`, ou affectez-le dans `when others` (et dans chaque branche). `process(all)` vous aide à éviter l'erreur de *liste de sensibilité*, mais il ne vous sauve **pas** de l'erreur de couverture — vous devez toujours affecter chaque sortie sur chaque chemin.

## Vérifiez la correction

Après votre fix, lancez la sim. Dans le chronogramme :

- Pour `15–25 µs` (quand `enable = '1'` et `a = b = '1'`) : `y = '1'`.
- À tout autre moment : `y = '0'`.

Avant le fix, `y` restait à `'1'` quand `enable` retombait à `'0'` — c'est le verrou qui tient sa valeur précédente, exactement ce que vous cherchez à éviter.

## À retenir

- Dans un process combinatoire, **chaque sortie doit être affectée sur chaque chemin** — un `if`/`case` qui rate un chemin infère un verrou transparent.
- Une affectation par défaut en tête du process est la façon la plus simple de garantir la couverture.
- Un verrou *accidentel* cause des problèmes de timing et de glitch ; un verrou *intentionnel* (latch D/SR) est un choix de design délibéré de l'Unité IV.
- `process(all)` corrige les listes de sensibilité, pas la couverture des affectations — vous devez toujours affecter chaque sortie.
- Si vous voulez vraiment de la mémoire, ne la laissez pas s'inviter en douce — passez à un process cadencé et assumez la décision.
