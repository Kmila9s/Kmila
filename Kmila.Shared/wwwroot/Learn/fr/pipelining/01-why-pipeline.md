# Pourquoi pipeliner un CPU ?

Ton CPU 8 bits monocycle du parcours précédent fonctionne — mais il est limité sur une dimension qui compte énormément sur du matériel réel : la **fréquence d'horloge**.

## Le plafond du monocycle

Dans un design monocycle, la période d'horloge doit être assez longue pour que l'instruction au chemin critique le plus long se propage entièrement : fetch → decode → lecture-registre → ALU → mémoire → writeback, le tout en combinatoire. Si le chemin le plus long fait 20 ns, ton horloge max est 50 MHz. Point final.

## Pipeliner = diviser ce chemin

Découpe le chemin en N étages séparés par des registres :

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         registres entre les étages
```

L'horloge n'a plus besoin que d'être aussi longue que l'*étage* le plus long, pas tout le chemin. Cinq étages à ~4 ns chacun donnent une horloge à 250 MHz. Le débit passe de 1 instruction / 20 ns à 1 instruction / 4 ns — **accélération 5×** gratuite, conceptuellement.

## Les aléas — le prix à payer

En pratique on n'obtient pas 5×. Trois types de collisions :

- **Aléa de données** — l'instruction N+1 lit un registre que l'instruction N est encore en train d'écrire. Solution : chemins de forwarding ou cycles de stall.
- **Aléa de contrôle** — l'instruction N est un branchement, et on a déjà cherché N+1 avant de savoir s'il fallait l'exécuter. Solution : prédiction de branchement ou flush du pipeline.
- **Aléa structurel** — deux étages ont besoin de la même ressource physique (ex. un port mémoire). Solution : dupliquer la ressource ou sérialiser.

Chaque leçon suivante de ce module implémente une solution à la fois sur le CPU 8 bits du cursus, en commençant par le forwarding.

## À retenir

- Le pipelining échange **latence par instruction** (plus haute) contre **débit** (bien plus haut).
- L'accélération d'horloge est plafonnée par l'étage le plus long, pas par le plus court.
- Les aléas coûtent des cycles ; les contre-mesures coûtent des portes. La conception réelle de CPU, c'est ce compromis à grande échelle.

*D'autres leçons à venir — ce module est en cours d'écriture.*
