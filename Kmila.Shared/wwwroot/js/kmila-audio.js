// Kmila audio subsystem — thin wrapper over the Web Audio API used by
// the buzzer widget (FW-5). One shared AudioContext plus one oscillator
// + gain node per widget instance. Vendored per offline-first principle;
// no CDN reference.
//
// Public surface (kept small on purpose):
//   window.kmilaAudio.unlock()               — resume the context. MUST be
//                                              called from a user gesture
//                                              (button press / switch flip
//                                              / lever tap). Idempotent.
//   window.kmilaAudio.setTone(id, hz, gain)  — start / retune the oscillator
//                                              for widget `id`. `hz > 0` and
//                                              `gain > 0` required.
//   window.kmilaAudio.stop(id)               — silence voice `id` (ramp to 0).
//   window.kmilaAudio.stopAll()              — silence every voice at once.
//                                              Called when the sim stops or
//                                              the tab is hidden so no buzzer
//                                              keeps droning after the run.
//   window.kmilaAudio.isReady()              — true iff the context is
//                                              running (unlock succeeded).
//
// iOS Safari + Android WebView both suspend AudioContext until the first
// user gesture. Widgets call setTone eagerly; if the context is still
// suspended the browser silently drops the audio and the widget's visual
// pulse still plays. The next user gesture that triggers unlock() will
// resume playback on the next setTone call.

(function () {
  if (window.kmilaAudio) return;

  let ctx = null;
  const voices = new Map(); // id → { osc: OscillatorNode, gain: GainNode }

  function ensureCtx() {
    if (ctx) return ctx;
    try {
      const AC = window.AudioContext || window.webkitAudioContext;
      if (!AC) return null;
      ctx = new AC();
    } catch (e) {
      console.warn('[kmilaAudio] failed to create AudioContext', e);
      ctx = null;
    }
    return ctx;
  }

  function clampGain(g) {
    if (typeof g !== 'number' || !isFinite(g)) return 0.0;
    // Cap at 0.15 so a page full of buzzers can't nuke the user's ears.
    return Math.max(0, Math.min(0.15, g));
  }

  window.kmilaAudio = {
    unlock() {
      const c = ensureCtx();
      if (!c) return;
      // resume() is safe to call repeatedly; on Suspended → Running it
      // returns a promise, on already-running it resolves immediately.
      try { c.resume(); } catch (e) { /* nothing to do */ }
    },

    isReady() {
      return ctx != null && ctx.state === 'running';
    },

    setTone(id, hz, gain) {
      const c = ensureCtx();
      if (!c) return;
      if (typeof hz !== 'number' || !isFinite(hz) || hz <= 0) {
        this.stop(id);
        return;
      }
      const g = clampGain(gain);
      let v = voices.get(id);
      if (!v) {
        try {
          v = {
            osc: c.createOscillator(),
            gain: c.createGain(),
          };
          v.osc.type = 'sine';
          v.gain.gain.value = 0;
          v.osc.connect(v.gain).connect(c.destination);
          v.osc.start();
          voices.set(id, v);
        } catch (e) {
          console.warn('[kmilaAudio] failed to allocate voice', id, e);
          return;
        }
      }
      // Frequency ramp keeps retuning from clicking; short ramp both
      // masks the switching artifact and stays perceptually instant.
      const now = c.currentTime;
      try {
        v.osc.frequency.cancelScheduledValues(now);
        v.osc.frequency.setTargetAtTime(hz, now, 0.005);
        v.gain.gain.cancelScheduledValues(now);
        v.gain.gain.setTargetAtTime(g, now, 0.010);
      } catch (e) { /* deprecated methods, silently ignore */ }
    },

    stop(id) {
      const v = voices.get(id);
      if (!v || !ctx) return;
      const now = ctx.currentTime;
      try {
        v.gain.gain.cancelScheduledValues(now);
        v.gain.gain.setTargetAtTime(0, now, 0.010);
      } catch (e) { /* nothing */ }
      // Leave the oscillator running with silent gain; disposing on
      // every stop would churn AudioNodes when the design pulses.
    },

    // Silence every allocated voice — the run-lifecycle safety net so a
    // stopped/backgrounded sim can't leave a buzzer sounding. Voices stay
    // allocated (silent gain); the next setTone re-arms them cheaply.
    stopAll() {
      voices.forEach((_v, id) => this.stop(id));
    },

    dispose(id) {
      const v = voices.get(id);
      if (!v) return;
      try { v.osc.stop(); } catch (e) {}
      try { v.osc.disconnect(); v.gain.disconnect(); } catch (e) {}
      voices.delete(id);
    },
  };
})();
