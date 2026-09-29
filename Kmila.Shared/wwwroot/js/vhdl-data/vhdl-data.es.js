// VHDL language documentation — Español.
// Misma forma que vhdl-data.en.js.

(function () {
    window.kmilaVhdlDocs = window.kmilaVhdlDocs || {};
    window.kmilaVhdlDocs.es = {

        keywords: {
            // --- Declaraciones estructurales ---
            entity:       { summary: "Declara la interfaz externa de un módulo de hardware.", description: "Una **entity** nombra una unidad de diseño y lista sus puertos (entradas/salidas) y genéricos (parámetros). Es la vista de caja negra — una architecture aporta la implementación.", example: "entity counter is\n  port (\n    clk : in  std_logic;\n    q   : out std_logic_vector(7 downto 0)\n  );\nend entity counter;" },
            architecture: { summary: "Cuerpo de implementación de una entity.", description: "Una **architecture** aporta una implementación concreta para una entity. Una misma entity puede tener varias architectures (p. ej. `behavioral`, `rtl`, `structural`).", example: "architecture behavioral of counter is\n  signal cnt : unsigned(7 downto 0) := (others => '0');\nbegin\n  process(clk)\n  begin\n    if rising_edge(clk) then cnt <= cnt + 1; end if;\n  end process;\n  q <= std_logic_vector(cnt);\nend architecture behavioral;" },
            component:    { summary: "Copia instanciable de la interfaz de una entity.", description: "La declaración **component** permite que una architecture instancie otra entity. Es necesaria antes de la instanciación directa de entidad moderna.", example: "component fifo is\n  port ( clk : in std_logic; din : in std_logic_vector(7 downto 0) );\nend component;" },
            configuration:{ summary: "Enlaza un componente con una pareja entity/architecture.", description: "Rara vez se usa en código sintetizable; más bien en simulación cuando hay varias architectures para una entity." },
            package:      { summary: "Colección reusable de tipos, constantes y subprogramas.", description: "Un **package** agrupa declaraciones que se pueden importar en otras unidades de diseño con `use`.", example: "package my_pkg is\n  constant WIDTH : integer := 8;\n  type state_t is (idle, busy, done);\nend package my_pkg;" },
            body:         { summary: "Mitad de implementación de un package.", description: "`package body` contiene las implementaciones de los subprogramas declarados en `package`." },

            // --- Librerías y cláusulas use ---
            library:      { summary: "Hace visible una biblioteca.", description: "Importa una biblioteca VHDL. `ieee` es la que casi siempre se quiere para `std_logic_1164` y `numeric_std`.", example: "library ieee;" },
            use:          { summary: "Hace visible el contenido de un package.", description: "Importa nombres desde un package para usarlos sin cualificar.", example: "use ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;" },

            // --- Puertos y genéricos ---
            port:         { summary: "Declara los puertos de una entity (señales que cruzan el límite).", description: "Aparece dentro de una entity. Cada puerto tiene un nombre, una dirección (`in`, `out`, `inout`, `buffer`) y un tipo.", example: "port (\n  clk  : in  std_logic;\n  data : out std_logic_vector(7 downto 0)\n);" },
            generic:      { summary: "Declara parámetros en tiempo de compilación para una entity.", description: "Los genéricos son como parámetros de plantilla — quien instancia elige su valor, y el diseño escala (anchos de bus, profundidad de FIFO).", example: "generic ( WIDTH : integer := 8 );" },
            map:          { summary: "Conecta los puertos/genéricos de una instancia con las señales que la rodean.", description: "Usado en `port map (...)` y `generic map (...)` al instanciar componentes.", example: "u_counter : counter port map (\n  clk => sys_clk,\n  q   => led\n);" },
            in:           { summary: "Dirección de puerto — solo entrada.", description: "El diseño lee este puerto; no puede manejarlo." },
            out:          { summary: "Dirección de puerto — solo salida.", description: "El diseño maneja este puerto; su valor no se puede leer dentro de la misma entity (usa `buffer` o una señal interna si hace falta)." },
            inout:        { summary: "Puerto bidireccional.", description: "Maneja y lee de la misma línea física. Solo un driver puede estar activo a la vez — los demás deben estar en `'Z'` (alta impedancia)." },
            buffer:       { summary: "Salida que también se puede leer internamente.", description: "Como `out`, pero también legible dentro de la entity. Considerado anticuado — prefiere `out` con una señal interna." },

            // --- Declaración de señales ---
            signal:       { summary: "Declara una señal (cable).", description: "Una **signal** transporta valores en el tiempo. Dentro de un process, las asignaciones (`<=`) programan la actualización en lugar de aplicarla inmediatamente.", example: "signal counter : unsigned(7 downto 0) := (others => '0');" },
            variable:     { summary: "Declara una variable (local al process).", description: "Una **variable** vive dentro de un process o subprograma. A diferencia de una signal, `:=` asigna inmediatamente — útil para cálculos intermedios dentro de un process.", example: "process\n  variable tmp : integer := 0;\nbegin\n  tmp := tmp + 1;\nend process;" },
            constant:     { summary: "Declara una constante con nombre.", description: "Le da un nombre legible a un valor fijo. Puede aparecer en cualquier ámbito.", example: "constant CLOCK_HZ : integer := 50_000_000;" },
            shared:       { summary: "Variable compartida entre varios procesos.", description: "Solo tiene sentido en VHDL-2008. Úsala con cuidado — típicamente tipos protegidos o enteros." },

            // --- Sistema de tipos ---
            type:         { summary: "Define un tipo nuevo.", description: "Se usa para enumeraciones, rangos, arreglos y registros.", example: "type state_t is (idle, run, done);" },
            subtype:      { summary: "Declara una versión restringida de un tipo existente.", description: "Añade restricciones de rango o resolución sin crear un tipo fundamentalmente nuevo.", example: "subtype byte is std_logic_vector(7 downto 0);" },
            array:        { summary: "Declara un tipo arreglo.", description: "De una o varias dimensiones. Puede ser restringido (tamaño fijo) o no restringido (tamaño determinado al usarse).", example: "type memory_t is array(0 to 255) of std_logic_vector(7 downto 0);" },
            record:       { summary: "Declara un tipo compuesto con campos con nombre.", description: "Equivale a los `struct` de C en VHDL.", example: "type axi_cmd_t is record\n  addr : std_logic_vector(31 downto 0);\n  len  : unsigned(7 downto 0);\nend record;" },
            range:        { summary: "Restricción de rango para tipos escalares.", description: "Típicamente `A to B` (ascendente) o `A downto B` (descendente). También se usa en bucles for.", example: "for i in 0 to 7 loop ... end loop;" },
            downto:       { summary: "Dirección de rango descendente.", description: "`MSB downto LSB` — forma estándar de declarar índices de vector para que el bit 7 quede a la izquierda.", example: "std_logic_vector(7 downto 0)" },
            to:           { summary: "Dirección de rango ascendente.", description: "`A to B` — se usa en bucles y arreglos indexados hacia arriba.", example: "for i in 0 to 15 loop ... end loop;" },
            others:       { summary: "Comodín para valores por defecto en agregados.", description: "Asigna todas las posiciones sin nombre en un agregado.", example: "signal zeros : std_logic_vector(7 downto 0) := (others => '0');" },
            units:        { summary: "Declara las unidades de un tipo físico.", description: "Se usa al definir tipos físicos personalizados, como `time`." },
            access:       { summary: "Tipo de acceso tipo puntero.", description: "Rara vez sintetizable — se usa para estructuras enlazadas en simulación." },
            file:         { summary: "Tipo de archivo para E/S durante la simulación.", description: "No sintetizable. Común en test benches para leer archivos de estímulo." },

            // --- Sentencias secuenciales / flujo de control ---
            process:      { summary: "Bloque secuencial de sentencias.", description: "Un **process** ejecuta sus sentencias secuencialmente y vuelve a correr cuando cambia una señal de su lista de sensibilidad. El pan de cada día de la lógica secuencial.", example: "process(clk, reset)\nbegin\n  if reset = '1' then\n    q <= (others => '0');\n  elsif rising_edge(clk) then\n    q <= d;\n  end if;\nend process;" },
            if:           { summary: "Ejecución condicional.", description: "Estructura estándar `if … then … elsif … else … end if;` dentro de un process.", example: "if rising_edge(clk) then\n  q <= d;\nend if;" },
            then:         { summary: "Separa la condición del cuerpo en `if` / `elsif`.", description: "" },
            elsif:        { summary: "Rama condicional adicional en un `if`.", description: "" },
            else:         { summary: "Rama alternativa en `if` / `when`.", description: "" },
            case:         { summary: "Ramificación múltiple según un valor.", description: "Como `switch` en C. Suele ser más claro que una cadena larga de `if`/`elsif`.", example: "case state is\n  when idle => …\n  when busy => …\n  when others => null;\nend case;" },
            when:         { summary: "Selector de rama en `case`, y también en asignación condicional.", description: "", example: "q <= '1' when enable = '1' else '0';" },
            select:       { summary: "Selecciona entre alternativas en una asignación seleccionada.", description: "Sentencia concurrente `with … select`, una de las dos formas clásicas de asignación condicional.", example: "with state select\n  q <= a when idle,\n       b when busy,\n       c when others;" },
            loop:         { summary: "Construcción genérica de bucle.", description: "Puede ser incondicional (infinito), indexado con `for` o condicionado con `while`.", example: "for i in 0 to 7 loop\n  result(i) <= data(7 - i);\nend loop;" },
            for:          { summary: "Bucle indexado.", description: "Itera sobre un rango. Útil tanto en processes como en bloques `generate`.", example: "for i in 0 to 15 loop … end loop;" },
            while:        { summary: "Bucle basado en condición.", description: "Rara vez sintetizable cuando la condición depende de señales." },
            next:         { summary: "Salta a la siguiente iteración de un bucle.", description: "" },
            exit:         { summary: "Sale de un bucle.", description: "" },
            return:       { summary: "Devuelve un valor desde una función o sale de un procedimiento.", description: "" },
            wait:         { summary: "Suspende un process.", description: "`wait on <señales>` / `wait until <cond>` / `wait for <tiempo>`. Solo sintetizable en formas concretas (p. ej. `wait until rising_edge(clk)`).", example: "wait until rising_edge(clk);" },
            null:         { summary: "Sentencia nula.", description: "Marcador donde hace falta una sentencia por sintaxis." },

            // --- Concurrentes / generate ---
            begin:        { summary: "Inicio de un bloque de sentencias.", description: "Abre el cuerpo de `architecture`, `process`, `function`, `procedure`, `generate`." },
            end:          { summary: "Cierra un bloque.", description: "Empareja con `entity`, `architecture`, `process`, `if`, `case`, `loop`, etc." },
            generate:     { summary: "Crea hardware condicional o en bucle.", description: "Piénsalo como un bucle o un if en tiempo de compilación que emite estructura replicada o condicional.", example: "gen_bits : for i in 0 to 7 generate\n  q(i) <= d(7 - i);\nend generate;" },
            block:        { summary: "Región concurrente con ámbito propio.", description: "Agrupa sentencias bajo una condición de guarda. Rara vez usado en código moderno." },
            assert:       { summary: "Comprobación en tiempo de simulación.", description: "Dispara un `report` y para (o no, según severidad) si la condición es falsa. No sintetizable.", example: "assert data'length = 8 report \"ancho incorrecto\" severity error;" },
            report:       { summary: "Emite un mensaje de simulación.", description: "Suele ir acompañado de `assert`. Severidad: `note`, `warning`, `error`, `failure`." },
            severity:     { summary: "Nivel de severidad para `assert`/`report`.", description: "" },

            // --- Subprogramas ---
            function:     { summary: "Subprograma puro que devuelve un valor.", description: "Calcula un valor a partir de sus argumentos — sin efectos secundarios sobre señales.", example: "function max(a, b : integer) return integer is\nbegin\n  if a > b then return a; else return b; end if;\nend function;" },
            procedure:    { summary: "Subprograma que puede manejar señales.", description: "A diferencia de una function, un procedure puede tener parámetros `out` / `inout`." },
            impure:       { summary: "Marca una function que puede depender de estado externo.", description: "Permite leer señales declaradas fuera de su ámbito." },
            pure:         { summary: "Marca una function sin dependencias externas.", description: "Es el valor por defecto de `function` — rara vez se escribe." },
            return:       { summary: "Devuelve un valor desde una función o sale de un procedimiento.", description: "" },
            open:         { summary: "Deja un puerto sin conectar, o abre un archivo.", description: "En un port map: `algun_puerto => open;`. Para E/S de archivo: `file F : text open read_mode is ...;`" },

            // --- Operadores (palabras clave) ---
            and:          { summary: "AND lógico.", description: "Elemento a elemento en std_logic_vector / bit_vector." },
            or:           { summary: "OR lógico.", description: "Elemento a elemento en vectores." },
            not:          { summary: "NOT lógico.", description: "Negación unaria." },
            nand:         { summary: "NAND lógico.", description: "" },
            nor:          { summary: "NOR lógico.", description: "" },
            xor:          { summary: "XOR lógico.", description: "" },
            xnor:         { summary: "XNOR lógico.", description: "" },
            mod:          { summary: "Módulo.", description: "El resultado toma el signo del divisor (al revés que `rem`)." },
            rem:          { summary: "Resto.", description: "El resultado toma el signo del dividendo." },
            abs:          { summary: "Valor absoluto.", description: "" },
            sla:          { summary: "Shift left aritmético.", description: "" },
            sra:          { summary: "Shift right aritmético.", description: "" },
            sll:          { summary: "Shift left lógico.", description: "" },
            srl:          { summary: "Shift right lógico.", description: "" },
            rol:          { summary: "Rotación a la izquierda.", description: "" },
            ror:          { summary: "Rotación a la derecha.", description: "" },

            // --- Varios ---
            is:           { summary: "Palabra clave separadora.", description: "Aparece en `entity X is`, `architecture Y of X is`, `type T is`, etc." },
            of:           { summary: "Palabra clave separadora.", description: "Aparece en `architecture A of X`, `array(…) of …`." },
            all:          { summary: "Comodín.", description: "Se usa en `use x.y.all;` y en contextos de atributos / listas de sensibilidad." },
            new:          { summary: "Reserva un objeto de tipo access (puntero).", description: "Rara vez sintetizable." },
            label:        { summary: "Etiqueta una sentencia con un nombre.", description: "Útil en processes, generates y bucles para referenciarlos luego." },
            alias:        { summary: "Da un nombre alternativo a un objeto o parte de él.", description: "", example: "alias sign : std_logic is data(7);" },
            attribute:    { summary: "Etiqueta de metadatos o una consulta predefinida.", description: "Consultas como `x'length`, `x'range`, `signal'event`." },
            group:        { summary: "Agrupa elementos con nombre para aplicar atributos.", description: "Poco común." },
            bus:          { summary: "Modificador de tipo de señal obsoleto.", description: "Histórico." },
            register:     { summary: "Modificador de tipo de señal obsoleto.", description: "Histórico." },
            disconnect:   { summary: "Controla el tiempo de desconexión en señales con guarda.", description: "Histórico / poco común." },
            after:        { summary: "Especificador de retardo de transporte.", description: "`a <= b after 5 ns;` — un retardo solo de simulación." },
            reject:       { summary: "Pulso de rechazo de retardo inercial.", description: "" },
            inertial:     { summary: "Retardo inercial (por defecto).", description: "Filtra pulsos cortos." },
            transport:    { summary: "Retardo de transporte (sin filtrado).", description: "" },
            guarded:      { summary: "Calificador para un block o una asignación.", description: "Poco común." },
            linkage:      { summary: "Dirección de puerto para enlace mixto entre lenguajes.", description: "Solo relevante en flujos mixtos VHDL/Verilog." },
            postponed:    { summary: "Retrasa un process o assert hasta el final del delta cycle.", description: "Solo simulación." },
            literal:      { summary: "Palabra clave relacionada con atributos.", description: "Poco común." },
            unaffected:   { summary: "'No hacer nada' en una asignación condicional.", description: "" },
            until:        { summary: "Se usa con `wait until <condición>`.", description: "" },
            with:         { summary: "Inicia una asignación seleccionada de señal.", description: "`with expr select …`." },
            on:           { summary: "Se usa en `wait on <lista-de-señales>`.", description: "" }
        },

        types: {
            std_logic:        { summary: "Lógica IEEE de 9 valores: una línea.", description: "Valores: `'0'`, `'1'`, `'Z'` (alta impedancia), `'X'` (desconocido), `'U'` (no inicializado), `'W'`, `'L'`, `'H'`, `'-'` (don't-care). Es el tipo por defecto para señales de un bit en síntesis.", example: "signal clk : std_logic := '0';" },
            std_logic_vector: { summary: "Vector de std_logic — un bus.", description: "Se declara con un rango: `(high downto low)` es la convención estándar. Bueno para cableado; para aritmética conviértelo a `unsigned`/`signed`.", example: "signal data : std_logic_vector(7 downto 0);" },
            std_ulogic:       { summary: "Versión no resuelta de std_logic.", description: "Un solo driver. Rara vez se usa directamente." },
            std_ulogic_vector:{ summary: "Vector no resuelto de std_ulogic.", description: "" },
            bit:              { summary: "Lógica simple de 2 valores: `'0'` o `'1'`.", description: "Es anterior a std_logic. Usa std_logic en síntesis — bit no puede representar Z, X, etc." },
            bit_vector:       { summary: "Vector de `bit`.", description: "" },
            boolean:          { summary: "Tipo booleano: `true` / `false`.", description: "Se usa en condiciones; no se puede sintetizar directamente a una línea (convierte a std_logic)." },
            integer:          { summary: "Entero — al menos 32 bits con signo.", description: "Genial para contadores e índices. Restringe con un subrango (p. ej. `integer range 0 to 255`) para que la síntesis sepa cuántos bits usar." },
            natural:          { summary: "`integer range 0 to integer'high`.", description: "Entero no negativo." },
            positive:         { summary: "`integer range 1 to integer'high`.", description: "Entero estrictamente positivo." },
            real:             { summary: "Tipo de coma flotante.", description: "No sintetizable — solo simulación / constantes." },
            character:        { summary: "Carácter ASCII / ISO-8859-1.", description: "" },
            string:           { summary: "Arreglo de character.", description: "Se usa sobre todo en mensajes de `report` y E/S de archivo." },
            time:             { summary: "Tipo físico que representa tiempo.", description: "Unidades: `fs`, `ps`, `ns`, `us`, `ms`, `sec`, `min`, `hr`. Solo simulación." },
            signed:           { summary: "Vector numérico (con signo) de ieee.numeric_std.", description: "Úsalo para aritmética con signo. Convierte desde std_logic_vector con el cast `signed()`.", example: "signal x : signed(7 downto 0);" },
            unsigned:         { summary: "Vector numérico (sin signo) de ieee.numeric_std.", description: "Úsalo para aritmética sin signo. Castea con `unsigned()` / `std_logic_vector()`.", example: "signal cnt : unsigned(7 downto 0) := (others => '0');" },
            line:             { summary: "Acceso a una línea de texto — para E/S de archivo.", description: "Se usa con `readline` / `writeline` en test benches." },
            text:             { summary: "Tipo de archivo del paquete textio.", description: "" },
            severity_level:   { summary: "Nivel de severidad para `assert`/`report`.", description: "Valores: `note`, `warning`, `error`, `failure`." },
            side:             { summary: "Tipo auxiliar de textio.", description: "" },
            width:            { summary: "Tipo auxiliar de textio.", description: "" }
        },

        builtins: {
            rising_edge:    { summary: "True en una transición 0→1 de un reloj.", signature: "rising_edge(s : std_ulogic) return boolean", description: "Se prefiere a `clk'event and clk = '1'`. Funciona para std_logic y std_ulogic.", example: "if rising_edge(clk) then q <= d; end if;" },
            falling_edge:   { summary: "True en una transición 1→0.", signature: "falling_edge(s : std_ulogic) return boolean", description: "Equivalente descendente de `rising_edge`." },
            to_integer:     { summary: "Convierte signed/unsigned a integer.", signature: "to_integer(arg : unsigned) return natural", description: "De `ieee.numeric_std`. Cast común después de aritmética.", example: "variable n : integer := to_integer(unsigned(data));" },
            to_unsigned:    { summary: "Convierte integer a vector unsigned.", signature: "to_unsigned(arg : natural; size : natural) return unsigned", description: "", example: "counter <= to_unsigned(42, 8);" },
            to_signed:      { summary: "Convierte integer a vector signed.", signature: "to_signed(arg : integer; size : natural) return signed", description: "" },
            resize:         { summary: "Redimensiona un vector signed/unsigned.", signature: "resize(arg : unsigned; new_size : natural) return unsigned", description: "Rellena con ceros el unsigned y extiende el signo en el signed." },
            shift_left:     { summary: "Desplazamiento lógico a la izquierda.", signature: "shift_left(arg : unsigned; count : natural) return unsigned", description: "Rellena con ceros por la derecha." },
            shift_right:    { summary: "Desplazamiento lógico a la derecha para unsigned / aritmético para signed.", signature: "shift_right(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_left:    { summary: "Rotación circular a la izquierda.", signature: "rotate_left(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_right:   { summary: "Rotación circular a la derecha.", signature: "rotate_right(arg : unsigned; count : natural) return unsigned", description: "" },
            conv_integer:   { summary: "Conversión a integer heredada.", signature: "conv_integer(arg : std_logic_vector) return integer", description: "De `ieee.std_logic_arith`. En código nuevo prefiere `to_integer` de numeric_std." },
            conv_std_logic_vector: { summary: "Conversión a vector heredada.", signature: "conv_std_logic_vector(arg : integer; size : integer) return std_logic_vector", description: "En código nuevo prefiere `std_logic_vector(to_unsigned(...))`." },
            now:            { summary: "Tiempo actual de simulación.", signature: "now : time", description: "Solo simulación." },
            readline:       { summary: "Lee una línea de un archivo textio.", signature: "readline(file f : text; l : out line)", description: "E/S de testbench." },
            writeline:      { summary: "Escribe una línea en un archivo textio.", signature: "writeline(file f : text; l : inout line)", description: "E/S de testbench." },
            endline:        { summary: "Comprobación de fin de línea.", signature: "endline(l : inout line) return boolean", description: "Utilidad de textio." },
            endfile:        { summary: "Comprobación de fin de archivo.", signature: "endfile(file f : text) return boolean", description: "" },
            write:          { summary: "Primitiva de escritura de textio.", signature: "write(l : inout line; value : … )", description: "" },
            read:           { summary: "Primitiva de lectura de textio.", signature: "read(l : inout line; value : out … )", description: "" }
        },

        snippets: {
            entity: {
                label: "entity (esqueleto)",
                summary: "Inserta una declaración mínima de entity",
                insertText: "entity ${1:name} is\n\tport (\n\t\t${2:clk}   : in  std_logic;\n\t\t${3:reset} : in  std_logic;\n\t\t${4:q}     : out std_logic_vector(${5:7} downto 0)\n\t);\nend entity ${1:name};\n\narchitecture behavioral of ${1:name} is\nbegin\n\t$0\nend architecture behavioral;"
            },
            process_clocked: {
                label: "process (síncrono, reset asíncrono)",
                summary: "Process síncrono estándar con reset asíncrono",
                insertText: "process(${1:clk}, ${2:reset})\nbegin\n\tif ${2:reset} = '1' then\n\t\t$3 <= (others => '0');\n\telsif rising_edge(${1:clk}) then\n\t\t$0\n\tend if;\nend process;"
            },
            process_combinational: {
                label: "process (combinacional)",
                summary: "Process combinacional con lista de sensibilidad",
                insertText: "process(${1:all})\nbegin\n\t$0\nend process;"
            },
            if_else: {
                label: "if … elsif … else",
                summary: "Condicional de tres ramas",
                insertText: "if ${1:condition} then\n\t$2\nelsif ${3:other} then\n\t$4\nelse\n\t$5\nend if;"
            },
            case_stmt: {
                label: "case … when … end case",
                summary: "Esqueleto de sentencia case",
                insertText: "case ${1:expr} is\n\twhen ${2:value} => $3\n\twhen others    => $0\nend case;"
            },
            for_loop: {
                label: "for i in 0 to N-1 loop",
                summary: "Bucle indexado",
                insertText: "for ${1:i} in 0 to ${2:N}-1 loop\n\t$0\nend loop;"
            },
            signal: {
                label: "declaración de signal",
                summary: "Declara una señal",
                insertText: "signal ${1:name} : ${2:std_logic_vector}(${3:7} downto 0) := (others => '0');"
            },
            counter: {
                label: "plantilla de contador",
                summary: "Contador con wrap-around",
                insertText: "signal cnt : unsigned(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk}, ${3:reset})\nbegin\n\tif ${3:reset} = '1' then\n\t\tcnt <= (others => '0');\n\telsif rising_edge(${2:clk}) then\n\t\tcnt <= cnt + 1;\n\tend if;\nend process;\n${4:q} <= std_logic_vector(cnt);"
            },
            shift_register: {
                label: "plantilla de registro de desplazamiento",
                summary: "Shift register de N bits",
                insertText: "signal sr : std_logic_vector(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk})\nbegin\n\tif rising_edge(${2:clk}) then\n\t\tsr <= sr(${1:7}-1 downto 0) & ${3:d};\n\tend if;\nend process;\n${4:q} <= sr;"
            },
            library_ieee: {
                label: "library ieee + use",
                summary: "Importaciones IEEE estándar",
                insertText: "library ieee;\nuse ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;\n\n$0"
            }
        },

        ui: {
            declaredOnLine: "declarada en la línea",
            kindSignal:     "señal",
            kindVariable:   "variable",
            kindConstant:   "constante",
            kindPort:       "puerto",
            categoryKeyword:  "Palabra clave VHDL",
            categoryType:     "Tipo",
            categoryBuiltin:  "Función IEEE",
            categoryUser:     "Declaración del usuario",
            categorySnippet:  "Snippet",
            translationPending: "⚠ Traducción pendiente — la descripción se muestra en inglés."
        }
    };
})();
