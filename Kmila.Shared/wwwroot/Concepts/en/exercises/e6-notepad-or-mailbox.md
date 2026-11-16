**Your goal:** with `a = 3` and `b = 5`, make `result` equal **16** at the end of the process. The starting program looks right — `s <= a + b; result <= s * 2` — but press **Run** and see what `result` really gets.

`s` is a **signal** (a mailbox): its new value only arrives when the delta ends, so the second line reads the old one. Change the program so the intermediate value lives in the **variable** `tmp` (a notepad), which updates immediately.
