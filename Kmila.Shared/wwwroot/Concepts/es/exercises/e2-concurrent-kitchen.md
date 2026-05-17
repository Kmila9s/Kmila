**Tu objetivo:** imagina cuatro cocineros en la cocina de un restaurante, cada uno en su propia estación. No se esperan entre sí — cada tarea inicia en el mismo instante. Arrastra cuatro tareas al espacio de trabajo y presiona **Ejecutar**.

**Pista — prueba este conjunto de tareas:**

1. Picar las verduras
2. Marinar la carne
3. Freír los huevos
4. Hervir la pasta

> Los bloques se ven iguales a los del ejercicio secuencial de la pizza, pero el reproductor contará una historia distinta: las cuatro líneas se resaltan juntas. Ese es el modelo de ejecución **concurrente** — mismos bloques, semántica distinta, porque estamos fuera de cualquier proceso secuencial.

Así se ven las asignaciones de señal concurrentes en el cuerpo de una `architecture` de VHDL: cada línea se dispara en el mismo ciclo delta.
