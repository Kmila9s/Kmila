## Buffer tri-estado — completa

Implementa un buffer tri-estado: cuando `en = '1'`, salida `a`; en otro caso, `'Z'` (alta impedancia).

```vhdl
y <= a when en = '1' else 'Z';
```

Sólo se evalúa la línea entre los marcadores.
