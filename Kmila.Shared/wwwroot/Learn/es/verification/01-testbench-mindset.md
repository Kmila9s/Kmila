# La mentalidad del testbench

En cada lección hasta aquí, Kmila ha sido tu testbench — arrastrabas estímulos en la pestaña Ports, Run ejecutaba el diseño y tú mirabas la waveform. Para aprender está bien. Pero no es así como se trabaja en diseño digital profesional.

## Qué es un testbench real

Un **testbench** es otra entidad VHDL cuyo único trabajo es ejercitar un *design under test* (DUT). No tiene puertos, porque nada la está alimentando; declara el DUT como componente, conecta sus propias señales internas a los puertos del DUT y usa un **process** para recorrer secuencias de estímulos en el tiempo.

Esqueleto:

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
            report "El contador debería valer 10 a los 200 ns, se obtuvo " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

Tres ideas nuevas:

1. **`wait`** — el tiempo avanza solo cuando un proceso lo dice.
2. **`assert … report … severity`** — la auto-verificación. Convierte una waveform de mil líneas en un "pass" o "fail".
3. **Entidad sin puertos** — los testbenches son auto-contenidos; el simulador los instancia directamente.

## Por qué importa

- **Regresión**: un testbench que escribiste ayer sigue corriendo hoy. Una waveform que miraste ayer ya no existe.
- **Cobertura**: puedes aplicar patrones de estímulo que una sesión interactiva nunca alcanzará (entradas aleatorias, casos límite, frecuencias extremas).
- **Automatización**: un sistema de CI ejecuta tu testbench en cada commit. Las waveforms no caben en CI.

## Límites actuales de Kmila

El simulador de Kmila ejecuta testbenches sin problemas si los escribes como entidades normales — lo único que falta hoy es la extracción automática de pass/fail desde `assert` hacia el log de Output. Está en la hoja de ruta.

*Más lecciones vendrán — este módulo está en desarrollo.*
