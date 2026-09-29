# Delta-cycles et conditions de course

Un simulateur doit donner un ordre déterministe à « deux choses qui se passent en même temps ». La réponse de VHDL est le **delta-cycle** : une tranche de temps infiniment petite qui sépare la cause de l'effet.

## Ce qu'est un delta-cycle

Le temps de simulation avance en pas réels (mesurés en ns, µs, ms). Mais à l'intérieur d'un seul pas de temps, des changements peuvent arriver dans un ordre **delta-cycle** :

- δ₀ : vous planifiez une mise à jour de signal.
- δ₁ : la mise à jour prend effet ; tout process sensible à ce signal a maintenant de nouvelles entrées.
- δ₂ : ces process planifient leurs mises à jour.
- δ₃ : ces mises à jour prennent effet.
- …

Chaque delta vaut « zéro temps réel » mais représente un pas causal. Le simulateur boucle sur les deltas jusqu'à ce que plus rien n'ait besoin d'être mis à jour — alors et seulement alors le temps réel avance.

## Pourquoi ça existe

Considérez deux signaux qui se mettent tous deux à jour sur le même front d'horloge :

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

Sans delta-cycles, le simulateur devrait choisir un ordre arbitraire — `a` mis à jour avant `b` ? Après ? Au même instant exact ? Les delta-cycles contournent ça : les deux affectations sont planifiées à δ₀ et prennent toutes deux effet à δ₁. Tout process qui lit `a` ou `b` voit les deux nouvelles valeurs en même temps.

## Le piège de la course

Regardez ça :

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

Quand `a` change, le premier process s'allume et planifie `b`. À δ₁, `b` a sa nouvelle valeur. Le second process s'allume, planifie `c`. À δ₂, `c` a la bonne valeur.

Maintenant intervertissez les deux process. Est-ce que quelque chose change ? **Non.** Les delta-cycles garantissent que les deux chemins produisent le même résultat final — même si l'« ordre » des process dans le fichier est différent. C'est exactement pourquoi les delta-cycles existent.

## Là où ça déraille

Le problème est quand vous mélangez sémantique **signal** avec « j'ai besoin que ceci se passe avant cela dans le même tick ». Exemple :

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a se met à jour en fin de tick
    b <= a + 1;     -- b lit l'ANCIEN a
  end if;
end process;
```

Si vous supposez « la ligne 1 s'exécute en premier, donc `a` est déjà à jour quand la ligne 2 tourne », vous vous trompez. `a` se met à jour en fin de tick. `b` lit l'ancienne valeur de `a`. Résultat : les deux s'incrémentent sur des données anciennes, pas sur les nouvelles données de l'autre.

Remplacez les signaux par des variables pour retrouver la sémantique style C :

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

Maintenant ça marche comme les programmeurs logiciels s'y attendent.

## À retenir

- Les delta-cycles sont la manière du simulateur de rendre la logique concurrente déterministe.
- Les affectations de signaux prennent effet **après** les delta-cycles. Pas les variables.
- Si votre banc de test se comporte « bizarrement » autour d'un front d'horloge, suspectez un problème d'ordre de delta-cycle avant de blâmer l'outil.

L'exercice suivant du cursus montre une petite course en action. Trouvez-la, nommez-la, corrigez-la — vous n'oublierez plus jamais la règle.
