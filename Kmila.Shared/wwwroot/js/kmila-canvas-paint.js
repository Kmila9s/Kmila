/* Kmila Visual Runtime — bespoke 2D widget rasteriser.
 *
 * Public API:
 *   kmilaCanvasRender(rootSelector, destCanvas) → boolean
 *
 * Purpose: replace html2canvas in the in-app recorder + PNG exporter.
 * html2canvas 1.4.1 throws on the `color()` CSS function that Bootstrap's
 * vendored stylesheet ships, and even with stylesheet stripping the
 * browser preserves wide-gamut colours in getComputedStyle values, so the
 * parser still trips. This module sidesteps CSS parsing entirely — it
 * reads widget state from a small set of class markers + inline custom
 * properties and paints canonical 2D primitives.
 *
 * Design invariants:
 *   - Fully synchronous: no promises, no clones, no awaited images. The
 *     recorder pump can call this every frame at 20 fps without queuing.
 *   - Only reads inline styles + class markers + getComputedStyle values
 *     that are always in `rgb()` / `rgba()` form (theme foreground /
 *     background). Never touches Bootstrap-fed colour values.
 *   - Unknown widget types render as a dashed placeholder so a new
 *     widget doesn't break the frame — it just shows outlined until a
 *     painter is added.
 */
(function () {
    const PAINTERS = {
        'kviz-led':              paintLed,
        'kviz-button':           paintButton,
        'kviz-switch':           paintSwitch,
        'kviz-multi-switch':     paintMultiSwitch,
        'kviz-led-array':        paintLedArray,
        'kviz-matrix-3x3':       paintLedMatrix,
        'kviz-matrix-3x3-rgb':   paintRgbMatrix,
        'kviz-7seg':             paint7Seg,
        'kviz-buzzer':           paintBuzzer,
        'kviz-keypad':           paintKeypad,
        'kviz-lcd':              paintLcd,
        'kviz-stepper':          paintStepper,
    };

    // Safe theme accessors — every call site returns a plain rgb()/rgba()
    // string because these properties never go through Bootstrap's
    // wide-gamut color() fallbacks.
    function readTheme() {
        const bs = window.getComputedStyle(document.body);
        const html = window.getComputedStyle(document.documentElement);
        return {
            fg:     bs.color || 'rgb(240,240,240)',
            fgDim:  'rgba(255,255,255,0.55)',
            bg:     bs.backgroundColor || 'rgb(24,24,32)',
            offCell:'rgba(255,255,255,0.08)',
            border: 'rgba(255,255,255,0.15)',
            // Accent: prefer the Kmila brand var if the document sets it
            // as a plain rgb; otherwise fall back to a Kmila slate.
            accent: (html.getPropertyValue('--kmila-accent') || '#f59e0b').trim(),
        };
    }

    function widgetType(el) {
        for (const c of el.classList) if (PAINTERS[c]) return c;
        return null;
    }

    function localRect(el, rootRect) {
        const r = el.getBoundingClientRect();
        return {
            x: r.left - rootRect.left,
            y: r.top  - rootRect.top,
            w: r.width,
            h: r.height,
        };
    }

    // Small utility — rounded rect path (CanvasRenderingContext2D lacks
    // roundRect on some older Chromes, so ship our own).
    function roundRect(ctx, x, y, w, h, r) {
        r = Math.min(r, w * 0.5, h * 0.5);
        ctx.beginPath();
        ctx.moveTo(x + r, y);
        ctx.arcTo(x + w, y,     x + w, y + h, r);
        ctx.arcTo(x + w, y + h, x,     y + h, r);
        ctx.arcTo(x,     y + h, x,     y,     r);
        ctx.arcTo(x,     y,     x + w, y,     r);
        ctx.closePath();
    }

    // Entry point.
    function kmilaCanvasRender(rootSel, dest) {
        const root = typeof rootSel === 'string'
            ? document.querySelector(rootSel) : rootSel;
        if (!root || !dest || !dest.getContext) return false;
        const ctx = dest.getContext('2d');
        if (!ctx) return false;
        const theme = readTheme();
        const rootRect = root.getBoundingClientRect();

        // Background fill — matches the running canvas colour so the
        // recording reads as a continuation of what the user sees.
        ctx.save();
        ctx.fillStyle = theme.bg;
        ctx.fillRect(0, 0, dest.width, dest.height);

        // Grid backdrop when the canvas is in grid mode (kviz-canvas--grid).
        if (root.classList.contains('kviz-canvas--grid')) {
            ctx.strokeStyle = 'rgba(255,255,255,0.04)';
            ctx.lineWidth = 1;
            const step = 24;
            for (let x = 0; x < dest.width; x += step) {
                ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, dest.height); ctx.stroke();
            }
            for (let y = 0; y < dest.height; y += step) {
                ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(dest.width, y); ctx.stroke();
            }
        }

        // Wires drawn under widgets so widget bodies overlap the wire
        // ends cleanly.
        paintWires(ctx, root, rootRect, theme);

        // Widgets in DOM order — z-index generally matches DOM order too.
        const widgets = root.querySelectorAll('.kviz-widget');
        widgets.forEach(el => {
            const type = widgetType(el);
            const box  = localRect(el, rootRect);
            const painter = PAINTERS[type] || paintUnknown;
            ctx.save();
            ctx.translate(Math.round(box.x), Math.round(box.y));
            try { painter(ctx, el, box, theme); }
            catch (e) { paintUnknown(ctx, el, box, theme); }
            ctx.restore();
        });

        ctx.restore();
        return true;
    }

    /* ── Widget painters ────────────────────────────────────────────── */

    function paintLed(ctx, el, box, theme) {
        const on = el.classList.contains('kviz-led--on');
        const color = el.style.getPropertyValue('--kviz-led-color').trim() || '#ef4444';
        // Bezel
        ctx.fillStyle = '#111';
        roundRect(ctx, 0, 0, box.w, box.h, 8); ctx.fill();
        // LED body
        const cx = box.w / 2, cy = Math.min(box.h / 2, 24);
        const r = Math.min(box.w, box.h) * 0.28;
        ctx.fillStyle = on ? color : theme.offCell;
        ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2); ctx.fill();
        if (on) {
            // Halo
            const grad = ctx.createRadialGradient(cx, cy, r * 0.4, cx, cy, r * 2.2);
            grad.addColorStop(0, 'rgba(255,255,255,0.55)');
            grad.addColorStop(0.4, hexOrColorWithAlpha(color, 0.35));
            grad.addColorStop(1, 'rgba(0,0,0,0)');
            ctx.fillStyle = grad;
            ctx.beginPath(); ctx.arc(cx, cy, r * 2.2, 0, Math.PI * 2); ctx.fill();
        }
        // Port label
        drawLabel(ctx, el.querySelector('.kviz-led-label'), 0, box.h - 12, box.w, theme);
    }

    function paintButton(ctx, el, box, theme) {
        const pressed = el.classList.contains('kviz-button--pressed');
        // Base
        ctx.fillStyle = pressed ? '#0f172a' : '#1e293b';
        roundRect(ctx, 0, 0, box.w, box.h, 10); ctx.fill();
        // Cap
        const pad = 6, capH = box.h - 22;
        ctx.fillStyle = pressed ? theme.accent : '#334155';
        roundRect(ctx, pad, pad + (pressed ? 2 : 0), box.w - pad * 2, capH, 8); ctx.fill();
        // Label
        ctx.fillStyle = pressed ? '#0f172a' : theme.fg;
        ctx.font = 'bold 11px sans-serif';
        ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        const label = (el.querySelector('.kviz-button-label')?.textContent || 'BTN').trim();
        ctx.fillText(label, box.w / 2, pad + capH / 2 + (pressed ? 2 : 0));
        ctx.textAlign = 'start'; ctx.textBaseline = 'alphabetic';
        drawLabel(ctx, el.querySelector('.kviz-button-port'), 0, box.h - 8, box.w, theme);
    }

    function paintSwitch(ctx, el, box, theme) {
        const on = el.classList.contains('kviz-switch--on');
        // Body
        ctx.fillStyle = '#1e293b';
        roundRect(ctx, 0, 0, box.w, box.h, 10); ctx.fill();
        // Track — horizontal pill in upper area
        const tW = box.w - 20, tH = 22;
        const tX = 10, tY = 10;
        ctx.fillStyle = on ? theme.accent : '#334155';
        roundRect(ctx, tX, tY, tW, tH, tH / 2); ctx.fill();
        // Knob
        const kR = (tH - 4) / 2;
        const kX = on ? tX + tW - kR - 2 : tX + kR + 2;
        const kY = tY + tH / 2;
        ctx.fillStyle = '#f8fafc';
        ctx.beginPath(); ctx.arc(kX, kY, kR, 0, Math.PI * 2); ctx.fill();
        // Label + port
        drawLabel(ctx, el.querySelector('.kviz-switch-label'), 0, tY + tH + 14, box.w, theme);
        drawLabel(ctx, el.querySelector('.kviz-switch-port'), 0, box.h - 8, box.w, theme);
    }

    function paintMultiSwitch(ctx, el, box, theme) {
        ctx.fillStyle = '#1e293b';
        roundRect(ctx, 0, 0, box.w, box.h, 10); ctx.fill();
        const levers = el.querySelectorAll('.kviz-multi-switch__lever');
        if (!levers.length) return;
        const gap = 4, pad = 8;
        const totalGap = gap * (levers.length - 1);
        const lw = Math.max(10, (box.w - pad * 2 - totalGap) / levers.length);
        const lh = box.h - 28;
        levers.forEach((lv, i) => {
            const on = lv.classList.contains('kviz-multi-switch__lever--on');
            const x = pad + i * (lw + gap);
            const y = 14;
            ctx.fillStyle = '#0f172a';
            roundRect(ctx, x, y, lw, lh, 4); ctx.fill();
            // Handle top when on, bottom when off
            ctx.fillStyle = on ? theme.accent : '#475569';
            const hh = lh * 0.35;
            const hy = on ? y + 2 : y + lh - hh - 2;
            roundRect(ctx, x + 2, hy, lw - 4, hh, 3); ctx.fill();
        });
    }

    function paintLedArray(ctx, el, box, theme) {
        const vertical = el.classList.contains('kviz-led-array--vertical');
        const on = el.style.getPropertyValue('--kviz-led-color').trim() || '#ef4444';
        // Housing
        ctx.fillStyle = '#111';
        roundRect(ctx, 0, 0, box.w, box.h, 8); ctx.fill();
        const cells = el.querySelectorAll('.kviz-led-array__cell');
        if (!cells.length) return;
        const gap = 4, pad = 8;
        const stripLen = vertical ? box.h - pad * 2 - 18 : box.w - pad * 2;
        const totalGap = gap * (cells.length - 1);
        const cellSize = Math.max(6, Math.min(24, (stripLen - totalGap) / cells.length));
        cells.forEach((c, i) => {
            const isOn = c.classList.contains('kviz-led-array__cell--on');
            const off = pad + i * (cellSize + gap);
            const cx = vertical ? box.w / 2 : off + cellSize / 2;
            const cy = vertical ? off + cellSize / 2 : pad + cellSize / 2;
            ctx.fillStyle = isOn ? on : theme.offCell;
            ctx.beginPath(); ctx.arc(cx, cy, cellSize / 2 - 1, 0, Math.PI * 2); ctx.fill();
            if (isOn) {
                ctx.fillStyle = hexOrColorWithAlpha(on, 0.35);
                ctx.beginPath(); ctx.arc(cx, cy, cellSize / 2 + 3, 0, Math.PI * 2); ctx.fill();
                ctx.fillStyle = isOn ? on : theme.offCell;
                ctx.beginPath(); ctx.arc(cx, cy, cellSize / 2 - 1, 0, Math.PI * 2); ctx.fill();
            }
        });
        drawLabel(ctx, el.querySelector('.kviz-led-array__port'), 0, box.h - 8, box.w, theme);
    }

    function paintLedMatrix(ctx, el, box, theme) {
        const on = el.style.getPropertyValue('--kviz-led-color').trim() || '#22c55e';
        ctx.fillStyle = '#111';
        roundRect(ctx, 0, 0, box.w, box.h, 8); ctx.fill();
        const cells = el.querySelectorAll('.kviz-matrix-3x3__cell');
        const cols = 3, rows = Math.ceil(cells.length / cols);
        const pad = 8, gap = 6;
        const cellW = (box.w - pad * 2 - gap * (cols - 1)) / cols;
        const cellH = (box.h - pad * 2 - gap * (rows - 1)) / rows;
        const r = Math.min(cellW, cellH) / 2 - 2;
        cells.forEach((c, i) => {
            const isOn = c.classList.contains('kviz-matrix-3x3__cell--on');
            const col = i % cols, row = Math.floor(i / cols);
            const cx = pad + col * (cellW + gap) + cellW / 2;
            const cy = pad + row * (cellH + gap) + cellH / 2;
            ctx.fillStyle = isOn ? on : theme.offCell;
            ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2); ctx.fill();
        });
    }

    function paintRgbMatrix(ctx, el, box, theme) {
        ctx.fillStyle = '#111';
        roundRect(ctx, 0, 0, box.w, box.h, 8); ctx.fill();
        const cells = el.querySelectorAll('.kviz-matrix-3x3-rgb__cell');
        const cols = 3, rows = Math.ceil(cells.length / cols);
        const pad = 8, gap = 6;
        const cellW = (box.w - pad * 2 - gap * (cols - 1)) / cols;
        const cellH = (box.h - pad * 2 - gap * (rows - 1)) / rows;
        const r = Math.min(cellW, cellH) / 2 - 2;
        cells.forEach((c, i) => {
            const cellCol = c.style.getPropertyValue('--cell-color').trim();
            const isOn = c.classList.contains('kviz-matrix-3x3-rgb__cell--on');
            const col = i % cols, row = Math.floor(i / cols);
            const cx = pad + col * (cellW + gap) + cellW / 2;
            const cy = pad + row * (cellH + gap) + cellH / 2;
            ctx.fillStyle = isOn && cellCol ? cellCol : theme.offCell;
            ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2); ctx.fill();
        });
    }

    // 7-segment segment geometry in an 60×100 housing. Trapezoidal
    // approximation — no bevels, but the on/off shape is unambiguous.
    const SEG_GEOM = {
        a: [[8, 4],   [52, 4],   [46, 10],  [14, 10]],
        b: [[52, 6],  [56, 44],  [50, 48],  [46, 12]],
        c: [[50, 52], [56, 90],  [52, 96],  [46, 88]],
        d: [[14, 90], [46, 90],  [52, 96],  [8, 96]],
        e: [[4, 52],  [10, 48],  [14, 88],  [8, 94]],
        f: [[8, 6],   [14, 12],  [10, 44],  [4, 48]],
        g: [[12, 46], [48, 46],  [46, 52],  [14, 52]],
    };
    function paint7Seg(ctx, el, box, theme) {
        const onColor = el.style.getPropertyValue('--kviz-7seg-color').trim() || '#22c55e';
        const offColor = 'rgba(255,255,255,0.06)';
        // Housing
        ctx.fillStyle = '#0f172a';
        roundRect(ctx, 0, 0, box.w, box.h, 6); ctx.fill();
        // Fit the 60×100 geometry inside the box (assume box ~ 70×120).
        const sx = box.w / 70, sy = box.h / 120;
        ctx.save();
        ctx.translate(5 * sx, 10 * sy);
        ctx.scale(sx, sy);
        const segEls = el.querySelectorAll('.kviz-7seg__seg');
        segEls.forEach(seg => {
            let key = null;
            for (const c of seg.classList) {
                if (c === 'kviz-7seg__seg--on') continue;
                const m = /^kviz-7seg__seg--([a-g])$/.exec(c);
                if (m) { key = m[1]; break; }
                if (c === 'kviz-7seg__seg--dp') { key = 'dp'; break; }
            }
            if (!key) return;
            const isOn = seg.classList.contains('kviz-7seg__seg--on');
            ctx.fillStyle = isOn ? onColor : offColor;
            if (key === 'dp') {
                ctx.beginPath(); ctx.arc(58, 92, 3, 0, Math.PI * 2); ctx.fill();
            } else {
                const geom = SEG_GEOM[key]; if (!geom) return;
                ctx.beginPath();
                geom.forEach(([x, y], i) => i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y));
                ctx.closePath(); ctx.fill();
            }
        });
        ctx.restore();
    }

    function paintBuzzer(ctx, el, box, theme) {
        const active = el.classList.contains('kviz-buzzer--active');
        ctx.fillStyle = '#111';
        roundRect(ctx, 0, 0, box.w, box.h, 8); ctx.fill();
        const cx = box.w / 2, cy = box.h / 2;
        const rBase = Math.min(box.w, box.h) * 0.22;
        // Cone
        ctx.fillStyle = active ? theme.accent : '#475569';
        ctx.beginPath(); ctx.arc(cx, cy, rBase, 0, Math.PI * 2); ctx.fill();
        // Rings
        if (active) {
            [1.6, 2.2, 2.8].forEach((k, i) => {
                ctx.strokeStyle = hexOrColorWithAlpha(theme.accent, 0.6 - i * 0.15);
                ctx.lineWidth = 2;
                ctx.beginPath(); ctx.arc(cx, cy, rBase * k, -0.6, 0.6); ctx.stroke();
            });
        }
    }

    function paintKeypad(ctx, el, box, theme) {
        ctx.fillStyle = '#0f172a';
        roundRect(ctx, 0, 0, box.w, box.h, 8); ctx.fill();
        const keys = el.querySelectorAll('.kviz-keypad__key');
        if (!keys.length) return;
        // Infer grid dims from class modifier (e.g., kviz-keypad--4x4) or sqrt(count).
        let cols = 4, rows = 4;
        for (const c of el.classList) {
            const m = /^kviz-keypad--(\d+)x(\d+)$/.exec(c);
            if (m) { cols = +m[1]; rows = +m[2]; break; }
        }
        if (rows * cols !== keys.length) {
            cols = Math.ceil(Math.sqrt(keys.length));
            rows = Math.ceil(keys.length / cols);
        }
        const pad = 6, gap = 4;
        const kw = (box.w - pad * 2 - gap * (cols - 1)) / cols;
        const kh = (box.h - pad * 2 - gap * (rows - 1)) / rows;
        keys.forEach((k, i) => {
            const pressed = k.classList.contains('kviz-keypad__key--pressed');
            const col = i % cols, row = Math.floor(i / cols);
            const x = pad + col * (kw + gap);
            const y = pad + row * (kh + gap);
            ctx.fillStyle = pressed ? theme.accent : '#334155';
            roundRect(ctx, x, y, kw, kh, 4); ctx.fill();
            ctx.fillStyle = pressed ? '#0f172a' : theme.fg;
            ctx.font = 'bold 10px sans-serif';
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            ctx.fillText((k.textContent || '').trim(), x + kw / 2, y + kh / 2);
            ctx.textAlign = 'start'; ctx.textBaseline = 'alphabetic';
        });
    }

    function paintLcd(ctx, el, box, theme) {
        const on = !el.classList.contains('kviz-lcd--off');
        // Bezel
        ctx.fillStyle = '#0f172a';
        roundRect(ctx, 0, 0, box.w, box.h, 6); ctx.fill();
        // Screen
        const pad = 8;
        ctx.fillStyle = on ? '#1a3d1a' : '#0a2510';
        roundRect(ctx, pad, pad, box.w - pad * 2, box.h - pad * 2, 3); ctx.fill();
        // Rows of characters — read from .kviz-lcd__row containers
        const rows = el.querySelectorAll('.kviz-lcd__row');
        if (!rows.length) return;
        const rowH = (box.h - pad * 2) / rows.length;
        ctx.font = 'bold ' + Math.round(rowH * 0.6) + 'px monospace';
        ctx.textBaseline = 'middle';
        ctx.textAlign = 'start';
        ctx.fillStyle = on ? '#7ee787' : 'rgba(255,255,255,0.25)';
        rows.forEach((row, i) => {
            const text = (row.textContent || '').replace(/\s+/g, ' ').trim();
            ctx.fillText(text, pad + 6, pad + rowH * i + rowH / 2);
        });
    }

    function paintStepper(ctx, el, box, theme) {
        ctx.fillStyle = '#0f172a';
        roundRect(ctx, 0, 0, box.w, box.h, 8); ctx.fill();
        const cx = box.w / 2, cy = box.h / 2;
        const rBody = Math.min(box.w, box.h) * 0.34;
        // Body
        ctx.fillStyle = '#1e293b';
        ctx.beginPath(); ctx.arc(cx, cy, rBody, 0, Math.PI * 2); ctx.fill();
        // Coils
        const coils = el.querySelectorAll('.kviz-stepper-coil');
        coils.forEach((coil, i) => {
            const lit = coil.classList.contains('kviz-stepper-coil--lit');
            const a = (i / Math.max(1, coils.length)) * Math.PI * 2 - Math.PI / 2;
            const ox = cx + Math.cos(a) * (rBody + 8);
            const oy = cy + Math.sin(a) * (rBody + 8);
            ctx.fillStyle = lit ? theme.accent : '#475569';
            ctx.beginPath(); ctx.arc(ox, oy, 5, 0, Math.PI * 2); ctx.fill();
        });
        // Rotor — read the inline transform for the angle
        const rotor = el.querySelector('.kviz-stepper-rotor') || el.querySelector('[class*="rotor"]');
        let angle = 0;
        if (rotor && rotor.style && rotor.style.transform) {
            const m = /rotate\((-?\d+(?:\.\d+)?)deg\)/.exec(rotor.style.transform);
            if (m) angle = parseFloat(m[1]) * Math.PI / 180;
        }
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(angle);
        ctx.fillStyle = theme.accent;
        ctx.beginPath();
        ctx.moveTo(-3, -rBody * 0.85);
        ctx.lineTo(3,  -rBody * 0.85);
        ctx.lineTo(1,   rBody * 0.35);
        ctx.lineTo(-1,  rBody * 0.35);
        ctx.closePath();
        ctx.fill();
        ctx.fillStyle = '#111';
        ctx.beginPath(); ctx.arc(0, 0, 4, 0, Math.PI * 2); ctx.fill();
        ctx.restore();
    }

    function paintUnknown(ctx, el, box, theme) {
        ctx.strokeStyle = theme.fgDim;
        ctx.setLineDash([4, 3]);
        ctx.lineWidth = 1;
        ctx.strokeRect(1, 1, box.w - 2, box.h - 2);
        ctx.setLineDash([]);
        ctx.fillStyle = theme.fg;
        ctx.font = '10px sans-serif';
        const label = (widgetType(el) || el.className.split(' ')[0]) || '?';
        ctx.fillText(label.slice(0, 22), 6, 14);
    }

    /* ── Wires + helpers ────────────────────────────────────────────── */

    function paintWires(ctx, root, rootRect, theme) {
        const svg = root.querySelector('.visual-page__wires');
        if (!svg) return;
        const svgRect = svg.getBoundingClientRect();
        const dx = svgRect.left - rootRect.left;
        const dy = svgRect.top  - rootRect.top;
        ctx.save();
        ctx.translate(dx, dy);
        const paths = svg.querySelectorAll('path, line, polyline');
        paths.forEach(p => {
            const stroke = window.getComputedStyle(p).stroke;
            ctx.strokeStyle = stroke && stroke !== 'none' ? stroke : theme.accent;
            ctx.lineWidth = parseFloat(window.getComputedStyle(p).strokeWidth) || 1.5;
            const dash = window.getComputedStyle(p).strokeDasharray;
            if (dash && dash !== 'none') {
                ctx.setLineDash(dash.split(/[ ,]+/).map(Number).filter(n => !isNaN(n)));
            } else {
                ctx.setLineDash([]);
            }
            if (p.tagName.toLowerCase() === 'path') {
                const d = p.getAttribute('d');
                if (!d) return;
                try { ctx.stroke(new Path2D(d)); } catch (_) {}
            } else if (p.tagName.toLowerCase() === 'line') {
                ctx.beginPath();
                ctx.moveTo(+p.getAttribute('x1') || 0, +p.getAttribute('y1') || 0);
                ctx.lineTo(+p.getAttribute('x2') || 0, +p.getAttribute('y2') || 0);
                ctx.stroke();
            } else if (p.tagName.toLowerCase() === 'polyline') {
                const pts = (p.getAttribute('points') || '').trim().split(/\s+/)
                    .map(pt => pt.split(',').map(Number)).filter(pt => pt.length === 2);
                if (pts.length < 2) return;
                ctx.beginPath();
                pts.forEach(([x, y], i) => i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y));
                ctx.stroke();
            }
        });
        ctx.restore();
    }

    // Draw a short text label at (x, y) inside a box of width w. Reads
    // the source element's textContent + falls back gracefully when null.
    function drawLabel(ctx, srcEl, x, y, w, theme) {
        if (!srcEl) return;
        const text = (srcEl.textContent || '').trim();
        if (!text) return;
        ctx.fillStyle = theme.fgDim;
        ctx.font = '9px sans-serif';
        ctx.textAlign = 'center'; ctx.textBaseline = 'alphabetic';
        // Clip label to widget width so long port names don't overflow.
        const maxW = Math.max(20, w - 4);
        let clipped = text;
        while (ctx.measureText(clipped).width > maxW && clipped.length > 3) {
            clipped = clipped.slice(0, -1);
        }
        if (clipped !== text) clipped = clipped.slice(0, -1) + '…';
        ctx.fillText(clipped, x + w / 2, y);
        ctx.textAlign = 'start'; ctx.textBaseline = 'alphabetic';
    }

    // Add alpha channel to an rgb() / rgba() / #hex / named colour.
    // Uses a scratch canvas to normalise then re-emits rgba(). Skips
    // wide-gamut color()/color-mix() by falling back to the fillStyle
    // reader (which converts to sRGB rgb() in every browser we support).
    let _alphaCanvas = null;
    function hexOrColorWithAlpha(color, alpha) {
        try {
            if (!_alphaCanvas) _alphaCanvas = document.createElement('canvas');
            const c = _alphaCanvas.getContext('2d');
            c.fillStyle = '#000';
            c.fillStyle = color;
            const norm = c.fillStyle;   // 'rgb(r, g, b)' or '#rrggbb'
            // Case: '#rrggbb'
            if (norm[0] === '#' && norm.length === 7) {
                const r = parseInt(norm.slice(1, 3), 16);
                const g = parseInt(norm.slice(3, 5), 16);
                const b = parseInt(norm.slice(5, 7), 16);
                return `rgba(${r}, ${g}, ${b}, ${alpha})`;
            }
            // Case: 'rgb(r, g, b)'
            const m = /^rgb\((\d+),\s*(\d+),\s*(\d+)\)/.exec(norm);
            if (m) return `rgba(${m[1]}, ${m[2]}, ${m[3]}, ${alpha})`;
            return `rgba(255, 255, 255, ${alpha})`;
        } catch (_) {
            return `rgba(255, 255, 255, ${alpha})`;
        }
    }

    window.kmilaCanvasRender = kmilaCanvasRender;
})();
