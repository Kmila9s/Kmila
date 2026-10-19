# Delta-cycles et conditions de course

Un simulateur doit donner un ordre déterministe à « deux choses qui se passent en même temps ». La réponse de VHDL est le **delta-cycle** : une tranche de temps infinitésimale qui sépare la cause de l'effet sans faire avancer l'horloge murale. Il est défini précisément dans le cycle de simulation du standard VHDL, et le comprendre est ce qui fait soudain prendre tout leur sens aux comportements « bizarres » d'un banc de test.

## Ce qu'est un delta-cycle

Le temps de simulation avance par pas réels (mesurés en ns, µs, ms). Mais à l'intérieur d'un seul pas de temps, les changements se produisent dans un ordre **delta-cycle** :

- δ₀ : une affectation planifie une mise à jour de signal.
- δ₁ : la mise à jour prend effet ; tout process sensible à ce signal voit maintenant de nouvelles entrées et se réveille.
- δ₂ : ces process réveillés planifient *leurs* mises à jour.
- δ₃ : ces mises à jour prennent effet ; d'autres process peuvent se réveiller.
- …

Chaque delta vaut « zéro temps réel » mais représente quand même un pas causal. Le simulateur continue de boucler sur les deltas jusqu'à ce qu'aucun signal ne change plus — le circuit est **stable** — et c'est seulement alors que le temps réel de simulation avance jusqu'au prochain événement.

## Le cycle de simulation, un peu plus précisément

Une passe dans le moteur, c'est : (1) faire avancer le temps jusqu'au prochain événement planifié ; (2) mettre à jour tous les signaux planifiés pour maintenant ; (3) exécuter jusqu'au bout chaque process qui attendait l'un de ces signaux, ce qui peut planifier de nouvelles mises à jour ; (4) si certaines concernent le temps *courant*, faire un autre delta-cycle ; sinon, faire avancer le temps. Les mises à jour de signaux de l'étape 2 ont toutes lieu *avant* qu'un process ne les lise à l'étape 3 — c'est le mécanisme derrière « les signaux se mettent à jour à la fin du pas ».

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

Sans delta-cycles, le simulateur devrait choisir un ordre arbitraire — `a` est-il mis à jour avant `b` ? Après ? Les delta-cycles contournent le problème : les deux affectations sont planifiées à δ₀ et prennent toutes deux effet à δ₁. Tout process qui lit `a` ou `b` voit les deux nouvelles valeurs ensemble. Le résultat ne dépend plus de l'ordre dans lequel le simulateur visite les choses.

## Une chaîne de flot de données

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

Quand `a` change, le premier process se déclenche et planifie `b`. Au delta suivant, `b` a sa nouvelle valeur, ce qui réveille le second process, qui planifie `c`. Au delta d'après, `c` est correct. Remarquez que `c` rattrape `a` au bout de **deux delta-cycles** — le temps réel n'a jamais bougé, mais la causalité a été préservée dans l'ordre. Intervertissez les deux process dans le fichier et rien ne change : l'ordonnancement par delta garantit le même résultat final. C'est exactement pour ça que les delta-cycles existent.

## Le piège de la condition de course

Le problème survient quand vous mélangez la sémantique **signal** avec l'hypothèse que « ceci doit se passer avant cela à l'intérieur du même tick » :

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a se met à jour à la fin du pas
    b <= a + 1;     -- b lit l'ANCIEN a
  end if;
end process;
```

Si vous avez supposé « la ligne 1 s'exécute d'abord, donc `a` est déjà à jour quand la ligne 2 tourne », vous vous trompez. Les deux membres droits sont évalués avec les **anciennes** valeurs, puis les deux résultats sont planifiés. `b` lit l'ancienne valeur de `a`, pas celle qui vient d'être calculée.

Remplacez les signaux par des variables pour retrouver la sémantique à la C, « relis ce que tu viens d'écrire » :

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

Maintenant ça fonctionne comme les programmeurs logiciels s'y attendent. (Et le matériel diffère : la version à signaux fabrique deux additionneurs parallèles à partir des anciens registres ; la version à variables fabrique une chaîne dépendante.)

## Un vrai bug classique : le front oublié

Le raisonnement en deltas explique aussi pourquoi ce stimulus de banc de test se comporte mal :

```vhdl
clk  <= not clk after 10 ns;
-- ailleurs, dans un process sans wait :
data <= next_value;   -- même delta que le front que le DUT échantillonne ?
```

Si un signal de stimulus change dans le *même* delta que celui où le DUT prend son horloge, savoir si le DUT voit l'ancienne ou la nouvelle valeur est fragile. Le remède est de changer le stimulus à une marge sûre *loin* du front actif (ou d'utiliser `wait until rising_edge(clk)` puis un petit délai). Beaucoup de rapports du type « ça passe en sim mais l'ordre semble aléatoire » remontent à ça.

## Repérer les deltas dans les outils

GHDL (le moteur que Kmila pilote) et GTKWave exposent tous deux les pas de delta — un signal peut mettre plusieurs delta-cycles à se propager jusqu'à sa valeur finale en un seul point temporel. Si un chronogramme montre un glitch qui occupe « zéro temps », vous regardez des valeurs de delta intermédiaires, pas un comportement matériel réel.

## À emporter

- Les delta-cycles sont la façon dont le simulateur rend la logique concurrente déterministe sans inventer de faux temps.
- Les affectations de signal prennent effet **après** le delta courant ; pas les variables — elles sont immédiates.
- Les membres droits sont évalués avec les *anciennes* valeurs de signaux, puis planifiés.
- Si votre banc de test se comporte « bizarrement » autour d'un front d'horloge, soupçonnez un problème d'ordonnancement de delta avant de blâmer l'outil.

## À retenir

- Un delta-cycle vaut zéro temps réel mais un pas causal ; le simulateur exécute des deltas jusqu'à ce que le circuit soit stable, puis fait avancer le temps.
- Les signaux se mettent à jour entre les deltas (donc vous lisez les anciennes valeurs dans la même passe) ; les variables se mettent à jour instantanément.
- Éloignez le stimulus du banc de test du front d'horloge actif pour éviter les courses.

L'exercice suivant montre une petite condition de course en action. Trouvez-la, nommez-la, corrigez-la — vous n'oublierez plus jamais la règle.
