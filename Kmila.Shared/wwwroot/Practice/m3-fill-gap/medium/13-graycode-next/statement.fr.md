## Conversion binaire → Gray — remplis la conversion

Un compteur binaire 3 bits `b(2..0)` tourne déjà et se remet à zéro
sur reset. Le bus de sortie `g` est alimenté par le vecteur Gray
`gi`. Calcule `gi` à partir de `b`.

### Spécification

La conversion binaire → Gray standard est :

```
gi(2) = b(2)
gi(1) = b(2) xor b(1)
gi(0) = b(1) xor b(0)
```

Elle produit la séquence `000, 001, 011, 010, 110, 111, 101, 100, …`
où un seul bit change à chaque pas.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le compteur,
le reset et `g <= gi` sont verrouillés.

### Astuce

Trois affectations concurrentes sur les bits de `gi`, ou une seule
combinée : `b(2) & (b(2) xor b(1)) & (b(1) xor b(0))`.
