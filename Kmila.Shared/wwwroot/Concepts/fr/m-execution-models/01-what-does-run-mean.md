# Que veut dire « exécuter » ?

Quand tu appuies sur **Run** dans Kmila, le simulateur n'allume pas vraiment un FPGA. Il parcourt ton VHDL une étape à la fois — mais **la manière** dont il le parcourt dépend de l'endroit où vit le code.

Il existe trois modèles d'exécution que tout le monde confond en permanence :

| Modèle | Signification | Où on le voit en VHDL |
|---|---|---|
| **Séquentiel** | Une sentence à la fois, dans l'ordre du code source. | Dans le corps d'un `process`. |
| **Concurrent** | Plusieurs sentences sont évaluées « ensemble » — leur ordre dans le fichier n'a pas d'importance. | Les sentences concurrentes dans une architecture (entre `begin` et `end`). |
| **Parallèle** | Plusieurs choses qui se produisent vraiment au même instant, en temps réel. | **Uniquement sur du matériel réel.** Jamais en simulation. |

L'exemple ci-dessous est volontairement minuscule — un `process` qui lit deux entrées `a` et `b` et écrit une sortie `sum`. Appuie sur ▶ dans le lecteur et observe la ligne que le simulateur est en train de « regarder » à chaque instant.

```text
Séquentiel :   ligne 1  →  ligne 2  →  ligne 3  …
Concurrent :   les lignes 1, 2 et 3 sont évaluées à cet instant même
Parallèle :    n'existe pas en simulation. Seulement sur le FPGA lui-même.
```

<!-- exec-fig:start -->
## L'analogie du restaurant

Imagine un restaurant de **10 tables**. La **serveuse**, c'est ce qui fait le travail (un CPU, ou le simulateur de Kmila). Chaque **table** est un travail qui demande de l'attention, et chaque **plat** met un moment à cuire. Utilise les boutons au-dessus du dessin pour changer la façon dont le restaurant tourne :

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=seq,poll,par" title="Restaurant : une serveuse, une serveuse qui jongle entre les tables, cinq serveuses" loading="lazy"></iframe></div>

- **1 →** — *séquentiel* : une serveuse s'occupe d'une table à la fois. Elle prend la commande et **reste plantée là jusqu'à ce que le plat arrive**, même si neuf autres tables attendent.
- **1 ↻** — *concurrent* : toujours une seule serveuse, mais si le plat n'est pas prêt elle **passe à la suivante** : prend la commande suivante, revient vérifier la première, sert ce qui est prêt.
- **5 ‖** — *parallèle* : cinq serveuses, deux tables chacune, qui travaillent **au même instant**.

Lire le graphique : une ligne par table ; **barre bleue** = la serveuse est à cette table, **ligne rouge** = le plat cuit, **pointillé** = le plat est prêt mais pas encore servi, **point vert** = servi. L'affichage indique <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><circle cx="8" cy="9" r="5.5"/><path d="M8 9V6.2M6.5 1.8H9.5M8 1.8V3.5"/></svg> le temps écoulé, <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M4.5 2H11.5M4.5 14H11.5M5.2 2C5.2 5.5 10.8 6.8 10.8 14M10.8 2C10.8 5.5 5.2 6.8 5.2 14"/></svg> le temps où la serveuse attend sans rien faire, ↻ les vérifications « c'est prêt ? » et ✓ les tables servies. À la fin de chaque mode, une ligne d'arrivée en pointillé reste sur le graphique pour comparer.

| Restaurant | Modèle d'exécution |
|---|---|
| Serveuse | Ce qui exécute : un thread du CPU, ou le simulateur |
| Table | Un `process` (ou une instruction concurrente) |
| Plat qui cuit | Le temps qui passe (`after 10 ns`, attendre un front d'horloge…) |
| Plat prêt | Un **événement** : ce que le process attendait s'est produit |
| Cinq serveuses | Du vrai matériel : un FPGA, où chaque morceau du circuit est sa propre « serveuse » |

> **Remarque :** la serveuse concurrente ne marche pas plus vite ; elle **n'attend jamais les bras croisés**. Le séquentiel prend environ 70 s, le concurrent environ 37 s, le parallèle environ 12 s.
<!-- exec-fig:end -->

## Pourquoi ça compte

Les débutants lisent `sum <= b;` et pensent que la valeur de `sum` devient `b` **immédiatement**, comme ce serait le cas avec `sum = b;` en C. Ce n'est pas ce qui se passe. L'affectation **publie une transaction** et la nouvelle valeur ne devient visible qu'après la fin du cycle delta courant.

Le lecteur à droite a une exécution codée en dur — aucune simulation réelle ne tourne. Ça garde l'explication honnête : chaque étape porte une note pédagogique au lieu de te faire deviner ce que pensait le simulateur.

> **Essaie ceci :** avance manuellement et regarde `sum` rester à `U` (« non initialisé ») même après l'étape 3 — il ne change qu'au moment où le cycle delta se ferme, à l'étape 4.
