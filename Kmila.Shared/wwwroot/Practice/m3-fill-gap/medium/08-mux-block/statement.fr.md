## Multiplexeur 4 vers 1 — remplis tout le bloc de sélection

L'entité déclare les quatre entrées `a, b, c, d`, le sélecteur 2 bits
`sel` et la sortie `y`. L'architecture est vide hormis la zone
éditable.

Tu dois implémenter toute la sélection 4 vers 1 en une seule
affectation concurrente à `y` :

| `sel` | `y` |
|:---:|:---:|
| `00` | `a` |
| `01` | `b` |
| `10` | `c` |
| `11` | `d` |

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. L'entité est
verrouillée.

### Astuce

Chaîne trois `when … else`, le `else` final couvre `sel = "11"`.
