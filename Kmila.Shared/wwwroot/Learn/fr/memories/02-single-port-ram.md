# Exemple : RAM simple port avec lecture write-first

La mémoire lecture/écriture est l'autre moitié de « l'inférence de BRAM ». Le même pattern `type…array…of` fonctionne, mais cette fois le tableau est un **signal**, et tu écris sur certains fronts d'horloge et tu lis sur les autres.

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

Toute RAM synchrone doit décider ce que fait `dout` **pendant une écriture**. Trois conventions existent :

- **Write-first (montré ci-dessus) :** lors d'une écriture, `dout` affiche la donnée fraîchement écrite. Sémantique la plus propre — une lecture et une écriture à la même adresse dans le même cycle te donnent la valeur fraîche.
- **Read-first :** lors d'une écriture, `dout` affiche l'**ancien** contenu de la cellule. Le banc de registres de certains CPU utilise cela.
- **No-change :** lors d'une écriture, `dout` ne se met pas à jour. Certains designs basse consommation.

Les trois se mappent sur du block RAM sur tout FPGA moderne, mais le synthétiseur choisit le mode en fonction de la forme exacte de ton process. Le template write-first est le plus portable — en cas de doute, écris celui-là.

## Ce que fait l'exemple

Pendant les 30 premières µs, la sim écrit à trois adresses (`0`, `1`, `2`) avec trois patterns d'octet différents. Puis `we` descend, et `addr` est remis à 0 à 40 µs. L'onglet Waveforms montre `dout` qui suit `din` durant les écritures, puis qui restitue la valeur stockée quand tu lis.

## À toi

- **Passe à une vraie RAM double port.** Deux jeux complets de `addr`/`din`/`dout`/`we`, tous deux cadencés par `clk`. C'est le pattern que toutes les FIFO et DPRAM double-horloge de tous les fournisseurs utilisent en dessous.
- **Ajoute un registre de sortie.** Le block RAM Xilinx supporte un second registre de pipeline après le tableau mémoire. Ajoute `dout_reg <= dout;` dans un second bloc cadencé et observe ce qui change dans la forme d'onde (indice : un cycle de latence supplémentaire, mais une bien meilleure fermeture temporelle).
- **Change le contenu initial.** Précharge les premières entrées avec un pattern reconnaissable en modifiant l'initialiseur de `signal mem`. Tous les synthétiseurs ne respectent pas les initialisateurs BRAM en VHDL portable — vérifie ta cible avant de compter dessus en production.
