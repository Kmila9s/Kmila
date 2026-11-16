# Listes de sensibilité et cycles delta

Tu as vu que les signaux ne se mettent pas à jour avant « la fin du cycle delta ». On va maintenant rencontrer le piège classique qui découle de cette règle.

Une **liste de sensibilité** est la `( … )` qui suit `process`. Elle dit au simulateur : *ne réveille ce `process` que lorsque l'un de ces signaux change*.

Un **cycle delta** est la plus petite unité de temps de simulation. À l'intérieur d'un cycle delta, les affectations de signaux ne font que **programmer** de nouvelles valeurs — les valeurs elles-mêmes n'apparaissent qu'à la fin du delta. Pendant tout ce temps, lire le signal te donne son **ancienne** valeur.

Combine ces deux règles et tu obtiens le piège ci-dessous. Le lecteur fait tourner un tout petit `process` avec deux affectations de signal :

```vhdl
process(a, b)
begin
  out1 <= a;
  out2 <= b and out1;   -- ← qu'est-ce que ceci lit ?
end process;
```

Ça a l'air anodin. L'étudiant pense en général : « La ligne 3 met `out1 = a`, puis la ligne 4 lit `out1` et fait un AND avec `b`. » Mais la ligne 4 lit l'**ancien** `out1` — la transaction de la ligne 3 n'a pas encore été validée.

Regarde le lecteur et tu verras `out2` sortir à `U` alors même que `a` et `b` valent tous les deux `1`. Surprenant la première fois.

<!-- exec-fig2:start -->
## Les sonnettes

Un process est une pièce dont l'employé **dort** jusqu'à ce qu'une sonnette retentisse. La **liste de sensibilité** décide quels signaux ont une sonnette.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-doorbell.html?mode=bug" title="Un process endormi, réveillé par des sonnettes reliées à sa liste de sensibilité" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- Avec `process(a, b)`, `out1` **n'a pas de sonnette** (son fil finit sur une prise vide ✕). Quand la ligne 4 s'exécute, l'employé lit `out1` et voit son **ancienne** valeur U. Ensuite `out1` passe à 1, mais personne ne se réveille, donc `out2` reste à U.
- Clique sur le bouton `process(a, b, out1)` : la sonnette de `out1` retentit, le process s'exécute une seconde fois et `out2` passe à 1. C'est l'option B ci-dessous.
<!-- exec-fig2:end -->

## Les deux façons de corriger ça

### Option A — utiliser une variable

Les variables se mettent à jour immédiatement, donc lire `tmp` à la ligne suivante te donne la valeur qui vient d'être affectée :

```vhdl
process(a, b)
  variable tmp : std_logic;
begin
  tmp  := a;
  out1 <= tmp;
  out2 <= b and tmp;   -- lit le NOUVEAU tmp — ça marche.
end process;
```

### Option B — attendre le deuxième delta

Ajoute `out1` à la liste de sensibilité. Après que le premier delta a validé `out1`, le `process` se réveille une deuxième fois et se ré-exécute :

```vhdl
process(a, b, out1)
begin
  out1 <= a;
  out2 <= b and out1;   -- le deuxième réveil lit out1 maintenant validé
end process;
```

Les deux options marchent ; l'option A est en général préférée parce qu'elle est plus rapide d'un delta et que le flot de données est explicite.

> **Essaie ceci :** avance pas à pas dans le lecteur et regarde la table des signaux. Même avec `a=1` et `b=1`, `out2` finit à `U` — pas à `1`. C'est la règle du cycle delta qui piège les débutants chaque semestre.
