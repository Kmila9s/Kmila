# L'état d'esprit testbench

Dans toutes les leçons jusqu'ici, Kmila a été ton testbench — tu as glissé des stimuli dans l'onglet Ports, Run a exécuté le design, et tu as examiné la forme d'onde à l'œil. C'est bien pour apprendre. Ce n'est pas comme ça que fonctionne le design numérique professionnel.

## Ce qu'est un vrai testbench

Un **testbench** est une autre entité VHDL dont le seul rôle est d'exercer un *design under test* (DUT). Il n'a pas de ports, puisque rien ne le pilote ; il déclare le DUT comme composant, câble ses propres signaux internes sur les ports du DUT, puis utilise un **process** pour parcourir des séquences de stimuli dans le temps.

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
            report "Counter should be 10 at 200 ns, got " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

Trois nouvelles idées :

1. **`wait`** — le temps ne passe que lorsqu'un process le dit.
2. **`assert … report … severity`** — la vérification automatique. Transforme une forme d'onde de 1000 lignes en « pass » ou « fail ».
3. **Entité sans ports** — les testbenches sont autonomes ; le simulateur les instancie directement.

## Pourquoi ça compte

- **Régression** : un testbench que tu as écrit hier tourne encore aujourd'hui. Une forme d'onde que tu as regardée hier est partie.
- **Couverture** : tu peux piloter des patterns de stimuli qu'une session interactive n'atteint jamais (entrées aléatoires, cas limites, fréquences de coin).
- **Automatisation** : un système de CI lance ton testbench à chaque commit. Les formes d'onde ne rentrent pas dans la CI.

## Limites actuelles de Kmila

Le simulateur Kmila exécute les testbenches sans problème si tu les écris comme des entités ordinaires — la seule chose qui manque aujourd'hui est l'extraction automatique pass/fail des instructions `assert` vers le log Output. C'est dans la roadmap.

*D'autres leçons à venir — ce module est en cours de rédaction active.*
