## Buffer 3 états — compléter

Implémente un buffer 3 états : quand `en = '1'`, sortie `a` ; sinon `'Z'` (haute impédance).

```vhdl
y <= a when en = '1' else 'Z';
```

Seule la ligne entre les marqueurs est évaluée.
