## Tri-State-Buffer — füllen

Implementiere einen Tri-State-Buffer: bei `en = '1'` Ausgabe `a`, sonst `'Z'` (hochohmig).

```vhdl
y <= a when en = '1' else 'Z';
```

Nur die Zeile zwischen den Markierungen wird bewertet.
