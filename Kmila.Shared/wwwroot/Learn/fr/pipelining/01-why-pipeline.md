# Pourquoi pipeliner un CPU ?

Ton CPU 8 bits single-cycle du parcours précédent fonctionne — mais il est limité dans une dimension qui compte énormément sur du vrai matériel : la **fréquence d'horloge**.

## Le plafond du single-cycle

Dans un design single-cycle, la période d'horloge doit être assez longue pour que l'instruction au plus long chemin critique se propage intégralement : fetch → decode → lecture registres → ALU → mémoire → writeback, tout en combinatoire. Si le chemin le plus long fait 20 ns, ton horloge maximale est 50 MHz. Point.

## Pipeliner, c'est découper ce chemin

Découpe le chemin en N étages séparés par des registres :

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         registres entre étages
```

L'horloge n'a plus qu'à être assez longue pour que l'*étage* le plus long se termine, pas tout le chemin. Cinq étages, chacun à ~4 ns, donne une horloge à 250 MHz. Le débit passe de 1 instruction / 20 ns à 1 instruction / 4 ns — **×5 de speedup** gratuit, en théorie.

## Aléas — le coût

En pratique, tu n'obtiens pas ×5. Trois types de collisions :

- **Aléa de données** — l'instruction N+1 lit un registre que l'instruction N est en train d'écrire. Correction : chemins de forwarding ou cycles de stall.
- **Aléa de contrôle** — l'instruction N est un branchement, et on a déjà fetché N+1 avant de savoir s'il fallait l'exécuter. Correction : prédiction de branchement ou flush du pipeline.
- **Aléa structurel** — deux étages ont besoin de la même ressource physique (par ex. un port mémoire). Correction : dupliquer la ressource ou sérialiser.

Chaque leçon suivante de ce module implémente un correctif à la fois sur le CPU 8 bits du programme, en commençant par le forwarding.

## À retenir

- Le pipelining échange **latence par instruction** (plus élevée) contre **débit** (bien plus élevé).
- L'accélération d'horloge est bornée par l'étage le plus long, pas le plus court.
- Les aléas coûtent des cycles ; les mitigations coûtent des portes. Le vrai design de CPU, c'est cet arbitrage à grande échelle.

*D'autres leçons à venir — ce module est en cours de rédaction active.*
