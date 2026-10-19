# Process et listes de sensibilité

Tout ce qui, en VHDL, se trouve hors d'un process est concurrent — des fils et des portes. Dès l'instant où vous voulez qu'un circuit **se souvienne** de quelque chose d'un cycle d'horloge au suivant, vous recourez à un process. Les process sont là où vivent les « instructions séquentielles » de l'unité HDL, et ils sont le pont entre la logique combinatoire (Unité III) et les éléments de mémoire (Unité IV).

## Ce qu'est un process

Un `process` est un bloc de code séquentiel à l'intérieur d'une architecture VHDL. Ses lignes se lisent de haut en bas, exactement comme du logiciel. Mais le process lui-même ne « tourne » que quand quelque chose dans sa **liste de sensibilité** change — la liste est la condition de réveil du process.

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

Ce bloc veut dire : « chaque fois que `clk` ou `reset` change, réévalue mon corps ». Entre ces événements, le process dort et `q` conserve sa valeur — ce qui est exactement le comportement d'un registre.

## Deux formes que vous verrez 99 % du temps

### Process combinatoire

Liste de sensibilité = **chaque signal lu** à l'intérieur. Pas d'horloge. La sortie est une fonction pure des entrées.

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

Se synthétise en un mux 2 vers 1. Aucune mémoire — à condition que chaque sortie soit affectée sur chaque chemin (on en reparle dans la correction du verrou).

### Process cadencé (séquentiel)

Liste de sensibilité = `(clk, reset)` pour un reset asynchrone, ou juste `(clk)` pour un reset synchrone. Les affectations de données n'ont lieu qu'à l'intérieur de `rising_edge(clk)` (ou `falling_edge`). La sortie est **enregistrée** — elle ne change qu'aux fronts d'horloge.

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

C'est le motif classique de la bascule D, et c'est le gabarit de pratiquement chaque registre que vous écrirez.

## `rising_edge` contre le vieil idiome `clk'event`

Vous verrez deux façons de détecter un front d'horloge :

```vhdl
elsif rising_edge(clk) then        -- préféré
elsif clk'event and clk = '1' then -- plus ancien, équivalent pour '0'/'1'
```

`rising_edge(clk)` (de `std_logic_1164`) est la forme moderne, plus claire. Elle ignore aussi correctement les fronts qui impliquent des métavaleurs (comme `'X'` → `'1'`), là où le brut `clk'event and clk='1'` peut se déclencher à tort dessus. Utilisez `rising_edge`.

## Le piège de la liste de sensibilité

Si vous oubliez d'inscrire un signal qu'un corps *combinatoire* lit, simulation et synthèse se contredisent :

- **Simulation** : le process ne se réévaluera pas quand ce signal changera. Le chronogramme a l'air faux (les sorties semblent « bloquées »).
- **Synthèse** : l'outil lit le corps, voit chaque lecture, et construit quand même le bon matériel — en ignorant votre liste.

Résultat : votre banc de test et votre carte se comportent différemment, ce qui est le pire genre de bug. (Les process cadencés sont immunisés contre ce piège précis puisqu'ils n'agissent qu'au front d'horloge, mais une liste incomplète sur un process *combinatoire* est une cause classique de pannes fantômes.) VHDL-2008 règle ça radicalement avec `process(all)` :

```vhdl
process(all)  -- l'outil inclut chaque signal lu dans le corps
begin
  ...
end process;
```

Le parser de Kmila accepte `process(all)`, et c'est ce que la plupart des équipes utilisent pour les process combinatoires dans les nouveaux designs.

## Une règle empirique

- Sensibilité = juste `(clk)` ou `(clk, reset)` → vous décrivez des **registres** (bascules).
- Sensibilité = chaque entrée que vous lisez (ou `all`) → vous décrivez de la **logique combinatoire**.
- Sensibilité mélange un front d'horloge avec des signaux de données supplémentaires que vous utilisez ensuite *hors* du front → vous avez presque à coup sûr un bug.

## Encore une : ne pilotez pas le même signal depuis deux process

Un signal piloté à l'intérieur d'un process a ce process pour driver. Si un *second* process (ou une instruction concurrente) le pilote aussi, vous avez créé des drivers multiples — résolus en `'X'` pour `std_logic`, ou une erreur de compilation pour les types non résolus. Donnez à chaque signal exactement un driver.

## À retenir

- Un process est l'unité de code séquentiel au sein d'un fichier par ailleurs concurrent.
- Les process cadencés (`rising_edge(clk)`) se synthétisent en bascules — c'est comme ça que vous obtenez de la mémoire.
- Préférez `rising_edge(clk)` à `clk'event and clk='1'`.
- Listez chaque signal qu'un process combinatoire lit, ou utilisez `process(all)` ; une liste incomplète fait diverger sim et synthèse.
- Un signal, un driver.
