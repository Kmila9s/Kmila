## FSM de feu tricolore

Implémente un contrôleur de feu à 3 états. À chaque front montant, avance dans le cycle `"00"` (rouge) → `"01"` (vert) → `"10"` (jaune) → retour à `"00"`.

### Spécification

- Reset asynchrone : `if reset='1'` ramène `state` à `"00"`.
- Une étape par coup d'horloge ; pas de diviseur — le banc de test avance la FSM à chaque front.
- `state` est un `STD_LOGIC_VECTOR` de 2 bits.

### Indice

Stocke l'état dans un signal `unsigned(1 downto 0)` et choisis le prochain via `if/elsif`.
