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

## Pourquoi ça compte

- **Régression** : un banc de test écrit hier tourne encore aujourd'hui. Un chronogramme que vous fixiez hier est perdu.
- **Couverture** : vous pouvez pousser des motifs de stimulus qu'une session interactive n'atteindra jamais (entrées aléatoires, cas limites, fréquences extrêmes).
- **Automatisation** : un système de CI lance votre banc à chaque commit. Les chronogrammes ne rentrent pas en CI.

## Limites actuelles de Kmila

Le simulateur Kmila exécute très bien les bancs de test si vous les écrivez comme des entities ordinaires — la seule chose qui manque aujourd'hui, c'est l'extraction automatique de pass/fail depuis les `assert` vers le journal de sortie. C'est dans la roadmap.

*D'autres leçons à venir — ce module est en cours d'écriture.*
