## Comparador mayor / igual / menor

Compara dos valores sin signo de 4 bits `a` y `b` y maneja tres banderas:

- `gt = 1` si `a > b`
- `eq = 1` si `a = b`
- `lt = 1` si `a < b`

Exactamente una bandera está en `1` en cada instante.

### Pista

Usa `IEEE.NUMERIC_STD.ALL` y la forma concurrente condicional:

```vhdl
gt <= '1' when unsigned(a) > unsigned(b) else '0';
```
