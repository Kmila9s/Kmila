## NOR-Gatter — Bug finden und beheben

Der Autor sollte ein 2-Eingang-NOR-Gatter implementieren. Der
Ausdruck `a nor b` war fast richtig, aber an einem Operanden
hängt ein überflüssiges `not`, das die Wahrheitstabelle
zerstört.

Finde die unnötige Invertierung und entferne sie.

### Spezifikation

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  1  |
|  0  |  1  |  0  |
|  1  |  0  |  0  |
|  1  |  1  |  0  |

### Hinweis

Beide Eingänge eines NOR werden direkt verschaltet — ohne
zusätzlichen Inverter.
