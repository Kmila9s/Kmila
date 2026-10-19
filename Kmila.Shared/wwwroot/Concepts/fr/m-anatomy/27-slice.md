# Cellule logique (slice)

Une **cellule logique** (ou *slice*) est la brique qui se répète dans le FPGA : une **LUT** + une **bascule** + un **MUX** de sortie (plus une chaîne de retenue). Elle donne une sortie combinatoire ou enregistrée.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/27-slice.html" title="Figure interactive" loading="lazy"></iframe></div>

Les entrées alimentent la LUT ; son résultat sort directement (**combinatoire**) ou passe par la bascule (**enregistré**) ; le MUX choisit. L'horloge pilote la bascule ; une chaîne de retenue traverse la tuile.

Mets `y <= a and b` et un registre `rising_edge(clk)` ensemble et ils tiennent dans un slice — la LUT pour la logique, la bascule pour l'état. Les slices modernes tiennent plusieurs LUT et bascules.
