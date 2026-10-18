# L'horloge : le battement du circuit

L'**horloge** (*clock*) est le signal qui cadence tout : un **signal carré** qui alterne entre 0 et 1 à **fréquence fixe**. Avant les portes et les bascules, il est utile de la voir bouger.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/01-reloj.html" title="Horloge interactive — une barre dentée traçant un signal carré" loading="lazy"></iframe></div>

**Déplace le curseur** à gauche et à droite pour changer la fréquence. La barre dentée est le signal rendu physique : le palpeur monte sur chaque dent (niveau **1**) et retombe dans chaque creux (niveau **0**), tandis que le volant impose une vitesse constante. L'afficheur montre le niveau et la fréquence actuels.

## Ce qui compte vraiment : le front

La logique séquentielle — bascules, registres, compteurs — ne réagit pas au *niveau* de l'horloge mais à son **front** : l'instant du saut de 0 à 1 (**front montant**) ou de 1 à 0 (**front descendant**). En VHDL :

```vhdl
process (clk)
begin
    if rising_edge(clk) then
        q <= d;          -- capturé exactement sur le front montant
    end if;
end process;
```

- **Fréquence** — cycles par seconde (Hz). Volant plus rapide, signal plus serré.
- **Période** — un cycle complet : une dent plus un creux.
- **Rapport cyclique** — la fraction de la période à 1 ; ici 50 %.
