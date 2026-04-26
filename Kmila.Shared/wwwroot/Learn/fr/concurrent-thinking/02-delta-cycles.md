# Cycles delta et conditions de course

Un simulateur doit donner à « deux choses qui se produisent en même temps » un ordre déterministe. La réponse de VHDL s'appelle le **cycle delta** : une tranche de temps infiniment petite qui sépare la cause de l'effet.

## Ce qu'est un cycle delta

Le temps de simulation avance par étapes réelles (mesurées en ns, µs, ms). Mais à l'intérieur d'un même pas de temps, des changements peuvent avoir lieu dans l'ordre des **cycles delta** :

- δ₀ : tu planifies une mise à jour de signal.
- δ₁ : la mise à jour prend effet ; tout process sensible à ce signal a maintenant de nouvelles entrées.
- δ₂ : ces process planifient leurs mises à jour.
- δ₃ : ces mises à jour prennent effet.
- …

Chaque delta représente « zéro temps réel » mais correspond à une étape causale. Le simulateur continue de cycler à travers les deltas jusqu'à ce que plus rien n'ait besoin de se mettre à jour — alors, et seulement alors, le temps réel avance.

## Pourquoi ça existe

Considère deux signaux qui se mettent tous deux à jour sur le même front d'horloge :

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

Sans les cycles delta, le simulateur devrait choisir un ordre arbitraire — `a` est-il mis à jour avant `b` ? Après ? Exactement au même instant ? Les cycles delta évitent ce problème : les deux assignations sont planifiées à δ₀ et prennent toutes deux effet à δ₁. Tout process qui lit `a` ou `b` voit les deux nouvelles valeurs en même temps.

## Le piège de la condition de course

Regarde ceci :

```vhdl
process(a)
begin
  b <= a;
end process;

process(b)
begin
  c <= b;
end process;
```

Quand `a` change, le premier process se déclenche et planifie `b`. À δ₁, `b` a sa nouvelle valeur. Le second process se déclenche, planifie `c`. À δ₂, `c` a la bonne valeur.

Maintenant, échange les deux process. Quelque chose change-t-il ? **Non.** Les cycles delta garantissent que les deux chemins produisent le même résultat final — même si l'« ordre » des process dans le fichier est différent. C'est exactement pour ça que les cycles delta existent.

## Là où ça tourne mal

Le problème, c'est quand tu mélanges la sémantique des **signaux** avec « j'ai besoin que ça arrive avant ça dans le même tick ». Exemple :

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a est mis à jour à la fin du tick
    b <= a + 1;     -- b lit l'ANCIEN a
  end if;
end process;
```

Si tu as supposé que « la ligne 1 s'exécute en premier, donc `a` est déjà à jour quand la ligne 2 s'exécute », tu te trompes. `a` se met à jour à la fin du tick. `b` lit l'ancienne valeur de `a`. Résultat : les deux s'incrémentent à partir des anciennes données, pas à partir des nouvelles données de l'autre.

Remplace les signaux par des variables pour obtenir une sémantique à la C :

```vhdl
process(clk)
  variable aa, bb : integer;
begin
  if rising_edge(clk) then
    aa := bb + 1;   -- immédiat
    bb := aa + 1;   -- voit le nouveau aa
  end if;
end process;
```

Maintenant ça fonctionne comme les programmeurs logiciels s'y attendent.

## À retenir

- Les cycles delta sont la façon du simulateur de rendre la logique concurrente déterministe.
- Les assignations de signaux prennent effet **après** les cycles delta. Les variables, non.
- Si ton testbench se comporte « bizarrement » autour d'un front d'horloge, soupçonne un problème d'ordre de cycle delta avant d'accuser l'outil.

L'exercice suivant du programme montre une petite condition de course à l'œuvre. Trouve-la, nomme-la, corrige-la — tu n'oublieras plus jamais la règle.
