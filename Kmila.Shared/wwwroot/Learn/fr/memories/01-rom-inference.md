# Exemple : ROM synchrone inférée

Chaque FPGA a des **block RAM** (BRAM) — des blocs mémoire dédiés que le synthétiseur utilise lorsqu'il reconnaît un pattern de ROM ou de RAM dans ton code. Tu n'instancies pas de primitives fournisseur ; tu écris du VHDL normal dans une forme que l'outil sait mapper.

Cet exemple est le pattern de ROM portable, celui des manuels. Tout synthétiseur sérieux (Xilinx Vivado, Intel Quartus, Lattice Radiant, Yosys) le reconnaîtra et allouera de la BRAM automatiquement.

## Le pattern

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

Trois choses font de cette ROM une ROM BRAM-friendly :

1. **Le stockage est une constante** (`constant ROM : rom_t := (...)`). Les données sont fixées au moment de la synthèse.
2. **La lecture est cadencée.** Une lecture combinatoire de ROM ne peut être mappée qu'en LUT — c'est correct pour une toute petite ROM de 16 entrées, mais tout ce qui est plus gros a besoin du pattern cadencé pour que le synthétiseur alloue de la BRAM.
3. **`addr` est utilisé purement comme index.** Pas de logique combinatoire coûteuse entre `addr` et l'accès mémoire.

## Pourquoi ces contenus ?

La ROM d'exemple contient `n²` pour `n = 0..15`. Utile pour une unité de mise au carré par table de lookup — plus rapide qu'un vrai multiplieur quand tu n'as besoin que de petits opérandes. Tu pourrais aussi stocker :

- Une table sinus (pour des générateurs DDS)
- Des polynômes CRC
- Des tables de lookup de protocole
- Des fontes de caractères

## Ce que fait l'exemple

La sim pilote `addr` à travers 5 valeurs spécifiques : 0, 1, 4, 8, 15. Après le front d'horloge, `data` affiche l'entrée ROM correspondante, un cycle plus tard (c'est la latence de lecture d'une ROM synchrone — tu demandes l'adresse, tu obtiens la donnée au **prochain** coup d'horloge).

## À toi

- **Rends-la paramétrable.** Remplace la taille fixe de 16 entrées par `generic (DEPTH : integer := 16; WIDTH : integer := 8)`. Le reste du code doit continuer à fonctionner.
- **Lis le contenu depuis un fichier.** Les vrais projets chargent le contenu des ROM via `init_from_file()` ou un package séparé de constantes. Essaie de faire lire au VHDL un fichier texte de valeurs hex (`textio` + `hread`) au moment de l'élaboration.
- **Ajoute un second port de lecture.** Pour des choses comme la mémoire instruction/donnée dans un CPU single-cycle. Tu verras ce pattern dans le projet CPU plus tard dans le programme.
