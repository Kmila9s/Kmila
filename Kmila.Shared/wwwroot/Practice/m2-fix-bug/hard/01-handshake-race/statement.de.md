## Handshake — behebe das metastabile Race

Ein einfaches `req`/`ack`-Handshake fällt sporadisch aus. Wenn der
Sender `req` für einen einzelnen Takt anhebt (ein Glitch durch
Domänenübergang oder einen verrauschten Bus), folgt `ack` sofort,
und der Empfänger startet fälschlich eine Transaktion.

### Spezifikation

* Synchroner, high-aktiver Reset.
* `ack` darf erst aktiv werden, wenn `req` für **mindestens einen
  vollen Taktzyklus stabil auf `'1'`** beobachtet wurde.
* Ein Ein-Takt-Glitch auf `req` darf **niemals** `ack = '1'`
  erzeugen.

### Hinweis

Zwei Flipflops in Serie bilden einen kleinen "Stabilitätsfilter":
der Ausgang wird nur dann `'1'`, wenn beide Stufen bereits eine
`'1'` gespeichert haben. Das ist das kanonische Synchroniser-Muster.
