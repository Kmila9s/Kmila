# Exemple : bascule D avec reset asynchrone

La bascule D (DFF) est la cellule mémoire la plus courante en design numérique — la vedette des « éléments de mémoire de type flip-flop » de l'Unité IV. Un FPGA moderne en compte des dizaines à des centaines de milliers. Chaque registre, compteur et étage de pipeline est construit à partir de bascules D.

## Flip-flop contre latch (la distinction à bien clouer)

Les deux stockent un bit, mais ils écoutent différemment :

- Un **latch** (verrou) est *sensible au niveau* : tant que son enable est haut, la sortie suit l'entrée (il est « transparent ») ; il ne retient que lorsque l'enable est bas.
- Une **flip-flop** (bascule) est *sensible au front* : elle échantillonne l'entrée à un seul instant — le front d'horloge — et retient pour le reste du cycle.

Cette leçon construit une bascule. L'exercice de correction suivant montre ce qui se passe quand vous construisez *accidentellement* un latch. Gardez la différence en tête : déclenché par front = flip-flop, déclenché par niveau = latch.

## Ce qu'elle fait

À chaque front montant de `clk`, la valeur présente sur `d` est copiée vers `q`. Entre deux fronts, `q` conserve sa valeur même si `d` change. Quand `reset` est haut, `q` est forcé immédiatement à `'0'` — c'est le reset **asynchrone**.

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

- **Liste de sensibilité `(clk, reset)`** — le process se réveille sur l'un ou l'autre. Inscrire `reset` est ce qui rend le reset asynchrone : il agit à l'instant même où `reset` monte, pas au prochain front d'horloge.
- **`if reset = '1'`** — le reset est prioritaire. Si la broche reset est haute, peu importe ce que font l'horloge ou `d`.
- **`elsif rising_edge(clk)`** — ce n'est que lorsque l'horloge passe de 0 à 1 que `d` est échantillonné dans `q`.

## Setup et hold : pourquoi les fronts ont besoin de respirer

Une bascule ne peut capturer `d` de façon fiable que si `d` est stable pendant une courte fenêtre **avant** le front (temps de setup) et **après** lui (temps de hold). Violez cette fenêtre et la sortie peut devenir *métastable* — hésitant entre `'0'` et `'1'` avant de se figer de façon imprévisible. Vous ne modélisez pas ces temps dans cet exemple, mais ils sont la raison pour laquelle les vrais designs enregistrent les entrées asynchrones à travers deux bascules (un « synchroniseur ») avant de les utiliser. Bon à savoir dès maintenant ; vous le reverrez.

## Parcours du chronogramme

L'exemple pilote `reset = '1'` pendant les 5 premières µs, puis le relâche. `d` bascule à 10, 30 et 50 µs. Passez à l'onglet Formes d'onde après Exécuter et remarquez :

- Avant 5 µs, `q = '0'` (maintenu par le reset).
- Après le relâchement du reset, `q` ne change qu'aux fronts montants de l'horloge — c'est la valeur de `d` *au front* qui est capturée.
- Les changements de `d` entre les fronts d'horloge n'ont aucun effet sur `q`.

## Reset synchrone contre asynchrone

Il y a deux écoles, et le choix a de vraies conséquences :

**Reset asynchrone** (`if reset = '1'` hors du bloc de front, comme ci-dessus) :
- Avantages : remet à zéro même si l'horloge ne tourne pas ; gère proprement la mise sous tension.
- Inconvénients : le *relâchement* du reset n'est pas aligné sur l'horloge, donc différentes bascules peuvent quitter le reset sur des fronts différents — un risque de métastabilité — et cela peut limiter la fréquence d'horloge maximale.

**Reset synchrone** (`if rising_edge(clk) then if reset = '1' then …`) :
- Avantages : se comporte comme n'importe quelle autre entrée synchrone ; se mappe directement sur le reset intégré de la bascule et s'entend bien avec la fermeture temporelle (timing closure).
- Inconvénients : a besoin que l'horloge tourne pour prendre effet.

Pour *apprendre*, le reset asynchrone est plus facile à raisonner, c'est pourquoi cet exemple l'utilise. Pour le *travail FPGA de production*, les recommandations des fabricants (par exemple le livre blanc « Get Smart About Reset » d'AMD/Xilinx et les guides méthodologiques ultérieurs) favorisent généralement les resets **synchrones**, et une partie de la logique à roue libre n'a besoin d'aucun reset. Suivez la recommandation de votre composant cible.

## Les autres bascules

Le cours couvre aussi les bascules SR, JK et T. Ce sont toutes une bascule D avec un peu de logique combinatoire à l'avant qui calcule l'état suivant :

- **T (toggle) :** `d = q xor t` → retient quand `t=0`, bascule quand `t=1`.
- **JK :** `d = (j and not q) or (not k and q)`.
- **SR :** set/reset, avec le cas `s=r=1` défini à part.

Donc une fois que vous savez écrire une bascule D propre, vous pouvez construire n'importe laquelle d'entre elles.

## À vous de jouer

- Ajoutez une entrée **enable** : `q` ne doit se mettre à jour sur le front montant que lorsque `enable = '1'`.
- Faites-en un **registre de bus 4 bits** : passez `d` et `q` en `std_logic_vector(3 downto 0)` et la valeur de reset en `(others => '0')`.
- Ajoutez une sortie `q_not` qui est toujours `not q`. Bonus : rebouclez `q_not` sur `d` et regardez-la devenir une horloge **divisée par deux** — une bascule T déguisée.
- Convertissez le reset en **synchrone** et comparez les chronogrammes au moment du relâchement du reset.

## À retenir

- Une bascule D échantillonne `d` au front d'horloge et retient entre les fronts — déclenchée par front, contrairement à un latch sensible au niveau.
- `process(clk, reset)` avec le reset testé *avant* `rising_edge` donne un reset asynchrone ; placez-le *à l'intérieur* du front pour un reset synchrone.
- Respectez setup/hold ; synchronisez les entrées asynchrones pour éviter la métastabilité.
- Les bascules SR, JK et T ne sont qu'une bascule D plus la logique d'état suivant.
