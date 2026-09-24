## Toggle-Register-Rückkopplung — füllen

Das Toggle-Register ist verdrahtet, aber die Rückkopplungszeile fehlt. Wenn `en = '1'` bei steigender Flanke ist, muss `qi` umschalten; sonst bleibt es.

Implementiere die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT`:

```vhdl
qi <= not qi;
```

Reset ist asynchron. Nur die markierte Zeile wird bewertet.
