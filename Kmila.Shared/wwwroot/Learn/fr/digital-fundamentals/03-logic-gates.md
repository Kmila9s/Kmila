# Les sept portes logiques

Tout circuit numérique, jusqu'à un GPU moderne, est une combinaison de sept cellules élémentaires. Si tu sais comment chacune se comporte et comment l'écrire en VHDL, tu as déjà commencé le design matériel.

## Les six portes à deux entrées

| Porte | Symbole en VHDL | Se lit | Sortie à `1` quand… |
|-------|-----------------|--------|---------------------|
| AND   | `a and b`       | « A et B »   | les deux entrées sont à 1 |
| OR    | `a or b`        | « A ou B »   | au moins une entrée est à 1 |
| XOR   | `a xor b`       | « A xor B »  | les entrées diffèrent |
| NAND  | `a nand b`      | « non (A et B) » | au moins une entrée est à 0 |
| NOR   | `a nor b`       | « non (A ou B) »  | les deux entrées sont à 0 |
| XNOR  | `a xnor b`      | « A = B »    | les entrées sont égales |

Et le NOT à une entrée :

```vhdl
y <= not a;
```

## Pourquoi NAND et NOR sont spéciaux

Résultat classique de logique numérique : **NAND seule** (ou NOR seule) suffit à bâtir toutes les autres portes. Toutes deux sont appelées « fonctionnellement complètes » exactement pour cette raison. C'est pourquoi les bibliothèques de procédé CMOS livrent des tonnes de cellules NAND2 et NOR2 — tu peux tout synthétiser à partir d'elles.

- NOT : `not a = a nand a`
- AND : `a and b = not (a nand b)`
- OR  : `a or b = (not a) nand (not b)`

## XOR est la matheuse

XOR est souvent dessinée avec un plus dans un cercle (⊕). C'est la porte qui calcule la « parité » — sa sortie vaut `1` quand un nombre impair d'entrées vaut `1`. Ça la met au cœur de :

- Additionneurs : `sum = a xor b xor carry_in`
- Vérificateurs de parité et codes de détection d'erreur
- Bascules toggle : `q_next = q xor toggle_input`

## Les six dans un seul fichier

Si tu ouvres le lab **explorateur de table de vérité** de ce module, tu trouveras une seule entity VHDL qui calcule simultanément les six portes 2 entrées, avec les entrées `a` et `b` câblées à l'onglet ports pour le stimulus. Le visualiseur de chronogrammes montre alors la réaction de chaque sortie à chaque combinaison.

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

Remarque : chacune de ces six lignes est **un fil**, pas une instruction qui s'exécute. Six portes, six morceaux de matériel — ils évaluent tous simultanément.

## À retenir

- Six portes à deux entrées + NOT, c'est tout l'ensemble.
- NAND et NOR sont « universelles » — tu peux tout construire à partir d'elles.
- En VHDL, les portes sont des affectations concurrentes d'une ligne. Elles ne sont pas « appelées » ; elles existent.
