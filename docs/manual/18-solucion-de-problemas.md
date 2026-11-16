# 18. Solución de problemas y errores

[← Preguntas frecuentes](17-preguntas-frecuentes.md) · [Índice](README.md)

---

Aquí tienes qué hacer ante los problemas más comunes. La regla general: cuando
algo falle, **revisa la pestaña «Salida»** (el registro de la compilación y la
simulación) y, para problemas de la app, los **Registros** (barra lateral ▸
Sistema).

---

## Errores al compilar

**Síntoma:** al pulsar **Compilar** o **Ejecutar** aparecen errores, o una
palabra queda **subrayada** en el editor.

**Qué hacer:**
1. Pasa el cursor sobre lo subrayado: el editor muestra el **mensaje y la línea**.
2. Abre la pestaña **Salida** para ver el registro completo.
3. Corrige y vuelve a compilar.

**Mensajes frecuentes (en inglés) y qué significan:**

| Mensaje | Qué quiere decir | Solución |
|---|---|---|
| `Unknown port type 'STD_LOGI'. Did you mean 'std_logic'?` | Escribiste mal un **tipo**. | Corrige el tipo (`std_logic`). |
| `missing ';'` / `expected ';'` | Falta un **punto y coma**. | Agrégalo al final de la línea. |
| `identifier 'x' not declared` | Usaste una señal/variable **no declarada**. | Decláradla (o revisa el nombre). |
| `entity 'y' not found` | No encuentra una **entidad**. | Revisa el nombre o que el archivo esté en el proyecto. |

> ✅ **Consejo** — Los mensajes técnicos van en inglés por diseño (coinciden con
> las herramientas estándar). El sentido es el de la tabla.

---

## «No pasa nada» al ejecutar / no veo ondas

- **¿Elegiste una FPGA?** Si el selector de FPGA está en **rojo**, no hay
  dispositivo; elige uno ([Cap. 10](10-entidades-y-recursos.md)).
- **¿Diste entradas?** Sin **estímulos** ni **reloj**, el diseño no cambia. Define
  los momentos en **Puertos** o usa un testbench ([Cap. 6](06-simulacion.md)).
- **¿La duración es suficiente?** Si es muy corta, no alcanzas a ver nada. Súbela.
- **¿Compiló bien?** Si hubo errores de compilación, no habrá simulación. Revisa
  **Salida** primero.

## Señales en «X» (línea punteada)

Una señal en **`X`** está **sin inicializar**. Suele resolverse aplicando un
**reset** al inicio o dando un valor inicial a tus señales internas. Es normal
ver `X` en los primeros instantes, antes del primer reset o flanco de reloj.

## La simulación va muy lenta

- Usa el modo **Rápido** en vez de **Visual** para corridas largas.
- **Reduce la duración** o sube la escala (µs/ms en vez de ns).
- Si la app se volvió lenta tras muchos cambios, prueba **Configuración ▸
  Compilación de bibliotecas ▸ Limpiar caché** ([Cap. 15](15-configuracion.md)).

---

## Problemas según la versión que uses

**La demo del navegador (WASM) «se congela» al simular.**
Es **esperado**: la página trabaja en un solo hilo mientras dura la simulación.
Espera a que termine. Para evitarlo, usa la app de escritorio o la web local.

**No veo «Control de versiones» / GitHub en la demo del navegador.**
En la demo WASM esa función **no está disponible** (restricciones del navegador).
Usa la app de escritorio o la web local ([Cap. 13](13-control-de-versiones.md)).

**La web local no abre / «el puerto está ocupado».**
Otro programa usa ese puerto (por defecto 8080). Arráncala en otro puerto:
```bash
./build.sh --native --run --port 8090
```
y abre `http://localhost:8090`.

**La app de escritorio no inicia sesión en GitHub.**
Conéctala **tú mismo** desde **Configuración ▸ Cuenta**; usa tu cuenta personal.

---

## Si nada de lo anterior ayuda

1. Abre **Registros** (barra lateral ▸ Sistema) y busca el error (hora,
   categoría, mensaje). Puedes **exportarlos**.
2. Desde **Configuración** puedes activar el **reporte de errores** o usar
   **«Reportar un problema»**.
3. Revisa que estés en la **sección correcta** del manual para esa función (usa
   el [índice](README.md)).

> 💡 **¿Sabías que…?** Guardar una **nueva versión** («Guardar como…») antes de un
> cambio grande te permite volver atrás si algo se rompe ([Cap. 3](03-proyectos.md)).

---

[← Preguntas frecuentes](17-preguntas-frecuentes.md) · [Índice](README.md)
