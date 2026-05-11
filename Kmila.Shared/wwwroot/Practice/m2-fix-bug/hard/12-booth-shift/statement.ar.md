## ضارب بوث — إزاحة خطأ

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

A single Booth iteration must perform an arithmetic right-shift on the accumulator. The starter uses a logical right-shift (zero-fill), so any negative accumulator value gets corrupted. Replace `unsigned`-based shift with `shift_right(signed(...), 1)`.
