(() => {
    console.clear();

    const links = new Set();
    const urlAttributes = [
        'href',
        'src',
        'poster',
        'data-src',
        'data-original',
        'data-lazy',
        'data-url',
        'data-href'
    ];

    const cleanUrl = url => url.split('?secure=', 1)[0];

    const addUrl = value => {
        if (!value) return;

        const trimmed = value.trim();
        if (!trimmed || trimmed.startsWith('data:') || trimmed.startsWith('javascript:')) return;

        try {
            const url = new URL(trimmed, window.location.href);
            if (url.protocol === 'http:' || url.protocol === 'https:') links.add(cleanUrl(url.href));
        } catch {
            // Ignore malformed values found in attributes or styles.
        }
    };

    const addSrcset = value => {
        if (!value) return;
        value.split(',').forEach(entry => addUrl(entry.trim().split(/\s+/)[0]));
    };

    document.querySelectorAll('*').forEach(element => {
        urlAttributes.forEach(attribute => addUrl(element.getAttribute(attribute)));
        addSrcset(element.getAttribute('srcset'));

        const backgroundImage = getComputedStyle(element).backgroundImage;
        if (backgroundImage && backgroundImage !== 'none') {
            const matches = backgroundImage.match(/url\(["']?(.*?)["']?\)/g);
            matches?.forEach(match => addUrl(match.replace(/url\(["']?(.*?)["']?\)/, '$1')));
        }
    });

    const urls = Array.from(links);
    const json = JSON.stringify(urls, null, 2);

    const fallbackCopy = text => {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.setAttribute('readonly', '');
        textarea.style.position = 'fixed';
        textarea.style.top = '-9999px';
        document.body.appendChild(textarea);
        textarea.select();

        try {
            document.execCommand('copy');
            console.log(`Copied ${urls.length} URLs to clipboard as JSON`);
        } finally {
            textarea.remove();
        }
    };

    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard
            .writeText(json)
            .then(() => console.log(`Copied ${urls.length} URLs to clipboard as JSON`))
            .catch(() => fallbackCopy(json));
    } else {
        fallbackCopy(json);
    }
})();
