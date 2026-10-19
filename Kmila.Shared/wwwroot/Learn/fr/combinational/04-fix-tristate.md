# Corrigez : le tampon trois-états qui pilote toujours

Un tampon trois-états est une sortie commutable. Quand le signal output-enable (`oe`) est haut, il fait passer ses données vers le bus. Quand `oe` est bas, il **relâche** le bus — il pilote la haute impédance (`'Z'`) pour que d'autres dispositifs puissent parler. C'est le seul endroit de cette unité où la valeur `'Z'` de la logique à neuf valeurs gagne son salaire.

## Ce qui ne va pas

Ouvrez l'exemple et regardez le corps :

```vhdl
y <= d when oe = '0' else 'Z';
```

Lisez-le à voix haute : « `y` vaut `d` **quand `oe` est à 0**, sinon `'Z'` ». C'est à l'envers. Vous voulez que le tampon laisse passer les données quand l'enable est **haut**, et qu'il relâche quand il est **bas**.

## Votre mission

- Ouvrez l'exemple dans l'éditeur.
- Corrigez la condition sur l'affectation `y <=` pour que le sens de `oe` soit correct :

```vhdl
y <= d when oe = '1' else 'Z';
```

- Lancez la simulation et vérifiez dans l'onglet Formes d'onde :
  - Quand `oe = '0'`, `y` doit valoir `'Z'`.
  - Quand `oe = '1'`, `y` doit suivre `d`.

## Pourquoi `'Z'` est spécial ici

`'Z'` n'est pas un troisième *niveau* logique — ça veut dire « je ne pilote pas ». Sur le chronogramme, GTKWave l'affiche comme un trait garé au milieu, ni haut ni bas. Tout l'intérêt d'un trois-états est de permettre à un fil d'avoir **plusieurs** drivers potentiels tandis qu'un seul est actif à la fois. Ce n'est sûr que parce que `std_logic` est un type *résolu* : quand un driver est à `'Z'` et qu'un autre pilote `'0'` ou `'1'`, la résolution laisse la valeur forte gagner. Si chaque driver est à `'Z'`, le fil flotte.

## En plus : surveillez les `'X'`

Si deux tampons trois-états pilotent le même bus en même temps avec des valeurs **différentes**, un vrai circuit se combattrait lui-même et surchaufferait. La fonction de résolution modélise ça par `'X'` (inconnu fort) sur le chronogramme — un signal d'alarme clair indiquant que vos enables se chevauchent. Construisez un petit banc de test avec deux de vos tampons câblés sur le même `y`, activez les deux lignes `oe` avec des `d` différents, et regardez le bus passer à `'X'`.

## Pourquoi le trois-états compte (et où il vit aujourd'hui)

Les tampons trois-états sont la façon dont plusieurs drivers partagent un fil unique (un bus) sans se court-circuiter mutuellement. Une mise en garde importante pour le matériel que vous ciblerez réellement : **les FPGA n'ont pas de tampons trois-états internes** dans leur tissu logique — la synthèse remplace automatiquement un bus trois-états interne par des multiplexeurs. Les trois-états ne survivent qu'aux **pads d'E/S**, où ils sont exactement la façon dont fonctionnent les broches bidirectionnelles : les lignes de données des mémoires DDR, le `sda` de l'I²C, et d'autres bus partagés activent et désactivent tous le driver de la broche avec un `oe`. Ce pad bidirectionnel se modélise avec un port `inout` :

```vhdl
io_pin <= drive_value when oe = '1' else 'Z';  -- pilote ou relâche le pad
read_value <= io_pin;                           -- échantillonne toujours ce qui est présent
```

## À retenir

- Un trois-états laisse passer les données quand il est activé et pilote `'Z'` (relâché) sinon — ayez le bon sens de `oe`.
- `'Z'` veut dire « je ne pilote pas », ce qui permet à plusieurs drivers de partager un même fil `std_logic` résolu.
- Deux drivers actifs avec des valeurs différentes se résolvent en `'X'` — un bug que vous pouvez voir sur le chronogramme.
- À l'intérieur d'un FPGA, les trois-états deviennent des muxes ; les vrais trois-états vivent aux pads d'E/S pour les broches bidirectionnelles (`inout`).
