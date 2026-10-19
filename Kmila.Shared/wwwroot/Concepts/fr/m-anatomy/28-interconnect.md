# Interconnexion et E/S

L'**interconnexion** est le câblage programmable entre cellules : **canaux de routage**, **boîtes de commutation** aux croisements qui les relient, et **blocs d'E/S (IOB)** au bord qui atteignent les broches.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/28-interconnect.html" title="Figure interactive" loading="lazy"></iframe></div>

Un signal se route depuis un bloc d'E/S, le long des canaux et à travers les boîtes de commutation, jusqu'à un CLB — le chemin éclairé. Programmer les boîtes, c'est ce qui connecte ton design.

L'essentiel de la surface et du délai d'un FPGA, c'est en fait le **routage**, pas la logique. Le place-and-route décide quelles cellules utiliser et comment les câbler — l'étape lente et astucieuse après la synthèse.
