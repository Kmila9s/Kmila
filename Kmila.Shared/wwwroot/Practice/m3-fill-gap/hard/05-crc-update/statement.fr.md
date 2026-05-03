## Étape CRC-8 — complète le trou

Un registre CRC-8 série est **presque** complet. Le registre 8
bits `r`, la constante polynôme `POLY = x"07"`, le reset
asynchrone et la sortie `crc <= r;` sont déjà là. À l'intérieur
du process, la variable `fb : STD_LOGIC` est déjà déclarée.

Ton travail : écrire l'**étape CRC sur un bit** exécutée à
chaque front d'horloge.

### Spécification

* Calcule le bit de retour :
  ```vhdl
  fb := r(7) xor din;
  ```
* Si `fb = '1'`, décale `r` à gauche (insère `'0'` au LSB) puis
  XOR avec `POLY`. Sinon décale seulement :
  ```vhdl
  if fb = '1' then
      r <= (r(6 downto 0) & '0') xor POLY;
  else
      r <= r(6 downto 0) & '0';
  end if;
  ```

### Où écrire

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont
évaluées.
