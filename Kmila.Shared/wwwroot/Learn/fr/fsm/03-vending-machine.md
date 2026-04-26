# Projet : distributeur automatique à pièces

C'est ton premier vrai projet. Le cahier des charges est fixe, le code de départ te livre la plomberie, et ton boulot est d'écrire la logique qui le complète.

## Ce que tu vas construire

Une FSM qui suit l'argent qu'un client a inséré et distribue un produit (une unique impulsion de sortie, `dispense`) quand le total atteint 25¢. Les entrées sont :

- `nickel` — une impulsion d'un cycle signifiant « une pièce de 5¢ vient de tomber »
- `dime`   — une impulsion d'un cycle signifiant « une pièce de 10¢ vient de tomber »
- `clk`, `reset`

Et les sorties :

- `dispense` — impulsion haute pendant exactement un cycle d'horloge quand tu as atteint ≥ 25¢
- `change`   — impulsion haute pendant un cycle en même temps que `dispense` si le total était **plus** que 25¢ (donc la machine doit rendre la monnaie)

## Critères d'acceptation

| Séquence de pièces    | Comportement attendu          |
|-----------------------|-------------------------------|
| 5 nickels             | `dispense=1, change=0`        |
| 2 dimes + 1 nickel    | `dispense=1, change=0`        |
| 3 dimes (= 30¢)       | `dispense=1, change=1`        |
| 2 nickels + 1 dime    | pas de distribution (seulement 20¢) |

## Ce que te donne le starter

L'entité et l'accumulateur `total` sont câblés. Chaque impulsion `nickel` ajoute 5 ; chaque impulsion `dime` ajoute 10. Mais le starter force toujours `dispense = '0'` et `change = '0'`. **C'est ce que tu dois changer.**

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

1. **Détecte la condition de vente.** À chaque front montant d'horloge, vérifie si `total >= 25` après avoir ajouté la pièce courante. (Attention : tu dois calculer le *nouveau* total, pas celui du cycle précédent.)
2. **Pulse `dispense` pendant exactement un cycle.** L'astuce : quand tu détectes la vente, lève un signal flag et remets `total` à zéro. Le flag vit un cycle.
3. **Calcule `change`.** Si le total au moment de la vente était > 25, pulse `change` aussi.

Indice : c'est plus simple si tu calcules le « next total » comme signal local (ou `variable` dans le process) :

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

- Ajoute un bouton **refund** qui remet `total` à zéro sans distribuer.
- Supporte une pièce de **25¢ (quarter)** — doit distribuer immédiatement.
- Suis le nombre d'articles vendus dans un compteur 8 bits séparé, visible en sortie. C'est le genre de feedback que les vrais distributeurs envoient en amont.
