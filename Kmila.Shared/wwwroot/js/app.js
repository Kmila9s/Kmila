// Kmila Application JavaScript Utilities

/**
 * Downloads a file with the specified name and content.
 * Creates a temporary anchor element, sets the download attribute,
 * and triggers a click to download the file.
 * @param {string} fileName - The name for the downloaded file.
 * @param {string} content - The content to be downloaded.
 */
window.downloadFile = function (fileName, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
};

/*
 * Theme persistence lives entirely in two places now:
 *   1. An inline <head> script in the host HTMLs applies localStorage['kmila-theme']
 *      to <html data-bs-theme=…> before first paint (no flash of wrong theme).
 *   2. TopBar.OnThemeChanged calls localStorage.setItem + DbContext directly via
 *      Blazor's dotted-path JSInterop — no wrapper function needed.
 * That way there is no custom JS on the interop path that could be missing at
 * startup, which avoided the "kmilaTheme is undefined" circuit crash.
 */
