# Table de correspondance (LUT)

Une **table de correspondance (LUT)** est la façon dont un FPGA devient n'importe quelle porte : une petite mémoire contenant une table de vérité. Les entrées forment une **adresse** ; la sortie est le bit stocké à cette adresse.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/26-lut.html" title="Figure interactive" loading="lazy"></iframe></div>

Les entrées adressent les 4 cellules de configuration (la table de vérité chargée) ; la sortie est le bit sélectionné. Regarde la configuration se **reprogrammer** — la même LUT devient ET, puis OU, puis XOR. C'est de la logique reconfigurable.

En VHDL :

```vhdl
y <= a and b;   -- the synthesizer loads this
                -- truth table into ONE LUT
```

Tout ton design est mappé en LUT : le synthétiseur transforme chaque expression booléenne en tables de vérité et les charge à la mise sous tension depuis le bitstream de configuration. Une LUT à N entrées peut être **n'importe quelle** fonction de N entrées.
