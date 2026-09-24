## FSM récepteur UART

Un contrôleur UART simplifié. La FSM 4 états enchaîne :

| État  | Code | Signification             |
|:-----:|:----:|:--------------------------|
| Idle  | `00` | Attend `rx = '0'`         |
| Start | `01` | Capture le bit de start   |
| Data  | `10` | Échantillonne un bit data |
| Stop  | `11` | Capture le bit de stop    |

### Spécification

- D'idle, passe à start uniquement si `rx = '0'`.
- De start/data/stop, avance à chaque front.
- Stop boucle vers idle.
- Reset → idle.

### Indice

`STD_LOGIC_VECTOR(1 downto 0)` + un `case`, c'est tout.
