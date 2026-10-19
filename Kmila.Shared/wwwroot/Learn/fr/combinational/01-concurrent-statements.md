# Affectations concurrentes : `when/else` et `with/select`

La logique combinatoire en VHDL s'écrit avec des instructions concurrentes — chaque ligne hors d'un process décrit un morceau de matériel câblé en dur. Il existe deux saveurs syntaxiques pour le même travail : l'affectation **conditionnelle** (`when/else`) et **sélective** (`with/select`). Ce sont les « instructions concurrentes » de l'unité HDL, et elles sont l'épine dorsale de tout circuit combinatoire du cours : additionneurs, convertisseurs de code, comparateurs, multiplexeurs, encodeurs, décodeurs.

## `when/else` — conditionnelle

Bien adaptée quand vous avez une liste priorisée de conditions, ou quand chaque branche teste un signal *différent* :

```vhdl
y <= (others => '0') when reset = '1' else
     a               when sel = "00"  else
     b               when sel = "01"  else
     c               when sel = "10"  else
     d;
```

La première condition qui correspond l'emporte. L'ordre compte — ça compile vers un **mux prioritaire** : la condition la plus haute a le chemin le plus direct vers `y`. Notez que toutes les expressions de valeur (`a`, `b`, `c`, `d`, l'agrégat tout-à-zéro) doivent avoir le **même type et la même largeur** que `y`, et que le `else` final n'a pas de condition — c'est le cas attrape-tout.

## `with/select` — sélective

Bien adaptée quand vous aiguillez sur une seule expression (comme le fait un `switch`/`case` logiciel) :

```vhdl
with sel select
  y <= a   when "00",
       b   when "01",
       c   when "10",
       'Z' when others;
```

Tous les choix comparent à la même expression (`sel`), les branches sont séparées par des **virgules** (pas par `else`), et `when others` est **obligatoire** pour couvrir toute valeur possible. Ce style compile vers un **mux équilibré** — aucune branche n'est privilégiée.

### Pourquoi `when others` n'est pas négociable

`sel` est ici un `std_logic_vector(1 downto 0)`. Vous penseriez que ça fait quatre cas — `"00"`, `"01"`, `"10"`, `"11"`. Mais chaque bit est un `std_logic` à *neuf valeurs*, alors le compilateur compte 9² = 81 motifs possibles (`"0X"`, `"ZU"`, …). Vous ne pouvez pas les énumérer, donc `when others` est requis pour rendre l'instruction totale. En synthèse, les métavaleurs disparaissent et vous obtenez le mux 4 voies propre que vous aviez en tête.

Vous pouvez aussi regrouper des choix avec l'opérateur barre :

```vhdl
with opcode select
  result <= a + b    when "000" | "001",   -- deux codes, même sortie
            a - b    when "010",
            (others => '0') when others;
```

## Lequel choisir ?

- **Même signal, beaucoup de motifs** (décodeur, mux, convertisseur de code) : `with/select` se lit comme une table de vérité. Utilisez-le.
- **Signaux différents, logique prioritaire** (reset bat enable, qui bat les données) : `when/else`. C'est l'ajustement naturel.
- **Sortie de synthèse :** sur un FPGA moderne, les deux produisent à peu près les mêmes portes, donc choisir la forme la plus claire au niveau source est presque toujours le bon réflexe. Ne recourez à `when/else` que quand vous voulez vraiment une priorité ; sinon `with/select` signale « pas de priorité » au prochain lecteur.

## Le piège du verrou (aperçu)

Les deux formes exigent que `y` finisse affecté dans **chaque** scénario. Une forme concurrente `when/else` ou `with/select` ne peut en réalité pas laisser `y` non affecté — la syntaxe impose un `else` final / un `when others`. Mais l'équivalent **if/case à l'intérieur d'un process**, lui, le peut, et *c'est là* que le synthétiseur infère un verrou pour « se souvenir » de la dernière valeur, ce qui n'est presque jamais ce que vous voulez. C'est le même piège du verrou que vous affronterez de plein fouet dans l'exercice de correction du module séquentiel, alors gardez l'habitude : affectez la sortie sur chaque chemin.

## Un petit raccord travaillé : un comparateur 2 bits

Le comparateur de magnitude du cours sort tout droit de `when/else` :

```vhdl
-- gt/eq/lt sont des sorties std_logic uniques
gt <= '1' when unsigned(a) > unsigned(b) else '0';
eq <= '1' when a = b else '0';
lt <= '1' when unsigned(a) < unsigned(b) else '0';
```

Trois instructions concurrentes indépendantes, trois comparateurs, tous vivants en même temps.

## Où ça apparaît ensuite

Vous utiliserez `with/select` dans l'exemple du décodeur 3 vers 8 juste après cette leçon, et `when/else` tout au long du travail sur l'additionneur, le comparateur et le mux. Les deux sont du VHDL de tous les jours.

## À retenir

- `when/else` = conditionnelle, priorisée, signaux différents autorisés → mux prioritaire.
- `with/select` = un seul sélecteur, choix séparés par des virgules, `when others` obligatoire → mux équilibré.
- Chaque branche de valeur doit correspondre au type et à la largeur de la cible.
- Les formes concurrentes ne peuvent pas abandonner la sortie, mais les versions `if`/`case` à base de process le peuvent — et c'est là que les verrous s'invitent.
