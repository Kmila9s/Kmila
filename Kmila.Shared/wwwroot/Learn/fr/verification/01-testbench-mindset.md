# La mentalité du banc de test

Dans toutes les leçons précédentes, Kmila a été votre banc de test — vous glissiez des stimuli dans l'onglet Ports, Exécuter lançait le design, et vous jugiez le chronogramme à l'œil. Pour apprendre, c'est très bien. Ce n'est pas comme ça que le design numérique professionnel fonctionne.

## Ce qu'est un vrai banc de test

Un **banc de test** est une autre entity VHDL dont l'unique rôle est d'exercer un *design under test* (DUT). Il n'a pas de ports, car rien ne le pilote ; il déclare le DUT comme un composant, câble ses propres signaux internes vers les ports du DUT, puis utilise un **process** pour dérouler des séquences de stimulus dans le temps.

Squelette :

```vhdl
entity tb_counter is
end entity tb_counter;

architecture sim of tb_counter is
    component counter
        port ( clk, reset : in  std_logic;
               q          : out std_logic_vector(3 downto 0) );
    end component;

    signal clk   : std_logic := '0';
    signal reset : std_logic := '1';
    signal q     : std_logic_vector(3 downto 0);
begin
    dut : counter port map (clk, reset, q);

    clk <= not clk after 10 ns;            -- 50 MHz

    process
    begin
        wait for 20 ns;
        reset <= '0';
        wait for 200 ns;
        assert q = "1010"
            report "Compteur devrait être 10 à 200 ns, obtenu " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

Trois idées nouvelles :

1. **`wait`** — le temps n'avance que quand un process le dit.
2. **`assert … report … severity`** — l'auto-vérification. Transforme un chronogramme de 1000 lignes en « pass » ou « fail ».
3. **Entity sans ports** — les bancs de test sont autonomes ; le simulateur les instancie directement.

## Générer une horloge

Cette ligne unique, `clk <= not clk after 10 ns;`, est toute l'horloge : elle bascule toutes les 10 ns, ce qui donne une période de 20 ns (50 MHz). Comme `clk` a une valeur initiale de `'0'`, elle démarre proprement. Pour une horloge à rapport cyclique non symétrique ou une que vous devez arrêter plus tard, utilisez plutôt un process :

```vhdl
clk_gen : process
begin
    clk <= '0'; wait for 10 ns;
    clk <= '1'; wait for 10 ns;
    if done then wait; end if;   -- gèle quand le test est fini
end process;
```

Gardez la génération d'horloge et le stimulus dans des process **séparés**. L'horloge ne fait que tiquer ; le process de stimulus pilote les entrées et vérifie les sorties, se synchronisant d'habitude avec `wait until rising_edge(clk)` pour s'aligner sur le DUT.

## Auto-vérification : assert, severity et valeurs de référence

Tout l'intérêt d'un banc de test, c'est qu'il se vérifie *lui-même*. `assert` évalue une condition et, si elle est fausse, affiche la chaîne `report` au `severity` choisi. Les quatre niveaux de sévérité sont **note**, **warning**, **error** et **failure** — et on peut dire à la plupart des simulateurs de s'arrêter dès qu'un niveau donné se déclenche. C'est comme ça qu'on transforme « ça avait l'air bon » en un pass/fail dur.

Pour des designs non triviaux, n'écrivez pas à la main chaque valeur attendue. Calculez-la à partir d'un **modèle de référence** (golden model) — une référence simple et manifestement correcte (même un simple calcul façon logiciel dans le banc de test) — et comparez le DUT à elle à chaque cycle. Cette comparaison-à-une-référence est l'idée centrale derrière un **scoreboard**, et elle passe à l'échelle d'un compteur jusqu'à un CPU.

```vhdl
expected <= std_logic_vector(unsigned(expected) + 1) when rising_edge(clk);
assert q = expected
    report "désaccord : obtenu " & to_string(q) & " attendu " & to_string(expected)
    severity error;
```

(`to_string` pour `std_logic_vector` est une commodité de VHDL-2008 — compilez le banc de test en mode 2008 pour l'utiliser.)

## Terminer la simulation proprement

Une session uniquement chronogramme tourne indéfiniment. Un banc de test auto-vérifiant devrait **s'arrêter lui-même** quand le stimulus est épuisé. En VHDL-2008, la façon propre est `finish` du package `std.env` :

```vhdl
use std.env.all;
...
report "Tous les tests passent";
finish;          -- termine la simulation ; stop mettrait en pause pour inspection
```

`finish` termine le run (idéal pour un usage automatisé / CI) ; `stop` le met en pause pour que vous puissiez fouiller interactivement. La vieille astuce de forcer un `assert … severity failure` pour arrêter marche aussi dans n'importe quelle version de VHDL, mais elle signale un *failure* même sur un run qui passe, ce qui perturbe les outils de régression — préférez `finish`.

## Stimulus dirigé vs aléatoire

- Les tests **dirigés** frappent des cas précis, choisis à la main : comportement du reset, le rebouclage d'un compteur, une séquence délicate connue. Parfaits pour épingler un bug et pour les cas d'acceptation d'une spec.
- Les tests **aléatoires / aléatoires-contraints** jettent beaucoup d'entrées légales-mais-imprévues au DUT, attrapant des recoins que vous n'auriez jamais pensé à écrire. Ils s'appuient sur le modèle de référence, car vous ne pouvez pas prédire à la main chaque résultat.

La vraie vérification mélange le plus souvent les deux : des tests dirigés pour les cas dont vous savez qu'ils comptent, de l'aléatoire pour ceux que vous ignorez.

## Pourquoi ça compte

- **Régression :** un banc de test écrit hier tourne encore aujourd'hui. Un chronogramme que vous fixiez hier est perdu.
- **Couverture :** vous pouvez pousser des motifs de stimulus qu'une session interactive n'atteindra jamais (entrées aléatoires, cas limites, fréquences extrêmes).
- **Automatisation :** un système de CI lance votre banc à chaque commit et lit le pass/fail à la sortie. Les chronogrammes ne rentrent pas en CI.

## Liens avec le cours

La vérification est l'épine dorsale silencieuse de toute la séquence de labos *Diseño de Sistemas Digitales*. Chaque pratique — registres, systèmes séquentiels, compteurs, mémoires, machines ASM — c'est en réalité « construisez-le en HDL, puis prouvez que ça marche ». Un banc de test auto-vérifiant est la façon de transformer chaque *reporte de práctica* de « voici une capture d'écran d'un chronogramme » en « voici un test qui passe », ce qui est exactement la rigueur que vise l'évaluation par problèmes de l'unité.

## Limites actuelles de Kmila

Le simulateur Kmila exécute très bien les bancs de test si vous les écrivez comme des entities ordinaires — la seule chose qui manque aujourd'hui, c'est l'extraction automatique de pass/fail depuis les instructions `assert` vers le journal de sortie. C'est dans la roadmap.

## À retenir

- Un banc de test est une entity sans ports qui instancie le DUT, génère une horloge, pilote le stimulus et **se vérifie lui-même** avec `assert`.
- Gardez horloge et stimulus dans des process séparés ; synchronisez les vérifications avec `wait until rising_edge(clk)`.
- Comparez à un modèle de référence (un scoreboard) au lieu d'écrire à la main chaque valeur attendue.
- Terminez les runs automatisés avec `std.env.finish` ; servez-vous des niveaux de sévérité pour que le simulateur s'arrête sur les vraies défaillances.
- Mélangez tests dirigés (cas connus) et tests aléatoires (les recoins que vous ne devineriez jamais).
