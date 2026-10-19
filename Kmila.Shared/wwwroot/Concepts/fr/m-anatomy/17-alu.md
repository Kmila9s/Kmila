# UAL — unité arithmétique et logique

L'**UAL** est la calculatrice au cœur d'un processeur : elle prend deux opérandes A et B et, selon une **sélection d'opération**, produit A+B, A−B, A ET B, A OU B, et plus.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/17-alu.html" title="UAL interactive avec un tambour de sélection d'opération" loading="lazy"></iframe></div>

Le tambour de sélection du module choisit l'opération ; les poussoirs fixent **A** et **B**. La liste surligne l'opération courante et l'afficheur montre **A op B = R**. C'est purement combinatoire — sans horloge.

En VHDL :

```vhdl
with op select
  r <= a + b     when "00",   -- ADD
       a - b     when "01",   -- SUB
       a and b   when "10",   -- AND
       a or b    when others; -- OR
```

Une UAL est de la **logique combinatoire** (additionneurs, portes et MUX qui choisissent le résultat) sans mémoire propre — les registres autour gardent opérandes et résultat. La sélection d'opération n'est qu'un MUX sur les résultats candidats.
