# Process et listes de sensibilité

Tout en VHDL hors d'un process est concurrent — des fils et des portes. Dès que vous voulez qu'un circuit **se souvienne** de quelque chose d'un cycle d'horloge au suivant, il vous faut un process.

## Ce qu'est un process

Un `process` est un bloc de code séquentiel à l'intérieur d'une architecture VHDL. Ses lignes se lisent de haut en bas, comme du logiciel. Mais le process lui-même ne « tourne » que quand quelque chose dans sa **liste de sensibilité** change.

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

Ce bloc veut dire : « chaque fois que `clk` ou `reset` change, réévalue mon corps ».

## Deux formes que vous voyez 99 % du temps

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

Synthétise un mux 2 vers 1. Aucune mémoire.

### Process cadencé (séquentiel)

Liste de sensibilité = `(clk, reset)`. Les affectations n'ont lieu qu'à l'intérieur de `rising_edge(clk)` (ou `falling_edge`, si pas de chance). La sortie est **enregistrée** — elle ne change qu'aux fronts d'horloge.

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

C'est le motif classique de la bascule D.

## Le piège de la liste de sensibilité

Si vous oubliez d'inscrire un signal que le corps lit, simulation et synthèse se contredisent :

- **Simulation** : le process ne se réévaluera pas quand ce signal change. Le chronogramme a l'air faux.
- **Synthèse** : l'outil lit le corps, voit chaque lecture, et construit quand même le bon matériel.

Résultat : votre banc de test passe, mais la carte ne marche pas — ou l'inverse. VHDL-2008 vous laisse écrire `process(all)` pour éviter ça :

```vhdl
process(all)  -- inclut tout signal lu dans le corps
begin
  ...
end process;
```

Le parser de Kmila accepte `process(all)`, et c'est ce que la plupart des équipes utilisent dans les nouveaux designs.

## Règle empirique

- Sensibilité = juste `(clk, reset)` → vous décrivez des **registres** (bascules).
- Sensibilité = toute entrée lue → vous décrivez de la **logique combinatoire**.
- Sensibilité mélange signal d'horloge et non-horloge → vous avez presque sûrement un bug.

## À retenir

- Un process est l'unité de code séquentiel dans un fichier par ailleurs concurrent.
- Les process cadencés (`rising_edge(clk)`) se synthétisent en bascules. C'est comme ça que vous avez de la mémoire.
- Listez chaque signal que vous lisez, ou utilisez `process(all)` pour laisser l'outil le faire.
