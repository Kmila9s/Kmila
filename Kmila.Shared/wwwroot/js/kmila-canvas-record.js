/* Kmila Visual Runtime — canvas recording v1 (§6 Item 1).
 *
 * Records the running Visual canvas as a webm video. Each frame is
 * painted onto an offscreen <canvas> by `kmilaCanvasRender` (defined in
 * kmila-canvas-paint.js — a bespoke pure-2D rasteriser that reads widget
 * state from CSS classes + inline custom properties). MediaRecorder
 * captures that offscreen canvas via captureStream(fps) and emits a
 * downloadable webm blob on stop.
 *
 * v1 is single-view (canvas only) with a 2-minute hard cap. v2 will add
 * a split view (canvas + Monaco line highlight) — bigger design tracked
 * in Documentacion/HANDOFF_visual_canvas_*.md.
 *
 * Public API on window:
 *   kmilaCanvasRecordStart(selector, opts) → {ok, mime, sessionId, reason?}
 *   kmilaCanvasRecordStop(sessionId)       → {ok, downloaded, reason?}
 *   kmilaCanvasRecordStatus(sessionId)     → {state, elapsedMs, fps, capMs}
 *
 * opts = { fps=20, hardCapSeconds=120,
 *          codecPreference=['video/webm;codecs=vp9','video/webm'],
 *          filename='kmila-canvas.webm' }
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

        const w = root.offsetWidth || 800;
        const h = root.offsetHeight || 600;
        const cvs = document.createElement('canvas');
        cvs.width = w; cvs.height = h;
        // Prime the surface so MediaRecorder gets a first frame immediately.
        window.kmilaCanvasRender(root, cvs);

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
        // is synchronous — no promise queue, no inflight bookkeeping.
        function pump(now) {
            if (stopping) return;
            if (now - lastDraw >= 1000 / fps) {
                lastDraw = now;
                try { window.kmilaCanvasRender(root, cvs); }
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
