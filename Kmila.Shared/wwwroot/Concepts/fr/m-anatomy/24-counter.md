# Compteur

Un **compteur** ajoute un à sa valeur mémorisée à chaque front, parcourant 0, 1, 2, … puis repartant. C'est un registre avec un additionneur +1 rebouclé.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/24-counter.html" title="Bloc séquentiel multi-bits interactif, piloté par l'horloge" loading="lazy"></iframe></div>

Une horloge libre bat à gauche ; l'état 4 bits est montré en rangée de **lampes Q** et en valeur en direct (décimal + binaire). À chaque front l'état se met à jour ; entre les fronts il est conservé.

En VHDL :

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q + 1;   -- increment every edge
  end if;
end process;
```

Les compteurs mesurent le temps et les événements, génèrent des adresses et divisent les horloges. Observe le bit le plus bas basculer à chaque front et chaque bit supérieur à la moitié du rythme — le comptage binaire en mouvement.
