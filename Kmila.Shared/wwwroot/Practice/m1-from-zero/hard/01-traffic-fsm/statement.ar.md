## FSM لإشارة المرور

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

## Traffic-light FSM

Implement a 3-state traffic-light controller. On each rising clock edge advance through the cycle `"00"` (red) → `"01"` (green) → `"10"` (yellow) → back to `"00"`.

### Spec

- Reset is asynchronous-style with `if reset='1'` resetting `state` to `"00"`.
- One step per clock; no timing dividers — the test rig advances the FSM each clock edge.
- `state` is a 2-bit `STD_LOGIC_VECTOR`.

### Hint

Hold the state in an internal `unsigned(1 downto 0)` signal and select the next value with an `if/elsif` chain.
