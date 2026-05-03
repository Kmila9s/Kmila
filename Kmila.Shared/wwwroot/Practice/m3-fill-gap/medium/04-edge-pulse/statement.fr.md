## Impulsion sur front montant — remplis le trou

Un échantillonneur à deux étages est câblé : à chaque cycle, les
échantillons précédent et courant de `d` sont stockés dans `d_prev` et
`d_curr`. Le reset du registre est aussi en place.

Tu dois produire `pulse` : il doit valoir `'1'` pendant **exactement un
cycle** d'horloge à chaque transition de `d` du bas vers le haut, et
`'0'` sinon.

### Spécification

* `pulse` est une fonction combinatoire de `d_curr` et `d_prev`.
* `pulse = '1'` ssi `d_curr = '1'` et `d_prev = '0'`.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le processus
d'échantillonnage et le reset sont verrouillés.

### Astuce

`pulse <= d_curr and (not d_prev);`
