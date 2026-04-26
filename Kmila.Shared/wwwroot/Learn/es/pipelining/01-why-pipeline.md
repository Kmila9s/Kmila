# Por qué segmentar (pipelining)

En una CPU **single-cycle**, cada instrucción recorre en un único ciclo de reloj todas las etapas: fetch, decode, execute, memory, writeback. El reloj tiene que ser tan lento como la etapa más lenta. Frecuencia baja, utilización pobre.

## La idea del pipeline

Rompes el camino en **etapas** separadas por registros. Cada etapa trabaja sobre una instrucción distinta al mismo tiempo:

```
ciclo:   1    2    3    4    5    6
instr1:  F    D    X    M    W
instr2:       F    D    X    M    W
instr3:            F    D    X    M    W
```

Todas las etapas trabajan en paralelo. Idealmente, una instrucción termina por ciclo — IPC ≈ 1 en lugar de 1/5.

## Lo que ganas

- **Frecuencia de reloj más alta**: cada etapa es más corta.
- **Más throughput**: una instrucción completada por ciclo en régimen estable.

## Lo que cuesta

Aparecen **hazards** (conflictos):

1. **Data hazard** — la instrucción 2 necesita el resultado de la instrucción 1 antes de que llegue a writeback. Se resuelve con *forwarding* o con *stalls*.
2. **Control hazard** — un salto desvía el flujo, pero las etapas ya tienen instrucciones especulativas cargadas. Se resuelve con *flush* o *branch prediction*.
3. **Structural hazard** — dos etapas quieren usar el mismo recurso (por ejemplo, un único puerto de memoria). Se resuelve duplicando el recurso o serializando.

## Por qué esto importa en FPGA

Los diseños combinacionales profundos no alcanzan frecuencias altas. Insertar registros (segmentar) es la herramienta número uno para cerrar timing. No es solo una técnica de CPUs: cualquier pipeline de procesamiento — filtros FIR, codificadores de vídeo, aceleradores ML — aplica exactamente el mismo principio.

*Más lecciones vendrán — este módulo está en desarrollo.*
