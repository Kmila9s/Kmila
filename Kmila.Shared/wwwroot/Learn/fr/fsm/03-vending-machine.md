# Projet : distributeur à pièces

C'est votre premier vrai projet. La spec est fixée, le code de démarrage vous tend la plomberie, et votre boulot est d'écrire la logique qui le complète.

## Ce que vous construisez

Une FSM qui suit l'argent qu'un client a inséré et délivre un produit (une seule impulsion de sortie, `dispense`) quand le total atteint 25¢. Les entrées sont :

- `nickel` — impulsion d'1 cycle : « une pièce de 5¢ vient d'entrer »
- `dime`   — impulsion d'1 cycle : « une pièce de 10¢ vient d'entrer »
- `clk`, `reset`

Et les sorties :

- `dispense` — pulse haut pendant exactement un cycle d'horloge quand vous avez atteint ≥ 25¢
- `change`   — pulse haut pendant un cycle en même temps que `dispense` si le total était **plus** que 25¢ (la machine doit rendre la monnaie)

## Critères d'acceptation

| Séquence de pièces   | Comportement attendu          |
|----------------------|-------------------------------|
| 5 nickels            | `dispense=1, change=0`        |
| 2 dimes + 1 nickel   | `dispense=1, change=0`        |
| 3 dimes (= 30¢)      | `dispense=1, change=1`        |
| 2 nickels + 1 dime   | pas de dispense (seulement 20¢) |

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
2. **Pulsez `dispense` pendant exactement un cycle.** Astuce : quand vous détectez la vente, mettez un drapeau et remettez `total` à zéro. Le drapeau ne vit qu'un cycle.
3. **Calculez `change`.** Si le total au moment de la vente était > 25, pulsez aussi `change`.

Indice : c'est plus simple si vous calculez le « total suivant » comme un signal local (ou `variable` dans le process) :

```vhdl
variable next_total : unsigned(5 downto 0);
...
next_total := total;
if    nickel = '1' then next_total := total + 5;
elsif dime   = '1' then next_total := total + 10;
end if;

if next_total >= 25 then
  dispense <= '1';
  change   <= '1' when next_total > 25 else '0';
  total    <= (others => '0');
else
  dispense <= '0';
  change   <= '0';
  total    <= next_total;
end if;
```

## Pour aller plus loin

- Ajoutez un bouton **refund** qui remet `total` à zéro sans distribuer.
- Supportez une pièce **quarter** (25¢) — doit distribuer immédiatement.
- Suivez le nombre d'articles vendus dans un compteur 8 bits séparé visible en sortie. C'est le genre de retour que les vrais distributeurs envoient vers le haut.
