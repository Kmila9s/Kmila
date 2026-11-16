# Séquentiel : une étape à la fois

Dans le corps d'un `process`, VHDL se comporte à peu près comme les langages que tu connais déjà : les sentences s'exécutent **de haut en bas, dans l'ordre du code source**. Déplace une ligne et tu changes le résultat.

Mais il y a un piège qui surprend tout le monde : les **variables** et les **signals** ne se comportent pas de la même façon.

| Type | Syntaxe | Quand la nouvelle valeur apparaît-elle ? |
|---|---|---|
| **Variable** | `tmp := a + 1` | **Immédiatement.** La sentence suivante lit le nouveau `tmp`. |
| **Signal** | `result <= tmp + b` | **À la fin du cycle delta.** Jusque-là, les lectures voient encore l'ancienne valeur. |

Le lecteur parcourt un tout petit `process` qui utilise les deux. Remarque comme `tmp` se met à jour à l'instant où on exécute la ligne 5, alors que `result` ne change pas avant que le `process` ne se termine.

<!-- exec-fig2:start -->
## Le carnet et la boîte aux lettres

Une **variable**, c'est comme un carnet dans la poche de l'employé : quand la ligne 4 ou 5 s'exécute, le nouveau nombre y est noté tout de suite, et la ligne suivante le lit déjà. Un **signal**, c'est comme une lettre : `result <= tmp + b` dépose la lettre (13) dans le panier de départ, mais `result` affiche toujours U jusqu'au passage du **camion postal** — la fin du cycle delta, marquée δ — qui la livre.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-mailbox.html" title="Bureau avec un carnet (variable) et un panier de départ, un camion postal et une boîte aux lettres (signal)" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

La figure joue les mêmes étapes que le lecteur ci-dessous. Clique sur les points sous le code pour sauter à une étape, ou sur le dessin pour mettre en pause.
<!-- exec-fig2:end -->

## Pourquoi ça compte

Imagine que tu écrives :

```vhdl
result <= 1;
result <= 2;
```

En C, après ces deux lignes, `result` vaudrait clairement `2`. En VHDL ? **La dernière affectation gagne** — mais seulement quand le delta se ferme. Pendant que le `process` tourne, `result` a encore son ancienne valeur, et le simulateur écarte silencieusement la transaction `<= 1` parce que `<= 2` la remplace avant la fin du delta.

> **Essaie ceci :** avance jusqu'à la ligne 7 et regarde la table des signaux. `tmp` vaut `8`, mais `result` est encore `U`. Avance d'une étape de plus et tu verras `result` sauter à `13` *après* que le corps du `process` a fini.
