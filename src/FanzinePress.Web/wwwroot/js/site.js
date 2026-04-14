// Glyph palette — insert glyphs into the last focused text field
(function () {
    let lastFocusedInput = null;

    // Track the last focused text input/textarea
    document.addEventListener('focusin', function (e) {
        if (e.target.matches('input[type="text"], textarea')) {
            lastFocusedInput = e.target;
        }
    });

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.glyph-btn');
        if (!btn) return;

        const glyph = btn.dataset.glyph;
        if (!glyph || !lastFocusedInput) return;

        e.preventDefault();

        const input = lastFocusedInput;
        const start = input.selectionStart;
        const end = input.selectionEnd;
        const value = input.value;

        input.value = value.substring(0, start) + glyph + value.substring(end);
        input.selectionStart = input.selectionEnd = start + glyph.length;
        input.focus();
    });
})();
