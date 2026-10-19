# FPGA — la puce entière

Un **FPGA** est une mer de ces pièces sur une puce : une grille de **CLB** (cellules logiques), des colonnes de **block RAM** et de **DSP**, un **anneau d'E/S**, un **arbre d'horloge** et le routage qui relie le tout.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/29-fpga.html" title="Figure interactive" loading="lazy"></iframe></div>

Les chemins éclairés sont deux signaux configurés routés à travers le tissu. Tout ce parcours vit ici : portes et additionneurs deviennent des LUT, les registres sont les bascules, et le routeur les câble par les canaux.

C'est tout le parcours sur une puce : l'**horloge** la cadence, **portes** et **additionneurs** deviennent des **LUT** dans des **cellules logiques**, les **bascules** gardent l'état, **mémoire** et **DSP** en colonnes, et l'**interconnexion** câble le tout — reprogrammable, car ce ne sont que des tables de correspondance et des interrupteurs chargés depuis un bitstream.
