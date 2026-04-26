# Process et listes de sensibilité

Tout ce qui est hors d'un process en VHDL est concurrent — des fils et des portes. Dès que tu veux qu'un circuit **se souvienne** de quelque chose d'un cycle d'horloge au suivant, il te faut un process.

## Ce qu'est un process

Un `process` est un bloc de code séquentiel à l'intérieur d'une architecture VHDL. Ses lignes sont lues de haut en bas, comme en logiciel. Mais le process lui-même ne « s'exécute » que lorsque quelque chose dans sa **liste de sensibilité** change.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Ce bloc signifie : « dès que `clk` ou `reset` change, ré-évalue mon corps ».

## Deux formes que tu verras dans 99% des cas

### Process combinatoire

Liste de sensibilité = chaque entrée lue à l'intérieur. Pas d'horloge. La sortie est une fonction pure des entrées.

```vhdl
process(a, b, sel)
begin
  if sel = '0' then
    y <= a;
  else
    y <= b;
  end if;
end process;
```

Se synthétise en un mux 2 vers 1. Pas de mémoire.

### Process cadencé (séquentiel)

Liste de sensibilité = `(clk, reset)`. Les assignations ne se font qu'à l'intérieur de `rising_edge(clk)` (ou `falling_edge`, si tu n'as pas de chance). La sortie est **registrée** — elle ne change qu'aux fronts d'horloge.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= (others => '0');
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

C'est le pattern classique du D flip-flop.

## Le piège de la liste de sensibilité

Si tu oublies de lister un signal lu dans le corps, simulation et synthèse divergent :

- **Simulation** : le process ne se réévaluera pas quand ce signal change. La forme d'onde paraît fausse.
- **Synthèse** : l'outil lit le corps, voit chaque lecture, et construit quand même le bon matériel.

Résultat : ton testbench passe, mais la carte ne marche pas — ou l'inverse. Le VHDL-2008 moderne te permet d'écrire `process(all)` pour éviter tout ça :

```vhdl
process(all)  -- inclut tout signal lu dans le corps
begin
  ...
end process;
```

Le parseur de Kmila accepte `process(all)`, et c'est ce que la plupart des équipes utilisent dans les nouveaux designs.

## Règle empirique

- Sensibilité = juste `(clk, reset)` → tu décris des **registres** (flip-flops).
- Sensibilité = chaque entrée lue → tu décris de la **logique combinatoire**.
- Sensibilité mélange signaux cadencés et non cadencés → tu as presque certainement un bug.

## À retenir

- Un process est l'unité de code séquentiel dans un fichier par ailleurs concurrent.
- Les process cadencés (`rising_edge(clk)`) se synthétisent en flip-flops. C'est comme ça que tu obtiens de la mémoire.
- Liste chaque signal que tu lis, ou utilise `process(all)` pour laisser l'outil le faire pour toi.
