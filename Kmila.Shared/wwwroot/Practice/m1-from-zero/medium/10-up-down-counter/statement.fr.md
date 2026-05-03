## Compteur 4 bits haut/bas

Implémente un compteur 4 bits avec un signal de direction. Au front montant :

- si `dir = '1'`, incrémente : `count <= count + 1`
- si `dir = '0'`, décrémente : `count <= count - 1`

Un `reset` asynchrone actif haut remet le compteur à `0000`.

### Indice

Utilise `IEEE.NUMERIC_STD.ALL` et un signal interne `unsigned(3 downto 0)`. Le comptage boucle modulo 16.
