# Exemple : RAM monoport avec lecture write-first

La mémoire lecture-écriture est l'autre moitié de « inférer la BRAM ». Le même motif `type…array…of` marche, mais maintenant le tableau est un **signal**, et vous écrivez sur certains fronts d'horloge et lisez sur les autres.

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

Pourquoi un **signal** et pas une `variable` ou une constante ? Le tableau doit être un état partagé qui persiste à travers les fronts d'horloge et se fait *réécrire* à l'exécution — c'est exactement ce qu'est un signal. Une constante (comme dans l'exemple de la ROM) ne peut pas être écrite ; une variable de process ne serait pas visible hors du process. Et comme chaque accès est à l'intérieur de `rising_edge(clk)`, la lecture comme l'écriture sont **synchrones** — ce qui est ce qui permet à l'outil de tout mapper en block RAM.

## Les trois politiques de lecture

Toute RAM synchrone doit décider de ce que fait `dout` **pendant une écriture** à l'adresse qu'on est en train de lire. Ce comportement de « lecture-pendant-écriture » a trois conventions standard, et la forme de votre process choisit celle que vous obtenez :

- **Write-first** (aussi appelée read-through, montrée ci-dessus) : lors d'une écriture, `dout` montre les données fraîchement écrites. Sémantique la plus propre — une lecture et une écriture à la même adresse au même cycle vous donnent la valeur fraîche.
- **Read-first :** lors d'une écriture, `dout` montre l'**ancien** contenu de la cellule. Certains register files de CPU utilisent ça.
- **No-change :** lors d'une écriture, `dout` ne se met pas du tout à jour — il garde sa dernière valeur. Courant dans les designs basse consommation, puisque le registre de sortie n'est pas basculé lors des écritures.

La différence tient purement à l'*ordre* des deux affectations dans le process :

```vhdl
-- READ-FIRST : capturer l'ancienne donnée, puis écraser
if we = '1' then
  dout <= mem(to_integer(unsigned(addr)));   -- ancienne valeur en sortie
  mem(to_integer(unsigned(addr))) <= din;    -- puis on écrit
end if;

-- NO-CHANGE : ne mettre dout à jour que quand on N'écrit PAS
if we = '1' then
  mem(to_integer(unsigned(addr))) <= din;    -- dout laissé intact
else
  dout <= mem(to_integer(unsigned(addr)));
end if;
```

Les trois se mappent en block RAM sur tout FPGA moderne, et chaque port d'une vraie RAM double port peut même utiliser un mode différent. Mais le synthétiseur choisit le mode d'après la forme *exacte* de votre process, alors faites correspondre le template au comportement que vous voulez réellement. La forme write-first est la plus largement portable — en cas de doute, écrivez-la.

## Ce que fait l'exemple

Pendant les 30 premières µs, la sim écrit à trois adresses (`0`, `1`, `2`) avec trois motifs d'octet différents. Puis `we` tombe, et `addr` est remis à 0 à 40 µs. L'onglet Formes d'onde montre `dout` qui suit `din` pendant les écritures, puis qui ressort la valeur stockée lors de la lecture — un cycle après que vous présentez l'adresse, parce que la lecture est registrée.

## Attention à

- **Largeur d'adresse vs profondeur.** Comme pour la ROM, `to_integer(unsigned(addr))` doit rester dans `0 .. 31`. Dimensionnez `addr` à `log2(DEPTH)` bits pour qu'il ne puisse pas sur-indexer.
- **Ne lisez pas en combinatoire.** Une lecture combinatoire (`dout <= mem(...)` hors du process cadencé) bloque généralement l'inférence de BRAM et force le tableau en flip-flops/LUTs — correct pour une douzaine d'octets, désastreux pour des kilo-octets.
- **Les initialisateurs ne sont pas garantis.** La valeur initiale `signal mem := (others => ...)` marche en simulation et sur beaucoup de FPGA (où la BRAM a un état de mise sous tension), mais pas sur toutes les cibles et jamais sur une vraie SRAM/DRAM externe, qui démarre indéfinie. Ne vous y fiez pas en production sans vérifier votre composant.

## À vous de jouer

- **Passez à une vraie RAM double port.** Deux jeux complets `addr`/`din`/`dout`/`we`, tous deux cadencés par `clk`. C'est le motif que toutes les FIFOs et DPRAMs double-horloge des fournisseurs utilisent en dessous.
- **Ajoutez un registre de sortie.** La block RAM supporte un second registre de pipeline après le tableau mémoire. Ajoutez `dout_reg <= dout;` dans un second bloc cadencé et regardez ce qui change sur la waveform (indice : un cycle de latence en plus, mais bien meilleur timing closure).
- **Construisez une FIFO par-dessus.** Enveloppez la RAM avec un pointeur d'écriture, un pointeur de lecture et des drapeaux `full`/`empty`. C'est la façon standard de construire une FIFO synchrone, et elle est demandée nommément dans cette unité.

## Liens avec le cours

C'est le cœur de l'unité IV, *Memorias* : RAM / SRAM / DRAM et la conception de mémoires RAM, ROM et **FIFO** en HDL via le maniement de tableaux. Write-first/read-first/no-change est le visage HDL du comportement de lecture-pendant-écriture qu'exhibent les vraies cellules SRAM ; l'exercice de FIFO ci-dessus est exactement les *tipos especiales de memorias* que liste l'unité.

## À retenir

- Mémoire lecture-écriture = le même motif de tableau que la ROM, mais un **signal** que vous écrivez à l'exécution.
- Gardez lecture et écriture à l'intérieur de `rising_edge(clk)` pour que l'outil infère la block RAM.
- Write-first / read-first / no-change se choisissent par l'ordre des instructions dans le process — prenez celui dont votre design a besoin ; write-first est le défaut sûr.
- Surveillez la largeur d'adresse, évitez les lectures combinatoires, et ne faites pas confiance aux initialisateurs de BRAM sur toutes les cibles.
