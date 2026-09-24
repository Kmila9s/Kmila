## Arbitre round-robin — rotation de grant

Un arbitre round-robin à 4 entrées suit `last_grant` (index du dernier accordé). À chaque cycle il accorde au prochain demandeur après `last_grant` dans l'ordre cyclique. Sans `req` actif, aucun grant.

Implémente le `case` qui pilote `nxt_grant` selon `last_grant` et `req`. La machine d'état et le registre `last_grant` sont déjà câblés hors marqueurs.

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont évaluées.
