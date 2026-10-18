# El reloj: el latido del circuito

El **reloj** (*clock*) es la señal que marca el ritmo de todo: una **onda cuadrada** que alterna entre 0 y 1 a **frecuencia fija**. Antes de ver compuertas y biestables, conviene verlo moverse.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/01-reloj.html" title="Reloj interactivo — una barra dentada que dibuja una onda cuadrada" loading="lazy"></iframe></div>

**Mueve el cursor** a izquierda y derecha para cambiar la frecuencia. La barra dentada es la onda hecha física: el palpador sube a cada diente (nivel **1**) y cae en cada hueco (nivel **0**), mientras el volante fija una velocidad constante. El lector muestra el nivel y la frecuencia actuales.

## Lo que de verdad importa: el flanco

La lógica secuencial —biestables, registros, contadores— no reacciona al *nivel* del reloj, sino a su **flanco**: el instante del salto de 0 a 1 (**flanco de subida**) o de 1 a 0 (**flanco de bajada**). En VHDL:

```vhdl
process (clk)
begin
    if rising_edge(clk) then
        q <= d;          -- se captura justo en el flanco de subida
    end if;
end process;
```

- **Frecuencia** — ciclos por segundo (Hz). Volante más rápido, onda más apretada.
- **Periodo** — un ciclo completo: un diente más un hueco.
- **Ciclo de trabajo** — la fracción del periodo en 1; aquí el 50 %.
