# Exemple : RAM monoport avec lecture write-first

La mémoire lecture-écriture est l'autre moitié de « inférer la BRAM ». Le même motif `type…array…of` marche, mais maintenant le tableau est un **signal**, et tu écris sur certains fronts d'horloge et lis sur les autres.

## Le template

```vhdl
type mem_t is array(0 to 31) of std_logic_vector(7 downto 0);
signal mem : mem_t := (others => (others => '0'));
...
process(clk)
begin
  if rising_edge(clk) then
    if we = '1' then
      mem(to_integer(unsigned(addr))) <= din;
      dout <= din;                             -- lecture write-first
    else
      dout <= mem(to_integer(unsigned(addr)));
    end if;
  end if;
end process;
```

## Les trois politiques de lecture

Toute RAM synchrone doit décider de ce que fait `dout` **pendant une écriture**. Trois conventions :

- **Write-first (ci-dessus) :** lors d'une écriture, `dout` montre les données fraîchement écrites. Sémantique la plus propre — une lecture et une écriture à la même adresse au même cycle te donnent la valeur fraîche.
- **Read-first :** lors d'une écriture, `dout` montre l'**ancien** contenu de la cellule. Certains register files de CPU utilisent ça.
- **No-change :** lors d'une écriture, `dout` ne change pas. Certains designs basse consommation.

Les trois mappent en block RAM sur tout FPGA moderne, mais le synthétiseur choisit le mode selon la forme exacte de ton process. Le template write-first est le plus portable — en cas de doute, écris-le.

## Ce que fait l'exemple

Pendant les 30 premières µs, la sim écrit à trois adresses (`0`, `1`, `2`) avec trois motifs d'octet différents. Puis `we` tombe, et `addr` est remis à 0 à 40 µs. L'onglet Waveforms montre `dout` qui suit `din` pendant les écritures, puis qui ressort la valeur stockée lors de la lecture.

## À toi de jouer

- **Passe à une vraie RAM double port.** Deux jeux complets `addr`/`din`/`dout`/`we`, tous deux cadencés par `clk`. C'est le motif que toutes les FIFOs et DPRAMs double-horloge des fournisseurs utilisent en dessous.
- **Ajoute un registre de sortie.** La block RAM Xilinx supporte un second pipeline register après le tableau mémoire. Ajoute `dout_reg <= dout;` dans un second bloc cadencé et regarde ce qui change sur la waveform (indice : un cycle de latence en plus, mais bien meilleur timing closure).
- **Change le contenu initial.** Précharge les premières entrées avec un motif reconnaissable en modifiant l'initialiseur de `signal mem`. Tous les synthétiseurs ne respectent pas les initialisateurs BRAM en VHDL portable — vérifie ta cible avant de t'y fier en production.
