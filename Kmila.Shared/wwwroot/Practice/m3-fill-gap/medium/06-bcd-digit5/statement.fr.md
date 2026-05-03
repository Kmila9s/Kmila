## BCD vers 7 segments — complète le chiffre 5

Un décodeur BCD vers 7 segments est **presque** complet : les chiffres
0-4 et 6-9 sont déjà câblés dans un `case`. Seule l'entrée pour le
chiffre 5 manque.

Tu dois écrire la ligne qui associe `digit = "0101"` à son motif 7
segments. L'ordre est `seg(6..0) = a..g`, **actif haut** (segment
allumé = `'1'`).

Pour le chiffre 5, les segments allumés sont `a, c, d, f, g` (`b` et
`e` éteints), ce qui donne :

```
seg = "1011011"
```

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Les autres
branches du `case` sont verrouillées.

### Astuce

Une seule ligne : `when "0101" => seg <= "1011011";`.
