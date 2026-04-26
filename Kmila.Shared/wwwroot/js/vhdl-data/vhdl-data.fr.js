// Documentation du langage VHDL — Français.
// Même structure que vhdl-data.en.js.

(function () {
    window.kmilaVhdlDocs = window.kmilaVhdlDocs || {};
    window.kmilaVhdlDocs.fr = {

        keywords: {
            // --- Déclarations structurelles ---
            entity:       { summary: "Déclare l'interface externe d'un module matériel.", description: "Une **entity** nomme une unité de conception et liste ses ports (entrées/sorties) et génériques (paramètres). C'est la vue boîte noire — une architecture en fournit l'implémentation.", example: "entity counter is\n  port (\n    clk : in  std_logic;\n    q   : out std_logic_vector(7 downto 0)\n  );\nend entity counter;" },
            architecture: { summary: "Corps d'implémentation d'une entity.", description: "Une **architecture** fournit une implémentation concrète pour une entity. Une même entity peut avoir plusieurs architectures (par ex. `behavioral`, `rtl`, `structural`).", example: "architecture behavioral of counter is\n  signal cnt : unsigned(7 downto 0) := (others => '0');\nbegin\n  process(clk)\n  begin\n    if rising_edge(clk) then cnt <= cnt + 1; end if;\n  end process;\n  q <= std_logic_vector(cnt);\nend architecture behavioral;" },
            component:    { summary: "Copie instanciable de l'interface d'une entity.", description: "La déclaration **component** permet à une architecture d'instancier une autre entity. Requise avant l'instanciation directe d'entité (style moderne).", example: "component fifo is\n  port ( clk : in std_logic; din : in std_logic_vector(7 downto 0) );\nend component;" },
            configuration:{ summary: "Lie un composant à un couple entity/architecture précis.", description: "Rarement utilisée en code synthétisable ; principalement en simulation lorsqu'il existe plusieurs architectures pour une même entity." },
            package:      { summary: "Collection réutilisable de types, constantes et sous-programmes.", description: "Un **package** regroupe des déclarations qui peuvent être importées dans d'autres unités via `use`.", example: "package my_pkg is\n  constant WIDTH : integer := 8;\n  type state_t is (idle, busy, done);\nend package my_pkg;" },
            body:         { summary: "Partie implémentation d'un package.", description: "`package body` contient les implémentations des sous-programmes déclarés dans `package`." },

            // --- Bibliothèques & clauses use ---
            library:      { summary: "Rend une bibliothèque visible.", description: "Importe une bibliothèque VHDL. `ieee` est presque toujours celle qu'on veut pour `std_logic_1164` et `numeric_std`.", example: "library ieee;" },
            use:          { summary: "Rend visible le contenu d'un package.", description: "Importe des noms d'un package pour les utiliser sans qualification.", example: "use ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;" },

            // --- Ports & génériques ---
            port:         { summary: "Déclare les ports d'une entity (signaux qui franchissent la frontière).", description: "Apparaît dans une entity. Chaque port a un nom, une direction (`in`, `out`, `inout`, `buffer`) et un type.", example: "port (\n  clk  : in  std_logic;\n  data : out std_logic_vector(7 downto 0)\n);" },
            generic:      { summary: "Déclare des paramètres compile-time pour une entity.", description: "Les génériques sont comme des paramètres de template — celui qui instancie choisit leur valeur, et le design s'adapte (largeurs de bus, profondeur de FIFO).", example: "generic ( WIDTH : integer := 8 );" },
            map:          { summary: "Connecte les ports/génériques d'une instance aux signaux environnants.", description: "Utilisé dans `port map (...)` et `generic map (...)` lors de l'instanciation de composants.", example: "u_counter : counter port map (\n  clk => sys_clk,\n  q   => led\n);" },
            in:           { summary: "Direction de port — entrée seulement.", description: "Le design lit ce port ; il ne peut pas le piloter." },
            out:          { summary: "Direction de port — sortie seulement.", description: "Le design pilote ce port ; sa valeur ne peut pas être relue à l'intérieur de la même entity (utilise `buffer` ou un signal interne si nécessaire)." },
            inout:        { summary: "Port bidirectionnel.", description: "Pilote et lit la même ligne physique. Un seul driver peut être actif à la fois — les autres doivent être à `'Z'` (haute impédance)." },
            buffer:       { summary: "Sortie qui peut aussi être lue en interne.", description: "Comme `out`, mais lisible aussi à l'intérieur de l'entity. Considéré comme dépassé — préfère `out` avec un signal interne." },

            // --- Déclaration de signaux ---
            signal:       { summary: "Déclare un signal (fil).", description: "Un **signal** transporte des valeurs dans le temps. Dans un process, les affectations (`<=`) planifient la mise à jour au lieu de l'appliquer immédiatement.", example: "signal counter : unsigned(7 downto 0) := (others => '0');" },
            variable:     { summary: "Déclare une variable (locale au process).", description: "Une **variable** vit à l'intérieur d'un process ou d'un sous-programme. Contrairement à un signal, `:=` affecte immédiatement — utile pour les calculs intermédiaires dans un process.", example: "process\n  variable tmp : integer := 0;\nbegin\n  tmp := tmp + 1;\nend process;" },
            constant:     { summary: "Déclare une constante nommée.", description: "Donne un nom lisible à une valeur fixe. Peut apparaître à n'importe quelle portée.", example: "constant CLOCK_HZ : integer := 50_000_000;" },
            shared:       { summary: "Variable partagée accessible à plusieurs processus.", description: "N'a de sens qu'en VHDL-2008. À utiliser avec précaution — typiquement des types protégés ou des entiers." },

            // --- Système de types ---
            type:         { summary: "Définit un nouveau type.", description: "Utilisé pour les énumérations, plages, tableaux, records.", example: "type state_t is (idle, run, done);" },
            subtype:      { summary: "Déclare une version contrainte d'un type existant.", description: "Ajoute des contraintes de plage ou de résolution sans créer un type fondamentalement nouveau.", example: "subtype byte is std_logic_vector(7 downto 0);" },
            array:        { summary: "Déclare un type tableau.", description: "Mono- ou multi-dimensionnel. Peut être contraint (taille fixe) ou non contraint (taille fixée à l'utilisation).", example: "type memory_t is array(0 to 255) of std_logic_vector(7 downto 0);" },
            record:       { summary: "Déclare un type composite avec champs nommés.", description: "L'équivalent VHDL des `struct` C.", example: "type axi_cmd_t is record\n  addr : std_logic_vector(31 downto 0);\n  len  : unsigned(7 downto 0);\nend record;" },
            range:        { summary: "Contrainte de plage pour les types scalaires.", description: "Typiquement `A to B` (croissante) ou `A downto B` (décroissante). Aussi utilisée dans les boucles for.", example: "for i in 0 to 7 loop ... end loop;" },
            downto:       { summary: "Direction de plage décroissante.", description: "`MSB downto LSB` — convention standard pour déclarer les indices de vecteur, le bit 7 à gauche.", example: "std_logic_vector(7 downto 0)" },
            to:           { summary: "Direction de plage croissante.", description: "`A to B` — utilisée dans les boucles et les tableaux indexés vers le haut.", example: "for i in 0 to 15 loop ... end loop;" },
            others:       { summary: "Joker pour les valeurs par défaut d'agrégat.", description: "Affecte toutes les positions non nommées d'un agrégat.", example: "signal zeros : std_logic_vector(7 downto 0) := (others => '0');" },
            units:        { summary: "Déclare les unités d'un type physique.", description: "Utilisé lorsqu'on définit un type physique personnalisé comme `time`." },
            access:       { summary: "Type d'accès façon pointeur.", description: "Rarement synthétisable — utilisé pour les structures chaînées en simulation." },
            file:         { summary: "Type fichier pour les E/S en simulation.", description: "Non synthétisable. Courant dans les test benches pour lire des fichiers de stimuli." },

            // --- Instructions séquentielles / contrôle de flux ---
            process:      { summary: "Bloc séquentiel d'instructions.", description: "Un **process** exécute ses instructions séquentiellement et se relance à chaque changement d'un signal de sa liste de sensibilité. La brique de base de la logique synchrone.", example: "process(clk, reset)\nbegin\n  if reset = '1' then\n    q <= (others => '0');\n  elsif rising_edge(clk) then\n    q <= d;\n  end if;\nend process;" },
            if:           { summary: "Exécution conditionnelle.", description: "Structure standard `if … then … elsif … else … end if;` à l'intérieur d'un process.", example: "if rising_edge(clk) then\n  q <= d;\nend if;" },
            then:         { summary: "Sépare la condition du corps dans `if` / `elsif`.", description: "" },
            elsif:        { summary: "Branche conditionnelle supplémentaire dans un `if`.", description: "" },
            else:         { summary: "Branche par défaut dans `if` / `when`.", description: "" },
            case:         { summary: "Branchement multiple sur une valeur.", description: "Comme `switch` en C. Souvent plus propre qu'une longue chaîne d'`if`/`elsif`.", example: "case state is\n  when idle => …\n  when busy => …\n  when others => null;\nend case;" },
            when:         { summary: "Sélecteur de branche dans `case`, aussi en affectation conditionnelle.", description: "", example: "q <= '1' when enable = '1' else '0';" },
            select:       { summary: "Sélectionne parmi des alternatives dans une affectation sélective.", description: "Instruction concurrente `with … select`, l'une des deux formes conditionnelles concurrentes classiques.", example: "with state select\n  q <= a when idle,\n       b when busy,\n       c when others;" },
            loop:         { summary: "Construction de boucle générique.", description: "Peut être inconditionnelle (infinie), indexée par `for` ou conditionnée par `while`.", example: "for i in 0 to 7 loop\n  result(i) <= data(7 - i);\nend loop;" },
            for:          { summary: "Boucle indexée.", description: "Itère sur une plage. Utile aussi bien dans les processes que dans les blocs `generate`.", example: "for i in 0 to 15 loop … end loop;" },
            while:        { summary: "Boucle basée sur une condition.", description: "Rarement synthétisable lorsque la condition dépend de signaux." },
            next:         { summary: "Passe à l'itération suivante d'une boucle.", description: "" },
            exit:         { summary: "Sort d'une boucle.", description: "" },
            return:       { summary: "Renvoie une valeur depuis une fonction ou sort d'une procédure.", description: "" },
            wait:         { summary: "Suspend un process.", description: "`wait on <signaux>` / `wait until <cond>` / `wait for <temps>`. Synthétisable seulement sous certaines formes (par ex. `wait until rising_edge(clk)`).", example: "wait until rising_edge(clk);" },
            null:         { summary: "Instruction nulle.", description: "Marqueur quand la syntaxe exige une instruction." },

            // --- Concurrent / generate ---
            begin:        { summary: "Début d'un bloc d'instructions.", description: "Ouvre le corps d'`architecture`, `process`, `function`, `procedure`, `generate`." },
            end:          { summary: "Ferme un bloc.", description: "Va de pair avec `entity`, `architecture`, `process`, `if`, `case`, `loop`, etc." },
            generate:     { summary: "Crée du matériel conditionnel ou en boucle.", description: "Vois-le comme une boucle for ou un if à la compilation, qui émet une structure répliquée ou conditionnelle.", example: "gen_bits : for i in 0 to 7 generate\n  q(i) <= d(7 - i);\nend generate;" },
            block:        { summary: "Région concurrente avec portée propre.", description: "Regroupe des instructions sous une condition de garde. Peu utilisée en code moderne." },
            assert:       { summary: "Vérification en temps de simulation.", description: "Déclenche un `report` et arrête (ou non, selon la sévérité) si la condition est fausse. Non synthétisable.", example: "assert data'length = 8 report \"largeur incorrecte\" severity error;" },
            report:       { summary: "Émet un message de simulation.", description: "Souvent associé à `assert`. Sévérités : `note`, `warning`, `error`, `failure`." },
            severity:     { summary: "Niveau de sévérité pour `assert`/`report`.", description: "" },

            // --- Sous-programmes ---
            function:     { summary: "Sous-programme pur renvoyant une valeur.", description: "Calcule une valeur à partir de ses arguments — aucun effet de bord sur les signaux.", example: "function max(a, b : integer) return integer is\nbegin\n  if a > b then return a; else return b; end if;\nend function;" },
            procedure:    { summary: "Sous-programme capable de piloter des signaux.", description: "Contrairement à une function, une procedure peut avoir des paramètres `out`/`inout`." },
            impure:       { summary: "Marque une function pouvant dépendre d'un état externe.", description: "Permet de lire des signaux déclarés en dehors de sa portée." },
            pure:         { summary: "Marque une function sans dépendances externes.", description: "Valeur par défaut de `function` — rarement écrite." },
            open:         { summary: "Laisse un port non connecté ou ouvre un fichier.", description: "Dans une port map : `some_port => open;`. Pour les E/S fichier : `file F : text open read_mode is ...;`" },

            // --- Opérateurs (mots-clés) ---
            and:          { summary: "ET logique.", description: "Bit à bit sur std_logic_vector / bit_vector." },
            or:           { summary: "OU logique.", description: "Bit à bit sur les vecteurs." },
            not:          { summary: "NON logique.", description: "Négation unaire." },
            nand:         { summary: "NON-ET logique.", description: "" },
            nor:          { summary: "NON-OU logique.", description: "" },
            xor:          { summary: "OU-exclusif logique.", description: "" },
            xnor:         { summary: "NON-OU-exclusif logique.", description: "" },
            mod:          { summary: "Modulo.", description: "Le résultat a le signe du diviseur (contrairement à `rem`)." },
            rem:          { summary: "Reste.", description: "Le résultat a le signe du dividende." },
            abs:          { summary: "Valeur absolue.", description: "" },
            sla:          { summary: "Décalage arithmétique à gauche.", description: "" },
            sra:          { summary: "Décalage arithmétique à droite.", description: "" },
            sll:          { summary: "Décalage logique à gauche.", description: "" },
            srl:          { summary: "Décalage logique à droite.", description: "" },
            rol:          { summary: "Rotation à gauche.", description: "" },
            ror:          { summary: "Rotation à droite.", description: "" },

            // --- Divers ---
            is:           { summary: "Mot-clé séparateur.", description: "Apparaît dans `entity X is`, `architecture Y of X is`, `type T is`, etc." },
            of:           { summary: "Mot-clé séparateur.", description: "Apparaît dans `architecture A of X`, `array(…) of …`." },
            all:          { summary: "Joker.", description: "Utilisé dans `use x.y.all;` et dans des contextes d'attribut/liste de sensibilité." },
            new:          { summary: "Alloue un objet de type access (pointeur).", description: "Rarement synthétisable." },
            label:        { summary: "Étiquette une instruction.", description: "Utile pour les processes, generates et boucles afin de pouvoir y faire référence." },
            alias:        { summary: "Donne un nom alternatif à un objet ou à une partie de celui-ci.", description: "", example: "alias sign : std_logic is data(7);" },
            attribute:    { summary: "Étiquette de métadonnée ou requête prédéfinie.", description: "Requêtes comme `x'length`, `x'range`, `signal'event`." },
            group:        { summary: "Regroupe des éléments nommés pour l'application d'attributs.", description: "Rarement vu." },
            bus:          { summary: "Modificateur de signal obsolète.", description: "Historique." },
            register:     { summary: "Modificateur de signal obsolète.", description: "Historique." },
            disconnect:   { summary: "Contrôle le délai de déconnexion sur les signaux gardés.", description: "Historique / rare." },
            after:        { summary: "Spécificateur de délai de transport.", description: "`a <= b after 5 ns;` — un délai purement de simulation." },
            reject:       { summary: "Impulsion de rejet de délai inertiel.", description: "" },
            inertial:     { summary: "Délai inertiel (par défaut).", description: "Filtre les impulsions courtes." },
            transport:    { summary: "Délai de transport (sans filtrage).", description: "" },
            guarded:      { summary: "Qualifie un block ou une affectation de signal.", description: "Rare." },
            linkage:      { summary: "Direction de port pour le lien multi-langage.", description: "Pertinent uniquement dans les flux mixtes VHDL/Verilog." },
            postponed:    { summary: "Reporte un process ou une assertion à la fin du delta cycle.", description: "Simulation seulement." },
            literal:      { summary: "Mot-clé lié aux attributs.", description: "Rare." },
            unaffected:   { summary: "Marqueur 'ne rien faire' dans une affectation conditionnelle.", description: "" },
            until:        { summary: "Utilisé avec `wait until <condition>`.", description: "" },
            with:         { summary: "Démarre une affectation sélective de signal.", description: "`with expr select …`." },
            on:           { summary: "Utilisé dans `wait on <liste de signaux>`.", description: "" }
        },

        types: {
            std_logic:        { summary: "Logique IEEE 9-valeurs : un fil.", description: "Valeurs : `'0'`, `'1'`, `'Z'` (haute impédance), `'X'` (inconnu), `'U'` (non initialisé), `'W'`, `'L'`, `'H'`, `'-'` (don't-care). Le défaut pour les signaux 1 bit en synthèse.", example: "signal clk : std_logic := '0';" },
            std_logic_vector: { summary: "Vecteur de std_logic — un bus.", description: "Déclaré avec une plage : `(high downto low)` est la convention standard. Bon pour le câblage ; pour l'arithmétique, convertir en `unsigned`/`signed`.", example: "signal data : std_logic_vector(7 downto 0);" },
            std_ulogic:       { summary: "Variante non résolue de std_logic.", description: "Un seul driver autorisé. Rarement utilisé directement." },
            std_ulogic_vector:{ summary: "Vecteur non résolu de std_ulogic.", description: "" },
            bit:              { summary: "Logique 2-valeurs simple : `'0'` ou `'1'`.", description: "Antérieur à std_logic. Utilise std_logic en synthèse — bit ne peut pas représenter Z, X, etc." },
            bit_vector:       { summary: "Vecteur de `bit`.", description: "" },
            boolean:          { summary: "Type booléen : `true` / `false`.", description: "Utilisé dans les conditions ; non synthétisable directement sur un fil (convertir en std_logic)." },
            integer:          { summary: "Entier — au minimum 32 bits signé.", description: "Idéal pour les compteurs et indices. Contrains avec un sous-intervalle (par ex. `integer range 0 to 255`) pour que la synthèse connaisse la largeur en bits." },
            natural:          { summary: "`integer range 0 to integer'high`.", description: "Entier non négatif." },
            positive:         { summary: "`integer range 1 to integer'high`.", description: "Entier strictement positif." },
            real:             { summary: "Type à virgule flottante.", description: "Non synthétisable — simulation/constantes uniquement." },
            character:        { summary: "Caractère ASCII / ISO-8859-1.", description: "" },
            string:           { summary: "Tableau de character.", description: "Utilisé surtout pour les messages `report` et les E/S fichier." },
            time:             { summary: "Type physique représentant le temps.", description: "Unités : `fs`, `ps`, `ns`, `us`, `ms`, `sec`, `min`, `hr`. Simulation uniquement." },
            signed:           { summary: "Vecteur numérique (signé) de ieee.numeric_std.", description: "Pour l'arithmétique signée. Conversion depuis std_logic_vector via le cast `signed()`.", example: "signal x : signed(7 downto 0);" },
            unsigned:         { summary: "Vecteur numérique (non signé) de ieee.numeric_std.", description: "Pour l'arithmétique non signée. Cast via `unsigned()` / `std_logic_vector()`.", example: "signal cnt : unsigned(7 downto 0) := (others => '0');" },
            line:             { summary: "Accès à une ligne de texte — pour les E/S fichier.", description: "Utilisé avec `readline` / `writeline` dans les test benches." },
            text:             { summary: "Type fichier du package textio.", description: "" },
            severity_level:   { summary: "Niveau de sévérité pour `assert`/`report`.", description: "Valeurs : `note`, `warning`, `error`, `failure`." },
            side:             { summary: "Type utilitaire de textio.", description: "" },
            width:            { summary: "Type utilitaire de textio.", description: "" }
        },

        builtins: {
            rising_edge:    { summary: "Vrai sur une transition 0→1 d'une horloge.", signature: "rising_edge(s : std_ulogic) return boolean", description: "Préféré à `clk'event and clk = '1'`. Fonctionne pour std_logic et std_ulogic.", example: "if rising_edge(clk) then q <= d; end if;" },
            falling_edge:   { summary: "Vrai sur une transition 1→0.", signature: "falling_edge(s : std_ulogic) return boolean", description: "Pendant de `rising_edge`." },
            to_integer:     { summary: "Convertit signed/unsigned en integer.", signature: "to_integer(arg : unsigned) return natural", description: "De `ieee.numeric_std`. Cast courant après une opération arithmétique.", example: "variable n : integer := to_integer(unsigned(data));" },
            to_unsigned:    { summary: "Convertit integer en vecteur unsigned.", signature: "to_unsigned(arg : natural; size : natural) return unsigned", description: "", example: "counter <= to_unsigned(42, 8);" },
            to_signed:      { summary: "Convertit integer en vecteur signed.", signature: "to_signed(arg : integer; size : natural) return signed", description: "" },
            resize:         { summary: "Redimensionne un vecteur signed/unsigned.", signature: "resize(arg : unsigned; new_size : natural) return unsigned", description: "Étend par zéros pour unsigned, par bit de signe pour signed." },
            shift_left:     { summary: "Décalage logique à gauche.", signature: "shift_left(arg : unsigned; count : natural) return unsigned", description: "Remplit la droite avec des zéros." },
            shift_right:    { summary: "Décalage logique à droite pour unsigned / arithmétique pour signed.", signature: "shift_right(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_left:    { summary: "Rotation circulaire à gauche.", signature: "rotate_left(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_right:   { summary: "Rotation circulaire à droite.", signature: "rotate_right(arg : unsigned; count : natural) return unsigned", description: "" },
            conv_integer:   { summary: "Conversion vers integer (legacy).", signature: "conv_integer(arg : std_logic_vector) return integer", description: "De `ieee.std_logic_arith`. Préfère `to_integer` de numeric_std en code neuf." },
            conv_std_logic_vector: { summary: "Conversion vers vecteur (legacy).", signature: "conv_std_logic_vector(arg : integer; size : integer) return std_logic_vector", description: "Préfère `std_logic_vector(to_unsigned(...))` en code neuf." },
            now:            { summary: "Temps de simulation courant.", signature: "now : time", description: "Simulation seulement." },
            readline:       { summary: "Lit une ligne d'un fichier textio.", signature: "readline(file f : text; l : out line)", description: "E/S de test bench." },
            writeline:      { summary: "Écrit une ligne dans un fichier textio.", signature: "writeline(file f : text; l : inout line)", description: "E/S de test bench." },
            endline:        { summary: "Vérification de fin de ligne.", signature: "endline(l : inout line) return boolean", description: "Utilitaire textio." },
            endfile:        { summary: "Vérification de fin de fichier.", signature: "endfile(file f : text) return boolean", description: "" },
            write:          { summary: "Primitive d'écriture textio.", signature: "write(l : inout line; value : … )", description: "" },
            read:           { summary: "Primitive de lecture textio.", signature: "read(l : inout line; value : out … )", description: "" }
        },

        snippets: {
            entity: {
                label: "entity (squelette)",
                summary: "Insérer une déclaration d'entity minimale",
                insertText: "entity ${1:name} is\n\tport (\n\t\t${2:clk}   : in  std_logic;\n\t\t${3:reset} : in  std_logic;\n\t\t${4:q}     : out std_logic_vector(${5:7} downto 0)\n\t);\nend entity ${1:name};\n\narchitecture behavioral of ${1:name} is\nbegin\n\t$0\nend architecture behavioral;"
            },
            process_clocked: {
                label: "process (synchrone, reset asynchrone)",
                summary: "Process synchrone standard avec reset asynchrone",
                insertText: "process(${1:clk}, ${2:reset})\nbegin\n\tif ${2:reset} = '1' then\n\t\t$3 <= (others => '0');\n\telsif rising_edge(${1:clk}) then\n\t\t$0\n\tend if;\nend process;"
            },
            process_combinational: {
                label: "process (combinatoire)",
                summary: "Process combinatoire avec liste de sensibilité",
                insertText: "process(${1:all})\nbegin\n\t$0\nend process;"
            },
            if_else: {
                label: "if … elsif … else",
                summary: "Conditionnelle à trois branches",
                insertText: "if ${1:condition} then\n\t$2\nelsif ${3:other} then\n\t$4\nelse\n\t$5\nend if;"
            },
            case_stmt: {
                label: "case … when … end case",
                summary: "Squelette d'instruction case",
                insertText: "case ${1:expr} is\n\twhen ${2:value} => $3\n\twhen others    => $0\nend case;"
            },
            for_loop: {
                label: "for i in 0 to N-1 loop",
                summary: "Boucle indexée",
                insertText: "for ${1:i} in 0 to ${2:N}-1 loop\n\t$0\nend loop;"
            },
            signal: {
                label: "déclaration de signal",
                summary: "Déclarer un signal",
                insertText: "signal ${1:name} : ${2:std_logic_vector}(${3:7} downto 0) := (others => '0');"
            },
            counter: {
                label: "modèle de compteur",
                summary: "Compteur avec wrap-around",
                insertText: "signal cnt : unsigned(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk}, ${3:reset})\nbegin\n\tif ${3:reset} = '1' then\n\t\tcnt <= (others => '0');\n\telsif rising_edge(${2:clk}) then\n\t\tcnt <= cnt + 1;\n\tend if;\nend process;\n${4:q} <= std_logic_vector(cnt);"
            },
            shift_register: {
                label: "modèle de registre à décalage",
                summary: "Registre à décalage N bits",
                insertText: "signal sr : std_logic_vector(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk})\nbegin\n\tif rising_edge(${2:clk}) then\n\t\tsr <= sr(${1:7}-1 downto 0) & ${3:d};\n\tend if;\nend process;\n${4:q} <= sr;"
            },
            library_ieee: {
                label: "library ieee + use",
                summary: "Imports IEEE standards",
                insertText: "library ieee;\nuse ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;\n\n$0"
            }
        },

        ui: {
            declaredOnLine: "déclaré à la ligne",
            kindSignal:     "signal",
            kindVariable:   "variable",
            kindConstant:   "constante",
            kindPort:       "port",
            categoryKeyword:  "Mot-clé VHDL",
            categoryType:     "Type",
            categoryBuiltin:  "Fonction IEEE",
            categoryUser:     "Déclaration utilisateur",
            categorySnippet:  "Extrait",
            translationPending: "⚠ Traduction en attente — description affichée en anglais."
        }
    };
})();
