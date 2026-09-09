# Que veut dire « exécuter » ?

Quand tu appuies sur **Run** dans Kmila, le simulateur n'allume pas
vraiment un FPGA. Il parcourt ton VHDL une étape à la fois — mais **la
manière** dont il le parcourt dépend de l'endroit où vit le code.

Il existe trois modèles d'exécution que tout le monde confond en
permanence :

| Modèle | Signification | Où on le voit en VHDL |
|---|---|---|
| **Séquentiel** | Une sentence à la fois, dans l'ordre du code source. | Dans le corps d'un `process`. |
| **Concurrent** | Plusieurs sentences sont évaluées « ensemble » — leur ordre dans le fichier n'a pas d'importance. | Les sentences concurrentes dans une architecture (entre `begin` et `end`). |
| **Parallèle** | Plusieurs choses qui se produisent vraiment au même instant, en temps réel. | **Uniquement sur du matériel réel.** Jamais en simulation. |

L'exemple ci-dessous est volontairement minuscule — un `process` qui lit
deux entrées `a` et `b` et écrit une sortie `sum`. Appuie sur ▶ dans le
lecteur et observe la ligne que le simulateur est en train de « regarder »
à chaque instant.

```text
Séquentiel :   ligne 1  →  ligne 2  →  ligne 3  …
Concurrent :   les lignes 1, 2 et 3 sont évaluées à cet instant même
Parallèle :    n'existe pas en simulation. Seulement sur le FPGA lui-même.
```

## Pourquoi ça compte

Les débutants lisent `sum <= b;` et pensent que la valeur de `sum` devient
`b` **immédiatement**, comme ce serait le cas avec `sum = b;` en C. Ce
n'est pas ce qui se passe. L'affectation **publie une transaction** et la
nouvelle valeur ne devient visible qu'après la fin du cycle delta courant.

Le lecteur à droite a une exécution codée en dur — aucune simulation
réelle ne tourne. Ça garde l'explication honnête : chaque étape porte une
note pédagogique au lieu de te faire deviner ce que pensait le simulateur.

> **Essaie ceci :** avance manuellement et regarde `sum` rester à `U`
> (« non initialisé ») même après l'étape 3 — il ne change qu'au moment
> où le cycle delta se ferme, à l'étape 4.
