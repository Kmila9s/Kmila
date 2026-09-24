# Corrige : le tampon trois-états qui pilote toujours

Un tampon trois-états est une sortie commutable. Quand le signal output-enable (`oe`) est haut, il fait passer ses données vers le bus. Quand `oe` est bas, il **relâche** le bus — il pilote en haute impédance (`'Z'`) pour que d'autres dispositifs puissent parler.

## Ce qui ne va pas

Ouvre l'exemple et regarde le corps :

```vhdl
y <= d when oe = '0' else 'Z';
```

Lis-le à voix haute : « `y` vaut `d` **quand `oe` est à 0**, sinon `'Z'` ». C'est l'inverse. Tu veux que le tampon laisse passer les données quand l'enable est **haut**, et relâche quand il est **bas**.

## Ta mission

- Ouvre l'exemple dans l'éditeur.
- Corrige la condition à la ligne 12 (l'affectation `y <=`).
- Lance la simulation et vérifie dans l'onglet Waveforms :
  - Quand `oe = '0'`, `y` doit valoir `'Z'`.
  - Quand `oe = '1'`, `y` doit suivre `d`.

## Pourquoi le trois-états compte

Les tampons trois-états sont la façon dont plusieurs drivers partagent un fil unique (un bus) sans se court-circuiter. Les FPGAs n'ont pas de trois-états dans leur tissu interne — ils utilisent des multiplexeurs à la place — mais les trois-états **externes** (aux pads I/O) restent la manière dont les mémoires DDR et les endpoints PCIe partagent les broches.

## Bonus : surveille les `'X'`

Si deux tampons trois-états pilotent le même bus en même temps avec des valeurs différentes, un vrai circuit fumerait. Le simulateur affiche ça en `'X'` (inconnu) sur le chronogramme. Essaie de bâtir un petit banc de test avec deux de tes tampons câblés sur le même `y` et regarde ce qui se passe.
