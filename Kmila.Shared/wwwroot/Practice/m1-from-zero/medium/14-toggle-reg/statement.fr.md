## Registre bascule avec enable

Construis un registre 1 bit qui bascule. Quand `en = '1'` au front montant, la sortie `q` est inversée. Quand `en = '0'`, `q` est inchangée. Un `reset` asynchrone actif haut remet `q` à `0`.

### Spécification

- Interne : `signal qi : std_logic := '0';`.
- Process : `if en = '1' then qi <= not qi;`.
- Sortie : `q <= qi;`.

### Indice

Quasiment identique à une bascule T. L'enable joue le rôle de l'entrée toggle.
