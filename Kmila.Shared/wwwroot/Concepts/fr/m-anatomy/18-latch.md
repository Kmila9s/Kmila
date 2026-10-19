# Verrou D

Un **verrou D** mémorise un bit. Tant que sa validation (ici l'horloge) est haute il est **transparent** — Q suit D ; quand elle redescend, il **fige** la dernière valeur. C'est le premier élément **séquentiel** du parcours : contrairement à une porte, sa sortie dépend de l'horloge et de son propre passé, pas seulement des entrées actuelles.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/18-latch.html" title="Élément séquentiel interactif avec chronogramme en direct" loading="lazy"></iframe></div>

Cet élément a de la **mémoire**. Observe le chronogramme : l'horloge (**CLK**) bat en haut, et la sortie **Q** ne change qu'à l'horloge — entre-temps elle **conserve** sa valeur quoi que fassent les entrées. Q est tracée en couleur ; les pointillés marquent les fronts d'horloge.

En VHDL :

```vhdl
process (en, d)
begin
  if en = '1' then   -- transparent while enabled
    q <= d;
  end if;
end process;
```

Étant **sensible au niveau**, un verrou est transparent pendant toute une phase d'horloge — tout glitch sur D dans cette fenêtre passe. D'où la préférence du design synchrone pour les bascules sur front.
