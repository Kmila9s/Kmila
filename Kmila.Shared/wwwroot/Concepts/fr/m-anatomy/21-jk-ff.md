# Bascule JK

Une **bascule JK** est la plus générale : à chaque front J met Q à 1, K la remet à 0, les deux ensemble la font **basculer**, et aucune la conserve. C'est le premier élément **séquentiel** du parcours : contrairement à une porte, sa sortie dépend de l'horloge et de son propre passé, pas seulement des entrées actuelles.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/21-jk-ff.html" title="Élément séquentiel interactif avec chronogramme en direct" loading="lazy"></iframe></div>

Cet élément a de la **mémoire**. Observe le chronogramme : l'horloge (**CLK**) bat en haut, et la sortie **Q** ne change qu'à l'horloge — entre-temps elle **conserve** sa valeur quoi que fassent les entrées. Q est tracée en couleur ; les pointillés marquent les fronts d'horloge.

En VHDL :

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if    j='1' and k='1' then q <= not q;
    elsif j='1' then q <= '1';
    elsif k='1' then q <= '0';
    end if;
  end if;
end process;
```

La JK supprime l'état interdit de l'ancien verrou SR. Toute bascule peut se construire à partir d'une autre — D, T et JK sont interchangeables avec un peu de logique devant.
