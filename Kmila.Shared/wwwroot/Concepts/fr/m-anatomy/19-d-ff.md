# Bascule D

Une **bascule D** mémorise un bit, capturé au **front montant** de l'horloge. Q prend la valeur de D à l'instant du front et la conserve jusqu'au suivant. C'est le premier élément **séquentiel** du parcours : contrairement à une porte, sa sortie dépend de l'horloge et de son propre passé, pas seulement des entrées actuelles.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/19-d-ff.html" title="Élément séquentiel interactif avec chronogramme en direct" loading="lazy"></iframe></div>

Cet élément a de la **mémoire**. Observe le chronogramme : l'horloge (**CLK**) bat en haut, et la sortie **Q** ne change qu'à l'horloge — entre-temps elle **conserve** sa valeur quoi que fassent les entrées. Q est tracée en couleur ; les pointillés marquent les fronts d'horloge.

En VHDL :

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= d;          -- sampled on the edge
  end if;
end process;
```

La bascule D est **la** brique du design synchrone — chaque registre, étage de pipeline et bit d'état est une ou plusieurs bascules D. Place un MUX devant D et tu obtiens une validation de chargement ou un reset synchrone.
