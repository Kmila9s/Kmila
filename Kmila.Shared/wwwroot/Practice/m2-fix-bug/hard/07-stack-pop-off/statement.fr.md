## Pile — corrigez le décalage d'un cycle sur `empty`

Une pile simple rapporte son état `empty` avec un cycle de retard.
Après un push, `empty` reste à `'1'` un cycle de trop ; après le
dernier pop, `empty` revient à `'1'` un cycle après que `count`
a déjà atteint 0. Le bloc consommateur voit un flag obsolète et
soit rate le premier élément valide, soit lit dans une pile vide.

### Spécification

* Reset synchrone actif-haut.
* `count` s'incrémente sur `push` (sans `pop`), se décrémente sur
  `pop` (sans `push`, tant que `count > 0`).
* `empty = '1'` exactement quand `count = 0`, **sans cycle de
  latence**.

### Indice

Ne registrez pas la comparaison : `empty` doit être une vue
combinatoire de `count`.
