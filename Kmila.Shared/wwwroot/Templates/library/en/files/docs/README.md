## {{PROJECT_NAME}} library

A reusable VHDL package. Two files because that's the VHDL convention:

- `pkg.vhd` — the **declaration**: constants, types, function signatures.
- `pkg_body.vhd` — the **body**: implementations for everything declared above.
- `docs/README.md` — this file.

The package name is `{{PROJECT_IDENT}}`. After you import this project as a
library (via the gallery's auto-import checkbox, or Settings → Libraries),
other projects can use it:

```vhdl
use {{PROJECT_IDENT}}.all;

-- inside an architecture:
signal s : data_bus_t;
s <= sat_add(x"F0", x"20");  -- saturates to x"FF"
```

## Publishing this library

When you created this project with the "Library" template and ticked
**Add to Kmila libraries**, Kmila already concatenated `pkg.vhd` + `pkg_body.vhd`
and registered the package under its declared name. Other projects in the
same Kmila install will see it on their next build.

If you didn't tick the checkbox, you can publish later: open
**Settings → Libraries** and import the combined `.vhd` manually.
