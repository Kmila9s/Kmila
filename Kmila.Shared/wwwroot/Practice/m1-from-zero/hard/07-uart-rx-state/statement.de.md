## UART-Empfänger-FSM

Ein vereinfachter UART-Receiver-Controller. Die 4-Zustands-FSM läuft:

| Zustand | Code | Bedeutung               |
|:-------:|:----:|:------------------------|
| Idle    | `00` | Warte auf `rx = '0'`    |
| Start   | `01` | Startbit erfassen       |
| Data    | `10` | Datenbit abtasten       |
| Stop    | `11` | Stoppbit erfassen       |

### Spezifikation

- Aus idle nach start nur, wenn `rx = '0'`.
- Aus start/data/stop pro Takt einen Schritt weiter.
- Stop kehrt zu idle zurück.
- Reset → idle.

### Hinweis

`STD_LOGIC_VECTOR(1 downto 0)` plus `case`, fertig.
