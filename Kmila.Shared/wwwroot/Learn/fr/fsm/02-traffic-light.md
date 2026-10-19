# Exemple : FSM de feu tricolore

Un feu tricolore est la FSM Moore canonique : trois états (`RED`, `GREEN`, `YELLOW`), une sortie par couleur, et un timer qui dit « tiens chaque état quelques cycles avant de bouger ».

## La structure

L'exemple suit le **motif à deux process** :

- Process 1 est le **registre d'état** — une bascule D classique qui stocke `state` et fait tiquer un compteur de maintien.
- Process 2 est la **logique de prochain état** — purement combinatoire, décide ce que vaut `next_state` selon l'état courant.
- Les affectations de sortie (en bas de l'architecture) sont des sorties Moore combinatoires — leurs valeurs ne dépendent que de `state`.

## Parcours du code

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 ticks par couleur
```

`state_t` est une **énumération** — le synthétiseur choisit l'encodage binaire réel, nous on ne fait référence qu'à des états nommés. `count` suit combien de temps on est dans l'état courant. `HOLD` est la durée de maintien.

Registre d'état :

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    state <= S_RED;
    count <= (others => '0');
  elsif rising_edge(clk) then
    if count = HOLD then
      state <= next_state;
      count <= (others => '0');
    else
      count <= count + 1;
    end if;
  end if;
end process;
```

Deux détails à nommer. D'abord, c'est un **reset asynchrone** (`reset` est dans la liste de sensibilité et testé avant `rising_edge`) — le feu claque sur `S_RED` à l'instant même où le reset s'active. Un reset synchrone retirerait `reset` de la liste de sensibilité et ne le testerait qu'à l'intérieur de la branche `rising_edge` ; les deux sont valables, choisissez juste un style et restez cohérent. Ensuite, le compteur transforme une transition d'état en un maintien de 8 cycles — l'état n'avance que sur le cycle où `count = HOLD`.

Logique de prochain état :

```vhdl
process(state)
begin
  case state is
    when S_RED    => next_state <= S_GREEN;
    when S_GREEN  => next_state <= S_YELLOW;
    when S_YELLOW => next_state <= S_RED;
  end case;
end process;
```

Parce que ce process affecte `next_state` sur chaque branche et que le `case` est exhaustif sur `state_t`, il n'y a pas de latch inféré ici. La liste de sensibilité n'est que `(state)` — correct, car `next_state` ne dépend de rien d'autre. Dès l'instant où vous faites dépendre l'état suivant d'une entrée, cette entrée doit rejoindre la liste de sensibilité (sinon le process combinatoire simulera faux), et vous devriez ajouter un défaut `next_state <= state;` sur la première ligne, par sécurité.

Sorties (Moore pur) :

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

Chaque sortie est une fonction de `state` seul — elles ne changent donc que quand le registre tique, et ne peuvent pas glitcher entre les fronts. C'est tout l'intérêt d'une machine Moore pour quelque chose qui pilote un signal physique.

## Ce que fait l'exemple

L'horloge tourne pendant environ 400 µs. Tous les 8 cycles (7 de maintien + 1 de transition), la FSM avance. Vous voyez trois signaux carrés tourner en séquence : `red` passe à 1 pendant 8 ticks, puis `green` pendant 8 ticks, puis `yellow`, puis retour à `red`.

## De la simulation à un vrai carrefour

`HOLD = 7` est une commodité de simulation. Sur un FPGA à 50 MHz, un tick fait 20 ns, donc sept ticks font 140 ns — pas un feu tricolore, un stroboscope. Pour un vrai maintien d'une seconde, il faudrait compter jusqu'à 50 000 000, ce qui veut dire élargir `count` et la constante `HOLD`. La façon propre, c'est un **generic** :

```vhdl
generic ( HOLD : natural := 7 );
...
signal count : unsigned(31 downto 0);
```

Simulez avec un `HOLD` minuscule, synthétisez avec un gros — même RTL, aucune retouche. Les vrais carrefours ajoutent aussi un **état tout-rouge de sécurité** entre deux verts en conflit ; en ajouter un est un bon exercice pour étendre une machine Moore sans casser sa garantie d'absence de glitch.

## À vous de jouer

- **Ajoutez une entrée requête piéton.** Quand `ped = '1'` et que l'état est `S_GREEN`, forcez `next_state` à `S_YELLOW` (le cycle se raccourcit pour laisser traverser les piétons). Ça transforme la machine Moore en quelque chose de Mealy-esque — l'état suivant dépend maintenant à la fois de `state` **et** de l'entrée `ped`, donc ajoutez `ped` à la liste de sensibilité et une affectation par défaut.
- **Émettez un signal `walk`** qui n'est haut que quand `state = S_RED`. C'est une nouvelle sortie Moore.
- **Paramétrez `HOLD`** en generic — 7 va pour la simu, mais sur un FPGA 50 MHz il vous faudrait 50 millions de cycles pour obtenir un maintien d'1 s.

## Liens avec le cours

Cet exemple est l'*aplicaciones con autómatas y lenguaje de descripción de hardware* de l'unité II en miniature : un automate Moore déterministe, dessiné en diagramme à trois états, puis décrit en HDL. Le compteur qui étire chaque état en un maintien temporisé est aussi votre premier avant-goût de l'unité III (contadores) vivant à l'intérieur d'une machine à états.

## À retenir

- Le feu tricolore est une FSM Moore de manuel : les sorties ne dépendent que de `state`, donc elles sont sans glitch.
- Le découpage en deux process (registre cadencé + prochain état combinatoire) garde le design lisible et sans latch.
- Un compteur à l'intérieur du registre d'état transforme une transition en un maintien temporisé ; paramétrez le maintien avec un generic pour qu'un seul RTL serve à la fois la simu et le silicium.
- Dès qu'une entrée influence l'état suivant, ajoutez-la à la liste de sensibilité et affectez `next_state` par défaut.
