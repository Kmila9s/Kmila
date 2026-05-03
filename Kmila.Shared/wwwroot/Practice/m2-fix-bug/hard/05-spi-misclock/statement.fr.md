## SPI — corrigez le front d'échantillonnage

Voici un générateur d'horloge SPI maître minimal. Il divise `clk`
par 8 pour produire `sclk` (bascule aux comptes 3 et 7) et émet
une impulsion `sample` d'un cycle quand `miso` doit être capturé.
Le bug : `sample` est actuellement activé sur le front **montant**
de `sclk`, mais le SPI mode 0 impose que l'esclave pilote `miso`
sur montant et que le maître échantillonne sur **descendant**.

### Spécification

* `sclk` bascule : monte à `divider = 3`, descend à `divider = 7`.
* `sample` est une impulsion d'un cycle égale à `miso` **au front
  descendant de `sclk`**.
* Reset synchrone actif-haut remet tout à 0.

### Indice

Déplacez l'affectation `sample_int <= miso;` de la branche
`divider = 3` à la branche `divider = 7`.
