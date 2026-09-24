## Booth multiplier — conditional add/subtract — fill the gap

A 4×4 Booth multiplier is **almost** complete. The accumulator
`acc`, the bookkeeping bits `q0` and `q_minus1`, and the
sign-extended multiplicand `m_ext` are already updated for you
on `start`. A `variable lsb_pair := q0 & q_minus1;` is computed
at the start of each step.

Your task is to write the **conditional add/subtract** that
runs on every step (when `start = '0'`).

### Specification

* Look at the two-bit pair `lsb_pair = q0 & q_minus1`:
  * `"01"` → add the multiplicand: `acc <= acc + m_ext;`
  * `"10"` → subtract the multiplicand: `acc <= acc - m_ext;`
  * `"00"` or `"11"` → no operation: `acc <= acc;`
* Use a `case lsb_pair is … end case;`.
* Do not touch `q0`, `q_minus1` or `m_ext` — they live outside
  the markers.

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
