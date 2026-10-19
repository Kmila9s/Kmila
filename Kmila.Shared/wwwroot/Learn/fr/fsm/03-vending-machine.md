# Projet : distributeur à pièces

C'est votre premier vrai projet. La spec est fixée, le code de démarrage vous tend la plomberie, et votre boulot est d'écrire la logique qui le complète.

## Ce que vous construisez

Une FSM qui suit l'argent qu'un client a inséré et délivre un produit (une seule impulsion de sortie, `dispense`) quand le total atteint 25¢. Les entrées sont :

- `nickel` — impulsion d'1 cycle : « une pièce de 5¢ vient d'entrer »
- `dime`   — impulsion d'1 cycle : « une pièce de 10¢ vient d'entrer »
- `clk`, `reset`

Et les sorties :

- `dispense` — pulse haut pendant exactement un cycle d'horloge quand vous avez atteint ≥ 25¢
- `change`   — pulse haut pendant un cycle en même temps que `dispense` si le total était **plus** que 25¢ (la machine doit donc rendre la monnaie)

## Deux façons de modéliser ça

Un distributeur est une FSM classique, et il y a deux façons honnêtes de le décrire :

- **États explicites.** Énumérez chaque total atteignable — `S0, S5, S10, S15, S20` — et tracez une transition pour chaque pièce. C'est l'approche de manuel, et c'est ce que vous feriez à la main pour un petit jeu de pièces. Ça rend le diagramme d'états évident mais explose si vous ajoutez des pièces ou un prix plus élevé.
- **Style accumulateur (chemin de données).** Gardez le total dans un petit registre et comparez-le au prix. Moins de lignes, passe à l'échelle de n'importe quel prix, et c'est la forme qu'utilise le code de démarrage. Vous pouvez voir le registre `total` comme l'état et le comparateur comme la logique de prochain état.

Dans les deux cas, `dispense` dépend de la pièce qui arrive *ce* cycle-ci, il se comporte donc comme une sortie **Mealy** — il réagit dans le cycle même où la condition de vente devient vraie, au lieu d'attendre un cycle.

## Ce que vous donne le starter

L'entity et l'accumulateur `total` sont câblés. Chaque pulse `nickel` ajoute 5 ; chaque pulse `dime` ajoute 10. Mais le starter pilote toujours `dispense = '0'` et `change = '0'`. **C'est ça que vous devez changer.**

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    total <= (others => '0');
  elsif rising_edge(clk) then
    if    nickel = '1' then total <= total + 5;
    elsif dime   = '1' then total <= total + 10;
    end if;
  end if;
end process;

dispense <= '0';  -- TODO
change   <= '0';  -- TODO
```

## Approche suggérée

1. **Détectez la condition de vente.** À chaque front montant, vérifiez si `total >= 25` *après* avoir ajouté la pièce courante. (Attention : vous devez calculer le *nouveau* total, pas celui du cycle précédent.)
2. **Pulsez `dispense` pendant exactement un cycle.** L'astuce : quand vous détectez la vente, mettez l'impulsion et remettez `total` à zéro. Comme `total` est effacé sur le même front, la condition est fausse au cycle suivant, donc l'impulsion fait naturellement un cycle de large.
3. **Calculez `change`.** Si le total au moment de la vente était > 25, pulsez aussi `change`.

Calculez le « total suivant » comme une `variable` à l'intérieur du process pour que la décision utilise la valeur à jour, pas le registre du cycle précédent :

```vhdl
process(clk, reset)
  variable next_total : unsigned(5 downto 0);
begin
  if reset = '1' then
    total    <= (others => '0');
    dispense <= '0';
    change   <= '0';
  elsif rising_edge(clk) then
    next_total := total;
    if    nickel = '1' then next_total := total + 5;
    elsif dime   = '1' then next_total := total + 10;
    end if;

    if next_total >= 25 then
      dispense <= '1';
      if next_total > 25 then change <= '1'; else change <= '0'; end if;
      total    <= (others => '0');
    else
      dispense <= '0';
      change   <= '0';
      total    <= next_total;
    end if;
  end if;
end process;
```

Deux choses à remarquer. Une `variable` s'affecte avec `:=` et se met à jour **immédiatement** à l'intérieur du process, ce qui est exactement pourquoi `next_total` inclut déjà la pièce de ce cycle — un signal (`<=`) ne se mettrait pas à jour tant que le process n'est pas suspendu. Et comme `dispense`, `change` et `total` sont tous pilotés dans le process cadencé, ils sont **registrés** — les impulsions s'alignent sur les fronts d'horloge et ne glitcheront pas.

## Attention à

- **La largeur.** `total + 10` peut atteindre 30 avant que le reset ne se déclenche, donc dimensionnez le registre pour au moins 6 bits — `unsigned(5 downto 0)` tient 0–63 avec de la marge. Un vecteur trop étroit reboucle en silence.
- **Deux pièces à la fois.** Le `if … elsif` fait qu'un `nickel` et un `dime` simultanés ne comptent que le nickel. Décidez si c'est acceptable ou une erreur pour votre spec.
- **Oublier d'effacer `total`.** Si vous pulsez `dispense` mais ne remettez pas l'accumulateur à zéro, la machine « vend » à nouveau chaque cycle jusqu'à ce que d'autres pièces changent le total. Effacer sur le front de vente est ce qui rend l'impulsion large d'un cycle.

## Critères d'acceptation

| Séquence de pièces   | Comportement attendu          |
|----------------------|-------------------------------|
| 5 nickels            | `dispense=1, change=0`        |
| 2 dimes + 1 nickel   | `dispense=1, change=0`        |
| 3 dimes (= 30¢)      | `dispense=1, change=1`        |
| 2 nickels + 1 dime   | pas de dispense (seulement 20¢) |

## Pour aller plus loin

- Ajoutez un bouton **refund** qui remet `total` à zéro sans distribuer.
- Supportez une pièce **quarter** (25¢) — doit distribuer immédiatement.
- Suivez le nombre d'articles vendus dans un compteur 8 bits séparé visible en sortie. C'est le genre de retour que les vrais distributeurs envoient vers le haut.

## Liens avec le cours

C'est l'*aplicaciones con autómatas y lenguaje de descripción de hardware* de l'unité II : une machine séquentielle déterministe spécifiée à partir d'un comportement, puis décrite en HDL. La forme accumulateur-plus-comparateur est aussi une entrée en douceur vers l'unité V, où vous découperez explicitement un design en un **chemin de données** (le registre `total` et l'additionneur) et un automate de **contrôle** qui le pilote.

## À retenir

- Un distributeur est une FSM ; modélisez-le avec des états explicites pour la clarté, ou un registre accumulateur pour l'échelle.
- Utilisez une `variable` (`:=`) pour le « total suivant » afin que le test de vente inclue la pièce qui vient d'arriver.
- Faites une impulsion d'un cycle en effaçant l'accumulateur sur le front de vente — des sorties registrées gardent l'impulsion propre.
- Dimensionnez les vecteurs pour la plus grande valeur atteignable, et décidez d'emblée du comportement des entrées simultanées.
