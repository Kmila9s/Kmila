## LFSR — corrigez le polynôme

Ce LFSR 8 bits est censé produire une séquence pseudo-aléatoire
de longueur maximale (période 255). Le code actuel utilise un
polynôme non primitif : seuls deux taps sont XORés, donc le
registre tombe vite dans un cycle court et la sortie n'est plus
aléatoire.

### Spécification

* Reset synchrone actif-haut ; valeur de reset `00000001`.
* LFSR de Fibonacci décalant à **gauche**, nouveau bit en
  position 0 : `reg <= reg(6 downto 0) & fb`.
* La rétroaction `fb` est le XOR des taps **8, 6, 5, 4** (notation
  1-indexée), soit `reg(7) xor reg(5) xor reg(4) xor reg(3)`.
  Polynôme : `x^8 + x^6 + x^5 + x^4 + 1`.

### Indice

Les polynômes à deux taps sur GF(2) ne sont presque jamais
primitifs en longueur 8. Le jeu de taps canonique pour un LFSR
8 bits maximal est `{8, 6, 5, 4}`.
