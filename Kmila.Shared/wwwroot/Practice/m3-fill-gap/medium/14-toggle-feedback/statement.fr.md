## Boucle de retour du registre toggle — compléter

Le registre toggle est câblé mais la ligne de rétroaction manque. Lorsque `en = '1'` sur un front montant, `qi` doit s'inverser ; sinon il maintient sa valeur.

Implémente la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` :

```vhdl
qi <= not qi;
```

Le reset est asynchrone. Seule la ligne marquée est évaluée.
