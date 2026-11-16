**Tu meta:** con `a = 3` y `b = 5`, haz que `result` valga **16** al final del proceso. El programa inicial parece correcto —`s <= a + b; result <= s * 2`—, pero presiona **Ejecutar** y mira qué recibe `result` en realidad.

`s` es una **señal** (un buzón): su nuevo valor solo llega al terminar el delta, así que la segunda línea lee el viejo. Cambia el programa para que el valor intermedio viva en la **variable** `tmp` (una libreta), que se actualiza de inmediato.
