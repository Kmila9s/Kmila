## Handshake — corrigez la course métastable

Un simple handshake `req`/`ack` tombe en panne par intermittence.
Lorsque le bloc émetteur place `req` à `1` pendant un seul cycle (un
glitch dû à un passage de domaines d'horloge ou à un bus bruité),
`ack` le suit immédiatement et le bloc récepteur démarre à tort une
transaction.

### Spécification

* Reset synchrone actif-haut.
* `ack` ne peut s'activer que si `req` est resté **stable à 1
  pendant au moins un cycle d'horloge complet**.
* Un glitch d'un seul cycle sur `req` ne doit **jamais** produire
  `ack = '1'`.

### Indice

Deux bascules en série forment un petit "filtre de stabilité" :
la sortie passe à `1` uniquement lorsque les deux étages ont déjà
capturé un `'1'`. C'est le motif canonique du synchroniseur.
