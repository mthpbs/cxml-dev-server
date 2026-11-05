// Small browser helpers called from the Blazor components.
window.cxmlDev = {
    copyText: async function (text) {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return;
        }
        // Fallback for plain-HTTP origins where the async clipboard API is unavailable.
        const area = document.createElement('textarea');
        area.value = text;
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        try { document.execCommand('copy'); } finally { document.body.removeChild(area); }
    }
};
