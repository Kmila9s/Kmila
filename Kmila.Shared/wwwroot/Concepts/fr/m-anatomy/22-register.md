# Registre

Un **registre** est un banc de bascules qui mémorise un mot entier d'un coup. Au front d'horloge (au chargement) tous les bits de D sont capturés ensemble dans Q.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/22-register.html" title="Bloc séquentiel multi-bits interactif, piloté par l'horloge" loading="lazy"></iframe></div>

Une horloge libre bat à gauche ; l'état 4 bits est montré en rangée de **lampes Q** et en valeur en direct (décimal + binaire). À chaque front l'état se met à jour ; entre les fronts il est conservé.

En VHDL :

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if ld = '1' then q <= d; end if;  -- load on edge
  end if;
end process;
```

Dans les registres un processeur garde ses valeurs de travail, et chaque étage de pipeline est un registre. Mets un MUX devant (nouvelle valeur vs. Q actuel) et la validation de chargement devient un maintien.
