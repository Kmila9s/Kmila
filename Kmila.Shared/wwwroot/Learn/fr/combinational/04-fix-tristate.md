# À corriger : le buffer tristate qui drive tout le temps

Un buffer tristate est une sortie commutable. Quand le signal output-enable (`oe`) est haut, il laisse passer ses données vers le bus. Quand `oe` est bas, il **relâche** le bus — il pilote en haute impédance (`'Z'`) pour que d'autres composants puissent parler.

## Ce qui cloche

Ouvre l'exemple et regarde le corps :

```vhdl
y <= d when oe = '0' else 'Z';
```

Lis-le à voix haute : « `y` vaut `d` **quand `oe` vaut 0**, sinon `'Z'`. » C'est à l'envers. Tu veux que le buffer laisse passer les données quand l'enable est **haut**, et qu'il relâche quand il est **bas**.

## Ta mission

- Ouvre l'exemple dans l'éditeur.
- Corrige la condition sur la ligne 12 (l'assignation `y <=`).
- Lance la simulation et vérifie dans l'onglet Waveforms :
  - Quand `oe = '0'`, `y` doit être `'Z'`.
  - Quand `oe = '1'`, `y` doit suivre `d`.

## Pourquoi le tristate importe

Les buffers tristate sont la manière dont plusieurs drivers partagent un même fil (un bus) sans se court-circuiter entre eux. Les FPGA n'ont pas de tristates dans leur fabric interne — ils utilisent des multiplexeurs à la place — mais les tristates **externes** (sur les pads d'E/S) sont encore la façon dont la DDR et les endpoints PCIe partagent les broches.

## Extra : attention au `'X'`

Si deux buffers tristate pilotent le même bus en même temps avec des valeurs différentes, un vrai circuit partirait en fumée. Le simulateur montre cela comme `'X'` (inconnu) sur la forme d'onde. Essaie de construire un petit testbench avec deux de tes buffers câblés sur le même `y` et observe ce qui se passe.
