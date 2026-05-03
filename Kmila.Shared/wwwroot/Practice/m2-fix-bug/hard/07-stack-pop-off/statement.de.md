## Stack — repariere das Off-by-One auf `empty`

Ein einfacher Stack meldet seinen `empty`-Zustand einen Takt
verspätet. Nach einem Push bleibt `empty` einen Takt zu lange auf
`'1'`; nach dem letzten Pop wird `empty` einen Takt nach Erreichen
von `count = 0` wieder `'1'`. Der Verbraucher sieht ein veraltetes
Flag und verpasst entweder das erste gültige Element oder liest
aus einem leeren Stack.

### Spezifikation

* Synchroner, high-aktiver Reset.
* `count` zählt bei `push` (und nicht `pop`) hoch, bei `pop`
  (und nicht `push`, solange `count > 0`) runter.
* `empty = '1'` genau dann, wenn `count = 0`, **ohne einen
  Takt Latenz**.

### Hinweis

Den Vergleich nicht registrieren: `empty` soll eine reine
kombinatorische Sicht auf `count` sein.
