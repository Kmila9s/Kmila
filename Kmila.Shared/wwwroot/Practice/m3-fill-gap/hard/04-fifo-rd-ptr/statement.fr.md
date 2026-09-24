## Pointeur de lecture FIFO — complète le trou

Le squelette d'une petite FIFO a déjà son **pointeur d'écriture**,
son **drapeau vide** et son **reset** câblés. Le signal vide est
calculé : `is_empty <= '1' when wr_ptr = rd_ptr else '0';`.

Ta tâche : faire avancer correctement le **pointeur de lecture**.
Il ne doit s'incrémenter que si l'hôte émet `pop = '1'` **et**
si la FIFO **n'est pas vide** (pour ne jamais dépiler à vide).

### Spécification

* Dans la branche `rising_edge(clk)` existante, écris :
  ```vhdl
  if pop = '1' and is_empty = '0' then
      rd_ptr <= rd_ptr + 1;
  end if;
  ```
* Ne touche pas à `wr_ptr` ni à `is_empty`.
* Les pops reçus à vide doivent être **ignorés silencieusement**.

### Où écrire

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont
évaluées.
