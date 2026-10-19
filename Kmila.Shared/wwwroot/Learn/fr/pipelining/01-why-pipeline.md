# Pourquoi pipeliner un CPU ?

Votre CPU 8 bits monocycle du parcours précédent fonctionne — mais il est limité sur une dimension qui compte énormément sur du matériel réel : la **fréquence d'horloge**.

## Le plafond du monocycle

Dans un design monocycle, la période d'horloge doit être assez longue pour que l'instruction au chemin critique le plus long se propage entièrement : fetch → decode → lecture-registre → ALU → mémoire → writeback, le tout en combinatoire. Si le chemin le plus long fait 20 ns, votre horloge maximale est 50 MHz. Point final.

## Pipeliner, c'est diviser ce chemin

Découpez le chemin en N étages séparés par des registres :

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         registres entre les étages
```

L'horloge n'a plus besoin que d'être assez longue pour que l'*étage* le plus long se termine, pas tout le chemin. Cinq étages, à ~4 ns chacun, donnent une horloge à 250 MHz. Le débit passe de 1 instruction / 20 ns à 1 instruction / 4 ns — **accélération 5×** gratuite, conceptuellement.

Remarquez ce que le pipelining n'améliore *pas* : la latence d'une instruction isolée. Une instruction prend toujours cinq étages pour se terminer — environ 20 ns, comme avant (un chouïa plus en fait, à cause du temps d'établissement du registre à chaque frontière). Ce qui change, c'est qu'une *nouvelle* instruction se termine à chaque cycle une fois le pipe plein. Vous ne rendez pas une voiture plus rapide ; vous transformez un ouvrier en chaîne de montage.

## Les registres inter-étages

Les registres entre les étages ne sont pas de la paperasse — ils *sont* le pipeline. Chacun latche tout ce dont les étages en aval ont encore besoin : le résultat partiel, le numéro du registre de destination, les signaux de contrôle décodés en arrière en ID. Sans eux, l'étage N+1 verrait les entrées de l'étage N changer en plein calcul. Ce sont les « registros inter etapa » que le programme ESCOM (unité IV) vous demande d'implémenter en HDL.

## Pourquoi vous n'obtenez jamais vraiment 5×

L'idéal est **CPI = 1** (un cycle par instruction) une fois le pipe plein. Deux choses l'érodent :

1. **Remplissage et vidage.** La première instruction a quand même besoin des cinq étages avant que quoi que ce soit ne sorte, donc un programme court paie un coût de démarrage. Pour un long run c'est négligeable ; pour une boucle serrée, non.
2. **Les aléas** — la vraie taxe, et le sujet du reste de ce module.

## Les aléas — le coût

Trois sortes de collisions, chacune avec son remède :

**Aléa structurel** — deux étages veulent la même ressource physique au même cycle. Le cas classique : IF veut lire une instruction pendant que MEM veut lire des données, mais il n'y a qu'un port mémoire. Remède : séparer les mémoires d'instructions et de données (un découpage Harvard), ou dupliquer le port.

**Aléa de données** — l'instruction N+1 a besoin d'une valeur que l'instruction N n'a pas encore écrite :

```
  ADD R1, R2, R3   -- résultat prêt à la fin d'EX, écrit en WB
  SUB R4, R1, R5   -- a besoin de R1 dans son propre EX, un cycle plus tard
```

Sans aide, SUB lit l'*ancien* R1. Le remède est le **forwarding** (aussi appelé bypassing) : router le résultat de l'ALU directement de la sortie EX/MEM de N vers l'entrée ALU de N+1, au lieu d'attendre qu'il rampe à travers WB puis ressorte du register file. Le forwarding tue la plupart des stalls de données — mais pas tous. L'**aléa load-use** est le têtu : la valeur d'un load n'est disponible qu'à la fin de MEM, un étage *après* EX, donc une instruction qui l'utilise immédiatement doit quand même **staller un cycle** (insérer une bulle) quelle que soit la qualité de votre forwarding.

**Aléa de contrôle** — l'instruction N est un branchement, et le pipeline a déjà récupéré N+1 (et peut-être N+2) avant de savoir si le branchement est pris. Remèdes :

- **Stall / flush** — geler le fetch jusqu'à ce que le branchement soit résolu, ou laisser les instructions du mauvais chemin démarrer et les écraser si le pari était faux. Résoudre le branchement plus tôt (en ID au lieu d'EX) réduit la pénalité à une seule bulle.
- **Prédiction statique** — toujours parier dans un sens à la compilation (par exemple « branchements arrière pris » attrape bien les boucles).
- **Prédiction dynamique** — apprendre à l'exécution avec une table de **compteurs saturants 2 bits** indexée par l'adresse du branchement. Le compteur a besoin de *deux* mauvais paris d'affilée avant de retourner sa prédiction, donc une boucle prise 99 fois puis quittée une fois ne change pas d'avis immédiatement. On prédit « pris » quand le compteur est dans ses deux états hauts, « non pris » dans ses deux états bas ; on le pousse vers pris à chaque branchement pris et vers non-pris sinon. Pas cher, et presque aussi bon que des schémas bien plus élaborés.

Chaque leçon suivante de ce module implémente un remède à la fois sur le CPU 8 bits du cursus, en commençant par le forwarding — ce qui suit la progression ESCOM estructural → datos → bypassing/forwarding → riesgo de control.

## À retenir

- Le pipelining échange la **latence par instruction** (inchangée, voire légèrement plus haute) contre le **débit** (bien plus haut) : CPI idéal → 1.
- L'accélération d'horloge est bornée par l'*étage* le plus long, pas par tout le chemin ni par l'étage le plus court.
- Les registres inter-étages transportent les données *et* le contrôle vers l'avant ; c'est eux qui rendent le recouvrement sûr.
- Le forwarding efface la plupart des aléas de données, mais une paire load-use coûte quand même un stall. Les branchements coûtent un flush sauf si vous les prédisez — un compteur saturant 2 bits est le prédicteur de base.
- Les aléas coûtent des cycles ; les contre-mesures coûtent des portes. La vraie conception de CPU, c'est ce compromis à grande échelle.
