# Exemple : D flip-flop avec reset asynchrone

Le D flip-flop (DFF) est la cellule mémoire la plus courante en design numérique. Un FPGA moderne en compte des dizaines à des centaines de milliers. Chaque registre, compteur et étage de pipeline est construit à partir de DFF.

## Ce qu'il fait

À chaque front montant de `clk`, la valeur sur `d` est copiée vers `q`. Entre deux fronts, `q` conserve sa valeur même si `d` change.

Quand `reset` est haut, `q` est forcé à `'0'` immédiatement — c'est le reset **asynchrone**.

## Le code

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Ligne par ligne :

- **Liste de sensibilité `(clk, reset)`** — le process se réveille sur l'un ou l'autre. C'est ce qui rend le reset asynchrone.
- **`if reset = '1'`** — le reset a priorité. Si la broche de reset est haute, peu nous importe ce que font l'horloge ou `d`.
- **`elsif rising_edge(clk)`** — ce n'est que lorsque l'horloge passe de 0 à 1 que `d` est échantillonnée.

## Promenade dans la forme d'onde

L'exemple pilote `reset = '1'` pendant les 5 premières µs, puis le relâche. `d` bascule à 10, 30 et 50 µs. Passe à l'onglet Waveforms après Run et remarque :

- Avant 5 µs, `q = '0'` (maintenu par le reset).
- Après le relâchement du reset, `q` ne change que sur les fronts montants d'horloge — la valeur de `d` au front est capturée.
- Les changements de `d` entre deux fronts d'horloge n'ont aucun effet.

## Sync vs async reset

Il y a deux écoles :

**Reset async** (`if reset = '1'` hors du bloc de front, comme ci-dessus) :
- Pour : reset même si l'horloge n'est pas active. Gère proprement la mise sous tension.
- Contre : plus difficile à respecter les timings à très haute fréquence ; la recommandation de Xilinx pour les 7-series et suivants est de l'éviter.

**Reset sync** (`if rising_edge(clk) then if reset = '1'…`) :
- Pour : se comporte comme toute autre entrée au front d'horloge ; joue bien avec le synthétiseur.
- Contre : requiert que l'horloge tourne pour faire le reset.

Pour apprendre, le reset async est plus facile à raisonner. Pour le travail FPGA en production, lis les recommandations de ton fournisseur.

## À toi

- Ajoute une entrée **enable**. `q` ne doit se mettre à jour sur le front montant que quand `enable = '1'`.
- Fais-en un **registre de bus 4 bits** au lieu d'un seul bit. Change `d` et `q` en `std_logic_vector(3 downto 0)` et ajuste la valeur de reset à `(others => '0')`.
- Ajoute une sortie `q_not` qui est toujours l'inverse de `q`. Bonus : reboucle `q_not` sur `d` et regarde-la devenir une horloge divisée par deux.
