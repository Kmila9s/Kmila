# Les sept portes logiques

Tout circuit numérique, jusqu'au GPU moderne, est une combinaison de sept cellules élémentaires. Si tu sais comment chacune se comporte et comment l'écrire en VHDL, tu as déjà commencé à faire du design matériel.

## Les six portes à deux entrées

| Porte | Symbole VHDL  | Se lit          | La sortie vaut `1` quand… |
|-------|---------------|------------------|---------------------------|
| AND   | `a and b`     | « A et B »       | les deux entrées sont à 1 |
| OR    | `a or b`      | « A ou B »       | au moins une entrée est à 1 |
| XOR   | `a xor b`     | « A ou-exclusif B » | les entrées sont différentes |
| NAND  | `a nand b`    | « non (A et B) » | au moins une entrée est à 0 |
| NOR   | `a nor b`     | « non (A ou B) » | les deux entrées sont à 0 |
| XNOR  | `a xnor b`    | « A = B »        | les entrées sont égales |

Et la porte à une entrée, NOT :

```vhdl
y <= not a;
```

## Pourquoi NAND et NOR sont spéciales

Résultat classique de la logique numérique : **le NAND seul** (ou le NOR seul) suffit à construire toutes les autres portes. Les deux sont dites « fonctionnellement complètes » pour exactement cette raison. C'est pourquoi les bibliothèques de procédés CMOS livrent beaucoup de cellules NAND2 et NOR2 — on peut tout synthétiser à partir d'elles.

- NOT : `not a = a nand a`
- AND : `a and b = not (a nand b)`
- OR  : `a or b = (not a) nand (not b)`

## XOR, la plus « mathématique »

XOR se dessine souvent avec un plus encerclé (⊕). C'est la porte qui calcule la « parité » — sa sortie vaut `1` quand un nombre impair d'entrées vaut `1`. Cela en fait le cœur de :

- Les additionneurs : `sum = a xor b xor carry_in`
- Les vérificateurs de parité et codes de détection d'erreur
- Les bascules T : `q_next = q xor toggle_input`

## Les six portes dans un même fichier

Si tu ouvres le labo **truth-table explorer** de ce module, tu trouveras une seule entité VHDL qui calcule les six portes à 2 entrées d'un coup, avec les entrées `a` et `b` reliées à l'onglet Ports pour le stimulus. Le visualiseur de formes d'onde montre alors la réaction de chaque sortie à chaque combinaison.

```vhdl
architecture rtl of gates is
begin
  o_and  <= a and b;
  o_or   <= a or  b;
  o_xor  <= a xor b;
  o_nand <= a nand b;
  o_nor  <= a nor  b;
  o_xnor <= a xnor b;
end architecture rtl;
```

Remarque que chacune de ces six lignes est **un fil**, pas une instruction qui s'exécute. Six portes, six morceaux de matériel — ils s'évaluent tous simultanément.

## À retenir

- Six portes à deux entrées + NOT, c'est l'ensemble complet.
- NAND et NOR sont « universelles » — on peut tout construire à partir d'elles.
- En VHDL, les portes sont des assignations concurrentes d'une ligne. Elles ne sont pas « appelées » ; elles existent.
