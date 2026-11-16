# Tout mettre ensemble

Les vrais designs utilisent **à la fois** du code concurrent et séquentiel dans la même entité. Le lecteur ci-dessous a une petite entité avec deux morceaux :

1. Un **demi-additionneur** — logique combinatoire pure, écrite comme deux affectations concurrentes. Pas d'horloge, pas de `process`.
2. Un **compteur 4 bits** — logique séquentielle et synchrone, écrite dans un `process` avec `rising_edge(clk)`.

Les deux morceaux cohabitent dans la même `architecture`. Ils utilisent des modèles d'exécution différents parce qu'ils répondent à des questions différentes :

| Question | Modèle | Pourquoi |
|---|---|---|
| Quelle est la somme de `a` et `b` *à cet instant* ? | Concurrent | Pas de mémoire, pas d'horloge — que des portes. |
| Quel est le compteur *après le prochain front d'horloge* ? | Séquentiel | Il faut un registre qui garde l'état entre deux fronts d'horloge. |

<!-- exec-fig2:start -->
## Des tuyaux et un tourniquet

La même architecture contient deux machines qui fonctionnent différemment :

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-turnstile.html" title="Tuyaux du demi-additionneur et un registre tourniquet cadencé" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- **Tuyaux (concurrent) :** quand `a` et `b` changent, l'eau atteint les modules XOR et AND et les lampes `sum` et `cout` se mettent à jour dans le même delta, sans horloge.
- **Tourniquet (séquentiel) :** `count <= count + 1` ne fait que préparer la valeur suivante (la carte `0001` qui attend à l'entrée). Elle ne passe — le tourniquet tourne et le panneau change — qu'au `rising_edge(clk)`.
<!-- exec-fig2:end -->

## Les deux modèles d'exécution, qui travaillent ensemble

Quand tu appuies sur play, tu verras :

1. Les entrées `a` et `b` changent → les deux lignes du demi-additionneur se déclenchent **ensemble** dans un même cycle delta.
2. L'horloge monte → le `process` du compteur se réveille et le garde `if rising_edge` nous laisse atteindre l'incrément.
3. Le `<= count + 1` du compteur est une affectation de signal — elle est validée à la fin du delta.

> **Pourquoi ça compte dans les vrais designs :** chaque CPU que tu as déjà utilisé a été construit comme ça. La logique combinatoire décide « quelle serait la réponse ? », les registres synchrones décident « quand est-ce qu'on la valide ? ». Maîtrise ce partage et tu tiens tout le modèle mental du design numérique.

## Dans le doute : `process` ou concurrent ?

- **Pas d'horloge ? Pas besoin de mémoire ?** Utilise des affectations concurrentes. Elles sont plus faciles à lire, plus faciles à raisonner, et se synthétisent en portes combinatoires.
- **Besoin de mémoriser une valeur entre deux fronts d'horloge ?** Utilise un `process` gardé par `rising_edge(clk)`. C'est comme ça qu'on obtient registres, FSM, compteurs, accumulateurs — tout ce qui garde un état.

Le lecteur est arrivé à sa fin. Tu as maintenant vu le séquentiel, le concurrent, pourquoi le vrai parallélisme ne s'applique pas, les pièges des listes de sensibilité, et les deux modèles qui cohabitent. Marque la leçon comme terminée ci-dessous — tu as le modèle mental.
