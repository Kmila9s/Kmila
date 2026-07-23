/* Kmila Visual Runtime — canvas export (§6 Item 2).
 *
 * Exports the current canvas view as PNG (via kmilaCanvasRender) or SVG
 * (via the DOM<-foreignObject serialisation path). PDF export is still
 * deferred until jspdf is vendored (offline-first).
 *
 * Public API on window:
 *   kmilaCanvasExportSvg(rootSelector, filename) → Promise<boolean>
 *   kmilaCanvasExportPng(rootSelector, filename) → Promise<boolean>
 *   kmilaCanvasExportSupportsPng()               → boolean
 *   kmilaCanvasExportSupportsPdf()               → boolean (false in v1)
 */
(function () {
    // ── SVG helpers (foreignObject path — lossless for SVG viewers) ──
    const RENDER_STYLE_PROPS = [
        'position', 'left', 'top', 'width', 'height',
        'display', 'flex-direction', 'align-items', 'justify-content', 'gap',
        'color', 'background', 'background-color', 'background-image', 'border',
        'border-color', 'border-width', 'border-radius', 'border-style',
        'box-shadow', 'padding', 'margin', 'font-family', 'font-size',
        'font-weight', 'line-height', 'text-align', 'opacity', 'transform',
        'transform-origin', 'stroke', 'stroke-width', 'fill', 'z-index',
        'overflow', 'white-space', 'clip-path', 'pointer-events'
    ];
    function inlineStyles(source, target) {
        const cs = window.getComputedStyle(source);
        let inline = '';
        for (const prop of RENDER_STYLE_PROPS) {
            const v = cs.getPropertyValue(prop);
            if (v && v !== 'none' && v !== 'auto') inline += `${prop}:${v};`;
        }
        target.setAttribute('style', inline);
        for (let i = 0; i < source.children.length; i++) {
            if (target.children[i]) inlineStyles(source.children[i], target.children[i]);
        }
    }
    function buildForeignObjectSvg(canvasEl) {
        let w = canvasEl.offsetWidth;
        let h = canvasEl.offsetHeight;
        if (!(w > 0) || !(h > 0)) {
            const r = canvasEl.getBoundingClientRect();
            w = Math.ceil(r.width) || 800;
            h = Math.ceil(r.height) || 600;
        }
        const clone = canvasEl.cloneNode(true);
        inlineStyles(canvasEl, clone);
        clone.style.transform = '';
        const xml = new XMLSerializer().serializeToString(clone);
        const xhtml = xml.replace(/^(<[a-zA-Z][^ >]*)/, '$1 xmlns="http://www.w3.org/1999/xhtml"');
        return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
  <foreignObject width="100%" height="100%">${xhtml}</foreignObject>
</svg>`;
    }

    function downloadBlob(blob, filename) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename || 'kmila-canvas';
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 500);
    }

    async function exportSvg(rootSelector, filename) {
        const root = (typeof rootSelector === 'string')
            ? document.querySelector(rootSelector) : rootSelector;
        if (!root) return false;
        const svgText = buildForeignObjectSvg(root);
        const blob = new Blob([svgText], { type: 'image/svg+xml;charset=utf-8' });
        downloadBlob(blob, filename || 'kmila-canvas.svg');
        return true;
    }

    // PNG export uses the same bespoke rasteriser as the recorder — no
    // taint issue, no CSS-parser issue, no dependencies. `.toBlob` on a
    // canvas painted via kmilaCanvasRender is safe because we only draw
    // shapes, no cross-origin images.
    async function exportPng(rootSelector, filename) {
        if (typeof window.kmilaCanvasRender !== 'function') return false;
        const root = (typeof rootSelector === 'string')
            ? document.querySelector(rootSelector) : rootSelector;
        if (!root) return false;
        const w = root.offsetWidth || 800;
        const h = root.offsetHeight || 600;
        const cvs = document.createElement('canvas');
        cvs.width = w; cvs.height = h;
        try { window.kmilaCanvasRender(root, cvs); }
        catch (_) { return false; }
        return await new Promise((resolve) => {
            cvs.toBlob((blob) => {
                if (!blob) { resolve(false); return; }
                downloadBlob(blob, filename || 'kmila-canvas.png');
                resolve(true);
            }, 'image/png');
        });
    }

    window.kmilaCanvasExportSvg = exportSvg;
    window.kmilaCanvasExportPng = exportPng;
    window.kmilaCanvasExportSupportsPng = function () {
        return typeof window.kmilaCanvasRender === 'function';
    };
    window.kmilaCanvasExportSupportsPdf = function () { return false; };
})();
