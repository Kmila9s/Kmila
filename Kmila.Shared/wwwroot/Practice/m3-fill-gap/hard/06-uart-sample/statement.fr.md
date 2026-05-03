## Échantillonnage milieu de bit UART — complète le trou

Dans un récepteur UART suréchantillonné à 16×, la décision sur
le bit se prend **au centre de la fenêtre** — c'est-à-dire
lorsque le compteur d'échantillon vaut 8. La détection de front,
le reset et la sortie `sample_now` sont déjà câblés.

Ton travail : écrire le comparateur qui produit le **strobe
d'échantillon** à partir de `bit_count`.

### Spécification

* `bit_count` est `STD_LOGIC_VECTOR(3 downto 0)` (indice 0..15).
* Mets `sample_now <= '1'` exactement lorsque `bit_count = "1000"` (8).
* Mets `sample_now <= '0'` pour toute autre valeur.
* Le bloc est dans `rising_edge(clk)`, donc la décision arrive
  un cycle plus tard.

### Indice

```vhdl
if unsigned(bit_count) = "1000" then
    sample_now <= '1';
else
    sample_now <= '0';
end if;
```

### Où écrire

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont
évaluées.
