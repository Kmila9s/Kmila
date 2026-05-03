## Additionneur en largeurs mixtes — bug d'extension de signe

`a` est un nombre en complément à deux sur 4 bits (plage −8…+7) et `b` est sur 8 bits, en complément à deux. La sortie `sum = a + b` fait 8 bits.

### Spécification

* Étends `a` de 4 à 8 bits par **extension de signe**, puis additionne avec `b`.
* Recopie le bit de signe `a(3)` dans les quatre bits hauts.

### Ce qui cloche

L'auteur étend `a` en préfixant `"0000"`, ce qui est une **extension par zéros**. Ça marche par hasard pour les `a` positifs, mais pour `a` négatif (p. ex. `1111` = −1) on obtient le positif 15 et la somme est fausse.

### Indice

`numeric_std` fournit `resize(valeur_signed, nouvelle_largeur)`, qui réalise l'extension de signe automatiquement quand la source est `signed`.
