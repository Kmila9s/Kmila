## Muestreo a mitad de bit del UART — completa el hueco

En un receptor UART con sobremuestreo 16×, la decisión de bit
ocurre **en el centro de la ventana de bit** — es decir, cuando
el contador de muestreo vale 8. El flanco de reloj, el reset
asíncrono y el puerto de salida `sample_now` ya están cableados.

Tu tarea es escribir el comparador que genere el **strobe de
muestra** a partir del `bit_count` en curso.

### Especificación

* `bit_count` es `STD_LOGIC_VECTOR(3 downto 0)` con el índice de
  sobremuestreo 0..15.
* Pon `sample_now <= '1'` exactamente cuando `bit_count = "1000"` (8).
* Pon `sample_now <= '0'` en cualquier otro valor.
* El bloque está dentro del `rising_edge(clk)`, así que la
  decisión se ve un ciclo después.

### Pista

```vhdl
if unsigned(bit_count) = "1000" then
    sample_now <= '1';
else
    sample_now <= '0';
end if;
```

### Dónde escribir

Sólo las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT` se
evalúan.
