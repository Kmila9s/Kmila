# Machines Mealy vs Moore

Une machine à états finis (FSM) est une logique de contrôle qui se souvient de ce qu'elle est en train de faire. Toute FSM a :

- Un **registre d'état** (un groupe de flip-flops).
- Une **logique d'état suivant** (combinatoire, décide quel sera l'état au prochain front d'horloge).
- Une **logique de sortie** (combinatoire, décide ce que valent les sorties maintenant).

La manière dont la logique de sortie cherche ses entrées divise les FSM en deux familles.

## Moore : les sorties dépendent uniquement de l'état

```
 entrée → [ état suivant ] → [ reg. d'état ] → [ sorties ]
                                          ↑
                                      (c'est tout)
```

Étant donné un état, la sortie est fixe. Si tu es en `S_GREEN`, `green = '1'`. Toujours. Peu importe ce que fait l'entrée.

**Pour**
- Les sorties ne changent qu'aux fronts d'horloge → pas de glitches.
- Facile à raisonner, facile à déboguer, facile à vérifier.

**Contre**
- Au moins un cycle d'horloge de latence entre l'entrée et la sortie (l'état doit d'abord transitionner).
- Parfois il faut plus d'états pour encoder le même comportement qu'une machine Mealy.

## Mealy : les sorties dépendent de l'état + de l'entrée

```
 entrée ──→ [ état suivant ] ──→ [ reg. d'état ]
   │                                  │
   └───→ [ logique de sortie ] ←──────┘
                  │
              sorties
```

Les sorties sont une fonction de l'état courant **et** de l'entrée courante. Elles peuvent changer entre deux fronts d'horloge.

**Pour**
- Plus rapide — les sorties peuvent réagir à une entrée dans le même cycle, pas au suivant.
- Souvent moins d'états.

**Contre**
- Les sorties peuvent glitcher (elles suivent l'entrée combinatoire).
- Plus difficile à déboguer quand les sorties ne s'alignent pas sur les fronts d'horloge.

## Laquelle utiliser ?

**Par défaut : Moore.** L'absence de glitches et les sorties alignées sur les fronts rendent les designs prévisibles. Pour une logique de contrôle dans un CPU, un contrôleur mémoire, ou toute chose liée à la sûreté, Moore est le choix le plus sûr.

**Choisis Mealy quand** tu as besoin de réagir dans le même cycle d'horloge, ou quand les états supplémentaires de Moore feraient exploser le design. UART et moteurs de protocole finissent souvent Mealy pour l'argument du temps de réaction.

## Comment Kmila les supporte

Les deux styles tombent dans le même **patron à deux process** que tu verras dans l'exemple du feu tricolore :

```vhdl
-- Process 1 : registre d'état (toujours Moore-safe)
process(clk, reset)
begin
  if reset = '1' then
    state <= S_IDLE;
  elsif rising_edge(clk) then
    state <= next_state;
  end if;
end process;

-- Process 2 : next-state combinatoire + sorties
process(state, input)
begin
  ...
end process;
```

Pour une machine Moore, les sorties ne regardent que `state`. Pour une machine Mealy, elles regardent `state` et `input`.

## À retenir

- Moore : la sortie est une fonction de l'état seul. Sans glitches.
- Mealy : la sortie est une fonction de l'état + entrée. Plus rapide.
- Commence par Moore ; passe à Mealy quand tu as une raison précise.
