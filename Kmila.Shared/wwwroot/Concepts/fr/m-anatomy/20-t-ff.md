# Bascule T

Une **bascule T bascule** : à chaque front, si T = 1 le bit mémorisé s'inverse (0↔1) ; si T = 0 il est conservé. Elle divise la fréquence d'horloge par deux. C'est le premier élément **séquentiel** du parcours : contrairement à une porte, sa sortie dépend de l'horloge et de son propre passé, pas seulement des entrées actuelles.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/20-t-ff.html" title="Élément séquentiel interactif avec chronogramme en direct" loading="lazy"></iframe></div>

Cet élément a de la **mémoire**. Observe le chronogramme : l'horloge (**CLK**) bat en haut, et la sortie **Q** ne change qu'à l'horloge — entre-temps elle **conserve** sa valeur quoi que fassent les entrées. Q est tracée en couleur ; les pointillés marquent les fronts d'horloge.

En VHDL :

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if t = '1' then q <= not q; end if;
  end if;
end process;
```

Chaîne des bascules T et chacune redivise par deux — c'est exactement un **compteur à propagation** binaire. Une T est une D dont D est reliée à Q xor T.
