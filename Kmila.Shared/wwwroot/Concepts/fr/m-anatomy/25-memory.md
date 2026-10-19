# Mémoire ROM

La **mémoire** stocke des mots que l'on relit par **adresse**. Une ROM a un contenu fixe ; chaque adresse sélectionne un mot, qui apparaît en sortie.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/25-memory.html" title="Figure interactive" loading="lazy"></iframe></div>

Les poussoirs d'adresse choisissent une cellule ; son mot stocké allume les lampes de sortie, et la table montre chaque adresse → donnée.

En VHDL :

```vhdl
type rom_t is array(0 to 3) of unsigned(2 downto 0);
constant ROM : rom_t := ("101","011","110","001");
...
data <= ROM(to_integer(addr));  -- addressed read
```

Lire, c'est du pur adressage — une ROM est littéralement un MUX N-vers-1 par bit de sortie. La RAM ajoute un chemin d'écriture ; la block RAM (BRAM) est une mémoire dédiée dans le FPGA.
