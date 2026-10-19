# Registre à décalage

Un **registre à décalage** déplace ses bits d'un cran à chaque front, un nouveau bit entrant par une extrémité — une chaîne de seaux pour les données.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/23-shift.html" title="Bloc séquentiel multi-bits interactif, piloté par l'horloge" loading="lazy"></iframe></div>

Une horloge libre bat à gauche ; l'état 4 bits est montré en rangée de **lampes Q** et en valeur en direct (décimal + binaire). À chaque front l'état se met à jour ; entre les fronts il est conservé.

En VHDL :

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q(2 downto 0) & si;  -- shift left, SI in
  end if;
end process;
```

Les registres à décalage font la conversion série-parallèle (réception UART), multiplient/divisent par deux et construisent des LFSR pour des séquences pseudo-aléatoires.
