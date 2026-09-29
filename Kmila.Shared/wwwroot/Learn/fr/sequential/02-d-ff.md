# Exemple : bascule D avec reset asynchrone

La bascule D (DFF) est la cellule mémoire la plus courante en design numérique. Un FPGA moderne en a des dizaines à des centaines de milliers. Tout registre, compteur, étage de pipeline en est construit.

## Ce qu'elle fait

À chaque front montant de `clk`, la valeur sur `d` est copiée vers `q`. Entre deux fronts, `q` garde sa valeur même si `d` change.

Quand `reset` est haut, `q` est forcé immédiatement à `'0'` — c'est le reset **asynchrone**.

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
- **`if reset = '1'`** — le reset est prioritaire. Si la broche reset est haute, on se moque de l'horloge et de `d`.
- **`elsif rising_edge(clk)`** — `d` n'est échantillonné que quand l'horloge passe de 0 à 1.

## Parcours du chronogramme

L'exemple pilote `reset = '1'` pendant les 5 premières µs, puis le relâche. `d` bascule à 10, 30 et 50 µs. Passez à l'onglet Formes d'onde après Exécuter et observez :

- Avant 5 µs, `q = '0'` (maintenu par le reset).
- Après le relâchement du reset, `q` ne change qu'aux fronts montants — la valeur de `d` au front est capturée.
- Les changements de `d` entre les fronts n'ont aucun effet.

## Sync vs async reset

Deux écoles de pensée :

**Reset async** (`if reset = '1'` hors du bloc edge, comme ci-dessus) :
- Pour : remet à zéro même si l'horloge ne tourne pas. Gère proprement le power-on.
- Contre : plus dur à boucler côté timing à très haute fréquence ; la recommandation Xilinx pour 7-series et plus est de l'éviter.

**Reset sync** (`if rising_edge(clk) then if reset = '1'…`) :
- Pour : se comporte comme tout autre signal au front ; s'entend bien avec le synthétiseur.
- Contre : il faut que l'horloge tourne pour reset.

Pour apprendre, l'async est plus facile à raisonner. En production, lisez les guidelines de votre vendeur.

## À vous de jouer

- Ajoutez une entrée **enable**. `q` ne se met à jour que sur front montant quand `enable = '1'`.
- Faites-en un **registre 4 bits** au lieu d'un seul bit. Changez `d` et `q` en `std_logic_vector(3 downto 0)` et ajustez la valeur de reset en `(others => '0')`.
- Ajoutez une sortie `q_not` qui est toujours l'inverse de `q`. Bonus : rebouclez `q_not` sur `d` et regardez apparaître une horloge divisée par deux.
