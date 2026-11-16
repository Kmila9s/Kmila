**Ton objectif :** avec `a = 3` et `b = 5`, fais en sorte que `result` vaille **16** à la fin du process. Le programme de départ semble juste — `s <= a + b; result <= s * 2` — mais appuie sur **Exécuter** et regarde ce que `result` reçoit vraiment.

`s` est un **signal** (une boîte aux lettres) : sa nouvelle valeur n'arrive qu'à la fin du delta, donc la deuxième ligne lit l'ancienne. Modifie le programme pour que la valeur intermédiaire vive dans la **variable** `tmp` (un carnet), qui se met à jour tout de suite.
