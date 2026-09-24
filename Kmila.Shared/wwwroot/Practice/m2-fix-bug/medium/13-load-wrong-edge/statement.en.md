## Loadable register — wrong clock edge

A 4-bit register with synchronous load was specified.

### Specification

* Async active-high `reset` clears `q` to `"0000"`.
* On `rising_edge(clk)`, if `load = '1'` then `q <- d`. Otherwise `q` holds.

### What's wrong

The register is clocked on the **falling** edge instead of the rising edge. Inputs sampled on rising edges look right by coincidence, but the data appears one half-cycle late and the timing model is wrong.

### Hint

`falling_edge(clk)` and `rising_edge(clk)` are mirror images. Check which one the process uses.
