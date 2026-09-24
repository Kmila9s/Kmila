## Realimentación de registro toggle — completa

El registro toggle está cableado pero falta la línea de realimentación. Cuando `en = '1'` en el flanco ascendente, `qi` debe invertirse; si no, mantiene su valor.

Implementa la línea entre `-- BEGIN_EDIT` y `-- END_EDIT`:

```vhdl
qi <= not qi;
```

El reset es asíncrono. Sólo se evalúa la línea marcada.
