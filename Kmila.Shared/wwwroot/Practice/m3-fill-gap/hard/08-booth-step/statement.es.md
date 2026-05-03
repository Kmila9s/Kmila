## Multiplicador Booth — suma/resta condicional — completa el hueco

Un multiplicador Booth 4×4 está **casi** completo. El acumulador
`acc`, los bits de control `q0` y `q_minus1`, y el multiplicando
extendido a 8 bits `m_ext` ya se actualizan al recibir `start`.
Al inicio de cada paso se calcula la variable
`lsb_pair := q0 & q_minus1;`.

Tu tarea es escribir la **suma/resta condicional** del paso
(cuando `start = '0'`).

### Especificación

* Mira el par de bits `lsb_pair = q0 & q_minus1`:
  * `"01"` → suma el multiplicando: `acc <= acc + m_ext;`
  * `"10"` → resta el multiplicando: `acc <= acc - m_ext;`
  * `"00"` o `"11"` → no operación: `acc <= acc;`
* Usa `case lsb_pair is … end case;`.
* No toques `q0`, `q_minus1` ni `m_ext` — están fuera de los
  marcadores.

### Dónde escribir

Sólo las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúan.
