# Alles zusammensetzen

Echte Designs benutzen **beides** — nebenläufigen und sequenziellen Code in derselben Entität. Der Player unten hat eine kleine Entität mit zwei Teilen:

1. Ein **Halbaddierer** — reine kombinatorische Logik, geschrieben als zwei nebenläufige Zuweisungen. Kein Takt, kein `process`.
2. Ein **4-Bit-Zähler** — sequenziell, getaktete Logik, geschrieben in einem `process` mit `rising_edge(clk)`.

Beide Teile leben Seite an Seite in derselben `architecture`. Sie verwenden unterschiedliche Ausführungsmodelle, weil sie unterschiedliche Fragen beantworten:

| Frage | Modell | Warum |
|---|---|---|
| Was ist die Summe von `a` und `b` *jetzt gerade*? | Nebenläufig | Kein Speicher, kein Takt — reine Gatter. |
| Was ist der Zählerstand *nach dem nächsten Taktschlag*? | Sequenziell | Wir brauchen ein Register, das den Zustand zwischen Taktflanken hält. |

<!-- exec-fig2:start -->
## Rohre und ein Drehkreuz

Dieselbe Architektur enthält zwei Maschinen, die unterschiedlich arbeiten:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-turnstile.html" title="Rohre des Halbaddierers und ein getaktetes Drehkreuz-Register" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- **Rohre (nebenläufig):** Wenn sich `a` und `b` ändern, erreicht das Wasser die Module XOR und AND, und die Lampen `sum` und `cout` aktualisieren sich im selben Delta — ohne Takt.
- **Drehkreuz (sequenziell):** `count <= count + 1` bereitet nur den nächsten Wert vor (die Karte `0001`, die am Eingang wartet). Er kommt erst durch — das Drehkreuz dreht sich, die Tafel klappt um — bei `rising_edge(clk)`.
<!-- exec-fig2:end -->

## Die zwei Ausführungsmodelle im Zusammenspiel

Wenn du Play drückst, siehst du:

1. Eingänge `a` und `b` ändern sich → beide Halbaddierer-Zeilen feuern **gemeinsam** in einem Delta-Zyklus.
2. Der Takt steigt → der Zähler-`process` wacht auf, und die `if rising_edge`-Wache erlaubt uns, das Inkrement zu erreichen.
3. Das `<= count + 1` des Zählers ist eine Signalzuweisung — sie committet, wenn der Delta endet.

> **Warum das in echten Designs zählt:** jede CPU, die du je benutzt hast, wurde so gebaut. Kombinatorische Logik entscheidet „was wäre die Antwort?", getaktete Register entscheiden „wann committen wir sie?". Meister diese Aufteilung und du hast das ganze mentale Modell hinter digitalem Design.

## Im Zweifelsfall: `process` oder nebenläufig?

- **Kein Takt beteiligt? Kein Speicher nötig?** Nimm nebenläufige Zuweisungen. Sie lesen sich einfacher, denken sich einfacher, und sie synthetisieren zu kombinatorischen Gattern.
- **Musst du einen Wert zwischen Taktflanken behalten?** Nimm einen `process` mit `rising_edge(clk)`-Wache. So bekommst du Register, FSMs, Zähler, Akkumulatoren — alles Zustandsbehaftete.

Der Player ist am Ende angekommen. Du hast jetzt sequenziell, nebenläufig, warum echte Parallelität nicht gilt, Fallen mit Sensitivitätslisten und die zwei Modelle im Zusammenspiel gesehen. Markier die Lektion unten als abgeschlossen — du hast das mentale Modell.
