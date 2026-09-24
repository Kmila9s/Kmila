## Pop de pile avec garde de vide — complète le trou

Un pointeur de pile simple est **presque** prêt. Le reset, la
branche **push** et le drapeau combinatoire `empty` sont déjà
câblés.

Ta tâche : écrire la branche **pop** avec une garde anti-
underflow.

### Spécification

* Dans `rising_edge(clk)`, après la branche push :
  ```vhdl
  if pop = '1' and sp /= 0 then
      sp <= sp - 1;
  end if;
  ```
* Les pops à pile vide doivent être **ignorés silencieusement**.
* Ne touche pas à la branche push ni au flag empty.

### Où écrire

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont
évaluées.
