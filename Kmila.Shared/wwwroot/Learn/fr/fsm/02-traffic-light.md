# Exemple : FSM de feu tricolore

Un feu tricolore est la FSM Moore canonique : trois états (`RED`, `GREEN`, `YELLOW`), une sortie par couleur, et un timer qui dit « reste quelques cycles dans chaque état avant de passer à la suite ».

## La structure

L'exemple suit le **patron à deux process** :

- Le process 1 est le **registre d'état** — un classique D flip-flop qui stocke `state` et fait tiquer un compteur de maintien.
- Le process 2 est la **logique d'état suivant** — purement combinatoire, décide ce que vaut `next_state` étant donné l'état courant.
- Les assignations de sortie (bas de l'architecture) sont des sorties Moore combinatoires — leurs valeurs ne dépendent que de `state`.

## Promenade dans le code

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 ticks par couleur
```

`state_t` est une **énumération** — le synthétiseur choisit l'encodage binaire réel, nous nous contentons d'états nommés. `count` garde trace de la durée passée dans l'état courant. `HOLD` est le temps de maintien.

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

Logique d'état suivant :

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

Sorties (pur Moore) :

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## Ce que fait l'exemple

L'horloge tourne pendant environ 400 µs. Tous les 8 cycles (7 de maintien + 1 de transition), la FSM avance. Tu verras trois signaux carrés qui tournent en séquence : `red` reste haut pendant 8 ticks, puis `green` pendant 8 ticks, puis `yellow`, puis retour à `red`.

## À toi

- **Ajoute une entrée de demande piéton.** Quand `ped = '1'` et que l'état est `S_GREEN`, force l'état suivant à `S_YELLOW` (pour raccourcir le cycle et laisser les piétons traverser). Ça transforme la machine Moore en quelque chose de type Mealy — la sortie `yellow` dépend à la fois de `state` **et** de l'entrée `ped`.
- **Émets un signal `walk`** haut seulement quand `state = S_RED`. C'est une nouvelle sortie Moore.
- **Paramétrise `HOLD`** en tant que generic — 7, c'est bien pour la simulation, mais sur un FPGA à 50 MHz tu voudrais quelques centaines de millions de cycles pour obtenir un maintien d'une seconde.
