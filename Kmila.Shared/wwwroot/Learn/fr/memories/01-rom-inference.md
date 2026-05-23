# Exemple : ROM synchrone inférée

Tout FPGA possède de la **block RAM** (BRAM) — des blocs mémoire dédiés que le synthétiseur utilise quand il reconnaît un motif de ROM ou de RAM dans ton code. Tu n'instances pas de primitives propriétaires ; tu écris du VHDL ordinaire dans une forme que l'outil sait mapper.

Cet exemple est le motif ROM portable et classique. Tout synthétiseur sérieux (Xilinx Vivado, Intel Quartus, Lattice Radiant, Yosys) le reconnaît et alloue automatiquement de la BRAM.

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

## Pourquoi ce contenu ?

L'exemple stocke `n²` pour `n = 0..15`. Utile comme unité de mise au carré par lookup — plus rapide qu'un vrai multiplieur quand tu n'as besoin que de petits opérandes. Tu pourrais aussi y mettre :

- Une table sinusoïdale (pour générateurs DDS)
- Des polynômes CRC
- Des tables de lookup de protocole
- Des fontes de caractères

## Ce que fait l'exemple

La sim pilote `addr` à travers 5 valeurs précises : 0, 1, 4, 8, 15. Après le front d'horloge, `data` montre l'entrée ROM correspondante, un cycle plus tard (c'est la latence de lecture d'une ROM synchrone — tu demandes l'adresse, tu obtiens les données au cycle **suivant**).

## À toi de jouer

- **Paramètre-le.** Remplace la taille fixe de 16 entrées par `generic (DEPTH : integer := 16; WIDTH : integer := 8)`. Le reste du code doit continuer à marcher.
- **Lis le contenu depuis un fichier.** Les vrais projets chargent les ROMs via `init_from_file()` ou un package de constantes. Essaie de faire lire un fichier texte de valeurs hex (`textio` + `hread`) à l'élaboration.
- **Ajoute un second port de lecture.** Pour des choses comme mémoire d'instructions/données dans un CPU monocycle. Tu verras ce motif plus tard dans le projet CPU du cursus.
