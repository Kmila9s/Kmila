/* Kmila Visual Runtime — canvas recording (§6 Item 1).
 *
 * Records the running Visual canvas as a webm video. Each frame is
 * painted onto an offscreen <canvas> by `kmilaCanvasRender` (defined in
 * kmila-canvas-paint.js — a bespoke pure-2D rasteriser that reads widget
 * state from CSS classes + inline custom properties). MediaRecorder
 * captures that offscreen canvas via captureStream(fps) and emits a
 * downloadable webm blob on stop.
 *
 * v1 (2026-06-…) is single-view (canvas only) with a 2-minute hard cap.
 * v2 (2026-07-28) adds an optional runbar overlay strip — a synchronized
 * timeline / speed / breakpoint band drawn under the canvas so the video
 * shows both the widget output AND the timing context. Wired via
 * `opts.withRunbar: true`; renders using 2D primitives that read DOM
 * state from `.visual-page__scrub-slider`, `.visual-page__tick-readout`,
 * `.visual-page__speed-label`, `.visual-page__bp-list` each frame. No
 * html2canvas dependency — the runbar shape is bespoke to Visual mode.
 *
 * Public API on window:
 *   kmilaCanvasRecordStart(selector, opts) → {ok, mime, sessionId, reason?}
 *   kmilaCanvasRecordStop(sessionId)       → {ok, downloaded, reason?}
 *   kmilaCanvasRecordStatus(sessionId)     → {state, elapsedMs, fps, capMs}
 *
 * opts = { fps=20, hardCapSeconds=120,
 *          codecPreference=['video/webm;codecs=vp9','video/webm'],
 *          filename='kmila-canvas.webm',
 *          withRunbar=false, runbarHeight=80 }
 */
(function () {
    function downloadBlob(blob, filename) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename || 'kmila-canvas.webm';
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 500);
    }

    // Module-level singleton — only one recording at a time.
    let session = null;
    let nextSessionId = 1;

    // Reads the runbar's live DOM state each frame. Never throws — a
    // missing selector just yields a default so the overlay paints
    // something sensible even mid-teardown or before the runbar mounts.
    function snapshotRunbar() {
        const q = (sel) => document.querySelector(sel);
        const scrub = q('.visual-page__scrub-slider');
        const scrubPct = scrub && scrub.value ? Math.max(0, Math.min(100, +scrub.value)) : 0;
        const tick = q('.visual-page__tick-readout');
        const tickText = tick ? tick.textContent.trim() : '';
        const speed = q('.visual-page__speed-label');
        const speedText = speed ? speed.textContent.trim() : '';
        const bps = [...document.querySelectorAll('.visual-page__bp-chip')]
            .map(el => {
                const t = el.textContent.trim();
                const m = t.match(/([\d.]+)/);
                return m ? parseFloat(m[1]) : NaN;
            })
            .filter(v => !isNaN(v));
        return { scrubPct, tickText, speedText, bps };
    }

    // Paints a compact runbar band across the bottom `bandH` pixels of the
    // offscreen canvas. Uses only 2D primitives (rects, text, lines) —
    // deliberately does NOT try to mirror the runbar's actual React-y layout,
    // since the recorded video is a documentary artifact, not a screenshot.
    function paintRunbar(cvs, bandH, snap) {
        const ctx = cvs.getContext('2d');
        const y0 = cvs.height - bandH;
        const w = cvs.width;
        // Band background — half-transparent scrim so the canvas above bleeds
        // through slightly, giving a floating-hud feel rather than a stark cut.
        ctx.save();
        ctx.fillStyle = 'rgba(15, 23, 42, 0.92)';
        ctx.fillRect(0, y0, w, bandH);
        ctx.strokeStyle = 'rgba(148, 163, 184, 0.35)';
        ctx.lineWidth = 1;
        ctx.beginPath(); ctx.moveTo(0, y0 + 0.5); ctx.lineTo(w, y0 + 0.5); ctx.stroke();

        // Progress bar row.
        const pad = 16;
        const trackY = y0 + Math.floor(bandH * 0.38);
        const trackH = 8;
        const trackW = w - pad * 2;
        // Track base.
        ctx.fillStyle = 'rgba(148, 163, 184, 0.25)';
        ctx.fillRect(pad, trackY, trackW, trackH);
        // Fill.
        ctx.fillStyle = 'rgba(56, 189, 248, 0.9)';
        const fillW = Math.round(trackW * (snap.scrubPct / 100));
        ctx.fillRect(pad, trackY, fillW, trackH);
        // Breakpoint tick marks along the same track.
        ctx.fillStyle = 'rgba(248, 113, 113, 0.9)';
        const maxBp = snap.bps.length ? Math.max(...snap.bps) : 0;
        if (maxBp > 0) {
            for (const bp of snap.bps) {
                const x = pad + Math.round(trackW * Math.min(1, bp / maxBp));
                ctx.fillRect(x - 1, trackY - 3, 3, trackH + 6);
            }
        }

        // Labels row (below the track).
        ctx.fillStyle = 'rgba(226, 232, 240, 0.95)';
        ctx.font = '13px system-ui, -apple-system, "Segoe UI", sans-serif';
        ctx.textBaseline = 'top';
        const labelY = trackY + trackH + 8;
        if (snap.tickText)  ctx.fillText(snap.tickText,  pad, labelY);
        if (snap.speedText) {
            ctx.textAlign = 'right';
            ctx.fillText(snap.speedText, w - pad, labelY);
            ctx.textAlign = 'left';
        }
        ctx.restore();
    }

    function pickMime(preference) {
        const list = Array.isArray(preference) && preference.length
            ? preference
            : ['video/webm;codecs=vp9', 'video/webm;codecs=vp8', 'video/webm'];
        if (typeof MediaRecorder === 'undefined') return null;
        for (const m of list) {
            if (MediaRecorder.isTypeSupported && MediaRecorder.isTypeSupported(m)) return m;
        }
        return null;
    }

    async function start(rootSelector, opts) {
        opts = opts || {};
        if (session) {
            return { ok: false, reason: 'already-recording', sessionId: session.id };
        }
        const root = (typeof rootSelector === 'string')
            ? document.querySelector(rootSelector) : rootSelector;
        if (!root) return { ok: false, reason: 'target-not-found' };
        if (typeof window.kmilaCanvasRender !== 'function') {
            return { ok: false, reason: 'renderer-missing' };
        }

        const fps = Math.max(1, Math.min(60, opts.fps || 20));
        const hardCapMs = Math.max(5, opts.hardCapSeconds || 120) * 1000;
        const mime = pickMime(opts.codecPreference);
        if (!mime) return { ok: false, reason: 'no-supported-codec' };

        const withRunbar = !!opts.withRunbar;
        const runbarH = withRunbar ? Math.max(40, opts.runbarHeight || 80) : 0;
        const w = root.offsetWidth || 800;
        const h = root.offsetHeight || 600;
        const cvs = document.createElement('canvas');
        cvs.width = w; cvs.height = h + runbarH;
        // Prime the surface so MediaRecorder gets a first frame immediately.
        // Renderer paints on the top-of-canvas subregion; the runbar band
        // covers the bottom `runbarH` pixels drawn separately.
        window.kmilaCanvasRender(root, cvs);
        if (withRunbar) paintRunbar(cvs, runbarH, snapshotRunbar());

        const stream = cvs.captureStream(fps);
        const chunks = [];
        const recorder = new MediaRecorder(stream, { mimeType: mime });
        recorder.ondataavailable = (e) => { if (e.data && e.data.size) chunks.push(e.data); };

        const id = nextSessionId++;
        const startTs = performance.now();
        let lastDraw = 0;
        let rafId = 0;
        let stopping = false;
        let capTimer = 0;

        // Frame pump: rAF driven, throttles at target fps. kmilaCanvasRender
        // is synchronous — no promise queue, no inflight bookkeeping. The
        // runbar overlay repaints alongside the main canvas each tick so
        // its progress/labels track the recording in real time.
        function pump(now) {
            if (stopping) return;
            if (now - lastDraw >= 1000 / fps) {
                lastDraw = now;
                try {
                    window.kmilaCanvasRender(root, cvs);
                    if (withRunbar) paintRunbar(cvs, runbarH, snapshotRunbar());
                }
                catch (e) { /* skip frame */ }
            }
            rafId = requestAnimationFrame(pump);
        }

        session = {
            id, root, mime, fps, hardCapMs, chunks, recorder, stream,
            canvas: cvs, startTs,
            state: 'recording',
            filename: opts.filename || 'kmila-canvas.webm',
            get rafId() { return rafId; },
            get capTimer() { return capTimer; },
            stopFn: null,
        };

        capTimer = setTimeout(() => {
            if (session && session.id === id && session.state === 'recording') {
                stop(id);
            }
        }, hardCapMs);
        session.capTimer = capTimer;
        session.stopFn = () => { stopping = true; };

        recorder.start(500);
        rafId = requestAnimationFrame(pump);
        return { ok: true, mime, sessionId: id };
    }

    async function stop(sessionId) {
        if (!session || (sessionId != null && session.id !== sessionId)) {
            return { ok: false, reason: 'no-active-session' };
        }
        if (session.state === 'stopping') return { ok: false, reason: 'already-stopping' };
        session.state = 'stopping';
        try { session.stopFn && session.stopFn(); } catch (_) {}
        if (session.rafId) cancelAnimationFrame(session.rafId);
        if (session.capTimer) clearTimeout(session.capTimer);
        return new Promise((resolve) => {
            const rec = session.recorder;
            rec.onstop = () => {
                const blob = new Blob(session.chunks, { type: session.mime });
                try { session.stream.getTracks().forEach(t => t.stop()); } catch (_) {}
                downloadBlob(blob, session.filename);
                const outId = session.id;
                const size = blob.size;
                session = null;
                resolve({ ok: true, downloaded: true, sessionId: outId, bytes: size });
            };
            try { rec.requestData(); } catch (_) {}
            try { rec.stop(); } catch (_) {
                session = null;
                resolve({ ok: false, reason: 'stop-threw' });
            }
        });
    }

    function status(sessionId) {
        if (!session || (sessionId != null && session.id !== sessionId)) {
            return { state: 'idle', elapsedMs: 0, fps: 0, capMs: 0 };
        }
        const elapsed = performance.now() - session.startTs;
        return {
            state: session.state,
            elapsedMs: Math.round(elapsed),
            fps: session.fps,
            capMs: session.hardCapMs,
            sessionId: session.id,
        };
    }

    window.kmilaCanvasRecordStart = start;
    window.kmilaCanvasRecordStop = stop;
    window.kmilaCanvasRecordStatus = status;
})();
