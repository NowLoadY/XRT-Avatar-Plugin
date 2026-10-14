/* Portable authoring preview; uses the same text source and preset definitions. */
(function () {
    'use strict';
    const source = new XrtBubble.CaptionSource();
    const stage = document.querySelector('#stage'), caption = document.querySelector('#caption');
    const input = document.querySelector('#text'), sample = document.querySelector('#sample');
    const hold = document.querySelector('#hold'), sourceView = document.querySelector('#source');
    const samples = {mixed: "到二楼集合，we'll meet upstairs. 👋", en: "The entrance is on the right. We'll wait near the stairs.",
        zh: '入口在右边，我们会在楼梯旁边等你。', ja: '入口は右側です。階段の近くで待っています。'};
    let preset = 'horizontal', revision = 0, x = .5, y = .5, dragging = null, displayText = '';
    function render(text) {
        const p = XrtBubble.profiles[preset], scale = Math.min(500, (stage.clientWidth - 48) / .9);
        const fontSize = p.lineHeight * scale, font = 'system-ui, sans-serif';
        const measure = document.createElement('canvas').getContext('2d');
        measure.font = fontSize + 'px ' + font;
        let columns = p.maxColumns, laid;
        do {
            laid = XrtBubble.layout(text, {profile: preset, maxColumns: columns});
            if (Math.max(...laid.text.split('\n').map(line => measure.measureText(line).width)) <= (p.width - 2*p.padding)*scale) break;
            columns--;
        } while (columns > 1);
        caption.classList.toggle('board', preset === 'board');
        caption.style.width = p.width*scale+'px';
        caption.style.minHeight = p.height*scale+'px';
        caption.style.fontFamily = font;
        caption.style.fontSize = fontSize+'px';
        caption.style.lineHeight = p.lineSpacing;
        caption.style.padding = p.padding*scale+'px';
        caption.style.color = `rgba(${p.textColor.red},${p.textColor.green},${p.textColor.blue},${p.textAlpha})`;
        caption.style.background = `rgba(${p.backgroundColor.red},${p.backgroundColor.green},${p.backgroundColor.blue},${p.backgroundAlpha})`;
        const horizontalMargin = Math.min(.49, p.width*scale/(2*stage.clientWidth)+.02);
        const verticalMargin = Math.min(.49, Math.max(p.height*scale,laid.height*scale)/(2*stage.clientHeight)+.02);
        x = Math.max(horizontalMargin,Math.min(1-horizontalMargin,x));
        y = Math.max(verticalMargin,Math.min(1-verticalMargin,y));
        caption.style.left = x*100+'%'; caption.style.top = y*100+'%';
        caption.textContent = laid.text;
        caption.style.visibility = text ? 'visible' : 'hidden';
        sourceView.textContent = text || '—';
    }
    function show() {
        source.accept({stream:'preview', id:'sample', revision:++revision, text:input.value, expires_ms:8000}, Date.now());
        displayText = source.snapshot(Date.now())?.text || '';
        render(displayText);
    }
    function clear() { source.clear(); displayText = ''; render(''); }
    document.querySelectorAll('[data-preset]').forEach(button => button.onclick = () => {
        preset = button.dataset.preset;
        document.querySelectorAll('[data-preset]').forEach(item => item.setAttribute('aria-pressed', String(item === button)));
        render(displayText);
    });
    document.querySelector('#show').onclick = show;
    document.querySelector('#clear').onclick = clear;
    document.querySelector('#center').onclick = () => { x = y = .5; render(displayText); };
    hold.onchange = show;
    sample.onchange = () => { input.value = samples[sample.value]; show(); };
    input.oninput = show;
    caption.onpointerdown = event => {
        dragging = {x:event.clientX, y:event.clientY, left:x, top:y};
        caption.setPointerCapture(event.pointerId);
    };
    caption.onpointermove = event => {
        if (!dragging) return;
        x = Math.max(.1, Math.min(.9, dragging.left+(event.clientX-dragging.x)/stage.clientWidth));
        y = Math.max(.1, Math.min(.9, dragging.top+(event.clientY-dragging.y)/stage.clientHeight));
        render(displayText);
    };
    caption.onpointerup = caption.onpointercancel = () => { dragging = null; };
    caption.onkeydown = event => {
        if (!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(event.key)) return;
        event.preventDefault();
        if (event.key === 'ArrowLeft') x-=.02;
        if (event.key === 'ArrowRight') x+=.02;
        if (event.key === 'ArrowUp') y-=.02;
        if (event.key === 'ArrowDown') y+=.02;
        x=Math.max(.1,Math.min(.9,x)); y=Math.max(.1,Math.min(.9,y)); render(displayText);
    };
    window.addEventListener('resize', () => render(displayText));
    setInterval(() => {
        if (hold.checked || !displayText) return;
        if (!source.snapshot(Date.now())) clear();
    }, 100);
    input.value = samples.mixed; show();
}());
