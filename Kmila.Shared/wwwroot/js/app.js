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
