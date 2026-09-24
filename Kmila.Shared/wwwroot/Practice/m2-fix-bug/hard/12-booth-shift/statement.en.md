## Booth multiplier — wrong shift

A single Booth iteration must perform an **arithmetic** right-shift on the accumulator (sign-extension into the MSB). The starter uses a **logical** right-shift (zero-fill), so any negative accumulator value gets corrupted.

Find the line that converts to `unsigned` for the shift and replace it with `shift_right(signed(...), 1)` so the sign bit propagates.
