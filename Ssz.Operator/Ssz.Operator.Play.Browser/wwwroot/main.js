import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const loading = createLoadingIndicator();

try {
    loading.setStatus('Загрузка файлов интерфейса...');

    const dotnetRuntime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .withModuleConfig({
            onDownloadResourceProgress: (loadedCount, totalCount) => loading.setProgress(loadedCount, totalCount)
        })
        .create();

    loading.setStatus('Запуск приложения...');

    const config = dotnetRuntime.getConfig();
    const mainTask = dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

    // Whichever happens first: the Avalonia canvas appears, or Main() returns.
    await Promise.race([loading.waitForAvaloniaCanvas(), mainTask]);
    loading.hide();

    await mainTask;
} catch (e) {
    loading.showError(e);
    throw e;
}

function createLoadingIndicator() {
    const root = document.getElementById('app-loading');
    const track = document.getElementById('app-loading-track');
    const fill = document.getElementById('app-loading-fill');
    const status = document.getElementById('app-loading-status');
    const percentText = document.getElementById('app-loading-percent');
    const filesText = document.getElementById('app-loading-files');
    const errorText = document.getElementById('app-loading-error');

    // The runtime reports the resource count it knows about so far, so the total
    // grows while loading. Keep the shown value monotonic to avoid a jumping bar.
    let shownPercent = 0;
    let maxTotal = 0;
    let hidden = false;
    let failed = false;
    let pendingFrame = 0;

    root.classList.add('app-loading--indeterminate');

    const render = (loadedCount) => {
        pendingFrame = 0;
        root.classList.remove('app-loading--indeterminate');
        fill.style.width = shownPercent + '%';
        percentText.textContent = shownPercent + '%';
        filesText.textContent = loadedCount + ' / ' + maxTotal;
        track.setAttribute('aria-valuenow', String(shownPercent));
    };

    const api = {
        setStatus(text) {
            if (!failed) status.textContent = text;
        },

        setProgress(loadedCount, totalCount) {
            if (hidden || failed) return;

            if (totalCount > maxTotal) maxTotal = totalCount;
            if (maxTotal <= 0) return;

            // Cap at 99%: the last resources are still being requested.
            const percent = Math.min(99, Math.floor((loadedCount / maxTotal) * 100));
            if (percent > shownPercent) shownPercent = percent;

            if (pendingFrame === 0)
                pendingFrame = requestAnimationFrame(() => render(loadedCount));
        },

        hide() {
            if (hidden || failed) return;
            hidden = true;

            if (pendingFrame !== 0) {
                cancelAnimationFrame(pendingFrame);
                pendingFrame = 0;
            }

            shownPercent = 100;
            root.classList.remove('app-loading--indeterminate');
            fill.style.width = '100%';
            percentText.textContent = '100%';
            track.setAttribute('aria-valuenow', '100');

            root.classList.add('app-loading--closing');
            root.addEventListener('transitionend', () => root.classList.add('app-loading--closed'), { once: true });
            // Fallback in case the transition never fires (reduced motion, hidden tab).
            setTimeout(() => root.classList.add('app-loading--closed'), 1000);
        },

        showError(error) {
            failed = true;
            hidden = false;
            root.classList.remove('app-loading--indeterminate', 'app-loading--closing', 'app-loading--closed');
            status.textContent = 'Не удалось загрузить приложение';
            errorText.textContent = (error && (error.stack || error.message)) || String(error);
            errorText.hidden = false;
            console.error(error);
        },

        // Resolves once Avalonia has attached its canvas and the browser painted it.
        waitForAvaloniaCanvas() {
            const host = document.getElementById('out');
            const found = () => host.querySelector('canvas.avalonia-canvas');

            return new Promise(resolve => {
                const finish = () => requestAnimationFrame(() => requestAnimationFrame(resolve));

                if (found()) {
                    finish();
                    return;
                }

                const observer = new MutationObserver(() => {
                    if (!found()) return;
                    observer.disconnect();
                    finish();
                });
                observer.observe(host, { childList: true, subtree: true });
            });
        }
    };

    // Available to managed code via JS interop, e.g. to report later startup stages.
    globalThis.appLoading = api;
    return api;
}
