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

Sorties (Moore pur) :

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## Ce que fait l'exemple

L'horloge tourne pendant environ 400 µs. Tous les 8 cycles (7 de maintien + 1 de transition), la FSM avance. Vous voyez trois signaux carrés tourner en séquence : `red` passe à 1 pendant 8 ticks, puis `green` pendant 8 ticks, puis `yellow`, puis retour à `red`.

## À vous de jouer

- **Ajoutez une entrée requête piéton.** Quand `ped = '1'` et que l'état est `S_GREEN`, forcez `next_state` à `S_YELLOW` (le cycle se raccourcit pour laisser traverser les piétons). Ça transforme la machine Moore en quelque chose de Mealy-esque — la sortie `yellow` dépend maintenant de `state` **et** de `ped`.
- **Émettez un signal `walk`** qui n'est haut que quand `state = S_RED`. Nouvelle sortie Moore.
- **Paramétrez `HOLD`** en generic — 7 va pour la simu, mais sur un FPGA 50 MHz il faut quelques centaines de millions de cycles pour tenir 1 s.
