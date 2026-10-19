# Exemple : ROM synchrone inférée

Tout FPGA possède de la **block RAM** (BRAM) — des blocs mémoire dédiés que le synthétiseur utilise quand il reconnaît un motif de ROM ou de RAM dans votre code. Vous n'instanciez pas de primitives propriétaires ; vous écrivez du VHDL ordinaire dans une forme que l'outil sait mapper. L'« inférence » est le nom de cette reconnaissance : l'outil lit votre tableau-et-process et *infère* que vous vouliez une mémoire.

Cet exemple est le motif ROM portable et classique. Tout synthétiseur sérieux (Xilinx/AMD Vivado, Intel/Altera Quartus, Lattice Radiant, Yosys open-source) le reconnaît et alloue automatiquement de la BRAM.

## Le motif

```vhdl
type rom_t is array(0 to 15) of std_logic_vector(7 downto 0);
constant ROM : rom_t := (
  x"00", x"01", x"04", x"09", x"10", x"19", x"24", x"31",
  x"40", x"51", x"64", x"79", x"90", x"A9", x"C4", x"E1"
);

process(clk)
begin
  if rising_edge(clk) then
    data <= ROM(to_integer(unsigned(addr)));
  end if;
end process;
```

Trois choses font de ça une ROM compatible BRAM :

1. **Le stockage est une constante** (`constant ROM : rom_t := (...)`). Les données sont figées à la synthèse.
2. **La lecture est cadencée.** Une lecture ROM combinatoire ne peut être mappée qu'en LUTs — c'est correct pour une ROM minuscule de 16 entrées, mais tout ce qui est plus gros a besoin du motif cadencé pour que le synthétiseur alloue de la BRAM.
3. **`addr` est utilisé purement comme indice.** Pas de logique combinatoire coûteuse entre `addr` et l'accès mémoire.

## Combinatoire vs synchrone, et latence de lecture

La lecture cadencée, c'est tout le jeu. Écrivez la lecture comme une simple affectation concurrente —

```vhdl
data <= ROM(to_integer(unsigned(addr)));   -- combinatoire : ROM distribuée (LUTs)
```

— et l'outil construit la ROM à partir de LUTs (mémoire « distribuée »). C'est zéro latence de lecture mais ça coûte de la logique générale, et ça ne passe pas à l'échelle : quelques kilo-octets de ROM en LUT noieront un petit composant. Registrez la lecture (le `process` ci-dessus) et l'outil peut déposer toute la table dans une block RAM, bien plus dense.

Le prix de la lecture registrée est **un cycle de latence** : vous présentez `addr` sur un front, et `data` apparaît au front *suivant*. Ce n'est pas un bug — c'est la caractéristique qui définit une mémoire synchrone, et le registre de sortie interne de la BRAM est ce qui permet au design de tenir le timing à haute fréquence. Beaucoup de FPGA offrent un second registre de sortie, optionnel, pour un timing encore meilleur au prix d'un second cycle de latence.

## La largeur d'adresse doit correspondre à la profondeur

`to_integer(unsigned(addr))` ne doit jamais indexer au-delà du tableau. Pour une ROM de 16 entrées, `addr` devrait faire exactement 4 bits ; un `addr` plus large peut produire un indice hors bornes qui échoue en simulation et se comporte de façon imprévisible après synthèse. Gardez `2**addr'length = DEPTH`, ou bornez/protégez l'indice. Liez ensemble la profondeur, la largeur et la taille d'adresse, et tout reste cohérent.

## Pourquoi ce contenu ?

L'exemple stocke `n²` pour `n = 0..15`. Utile comme unité de mise au carré par lookup — plus rapide qu'un vrai multiplieur quand vous n'avez besoin que de petits opérandes. Vous pourriez aussi y mettre :

- Une table sinusoïdale (pour générateurs de signaux DDS)
- Des polynômes CRC
- Des tables de lookup de protocole
- Des fontes de caractères

Une ROM n'est vraiment qu'une table de vérité figée dans le silicium : toute fonction combinatoire des bits d'adresse peut être précalculée et stockée, échangeant de la logique contre de la mémoire.

## Ce que fait l'exemple

La sim pilote `addr` à travers 5 valeurs précises : 0, 1, 4, 8, 15. Après le front d'horloge, `data` montre l'entrée ROM correspondante, un cycle plus tard (c'est la latence de lecture d'une ROM synchrone — vous demandez l'adresse, vous obtenez les données au front **suivant**).

## À vous de jouer

- **Paramétrez-le.** Remplacez la taille fixe de 16 entrées par `generic (DEPTH : integer := 16; WIDTH : integer := 8)`, et dimensionnez `addr` en `std_logic_vector(log2(DEPTH)-1 downto 0)`. Le reste du code devrait continuer à marcher.
- **Lisez le contenu depuis un fichier.** Les vrais projets chargent le contenu de ROM via une fonction d'initialisation ou un package de constantes séparé. Essayez de faire lire au VHDL un fichier texte de valeurs hex (`std.textio` + `hread`) dans une fonction impure qui retourne le tableau — la plupart des outils l'exécutent à l'élaboration et cuisent le résultat dans la BRAM.
- **Ajoutez un second port de lecture.** Pour des choses comme la mémoire d'instructions/données dans un CPU monocycle. Vous verrez ce motif dans le projet CPU plus loin dans le cursus.

## Liens avec le cours

C'est l'unité IV, *Memorias* : la famille des ROM (ROM, PROM, EPROM, EEPROM, Flash) et, spécifiquement, le *manejo de arreglos y diseño de memorias con lenguaje de descripción de hardware*. La constante `type … array … of` est exactement le « maniement de tableaux » que l'unité demande ; l'exercice d'initialisation par fichier tend la main vers le travail de l'unité V avec fichiers, fonctions et packages.

## À retenir

- L'inférence consiste à écrire la mémoire dans une forme que l'outil reconnaît — aucune primitive propriétaire nécessaire.
- Un tableau **constant** lu dans un process cadencé est le motif portable de ROM synchrone ; il se mappe en block RAM.
- Lecture cadencée = un cycle de latence et BRAM dense ; lecture combinatoire = zéro latence mais gourmande en LUT et non extensible.
- Faites correspondre la largeur d'adresse à la profondeur pour que l'indice ne sorte jamais du bout du tableau.
