import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// These stages run before .NET exists, so their texts cannot come from the application
// resources. They follow navigator.language the same way the managed side does, otherwise the
// overlay would switch language halfway through loading.
const TEXTS = {
    en: {
        preparing: 'Preparing to load...',
        downloadingApp: 'Downloading application files...',
        startingApp: 'Starting the application...',
        failed: 'Could not load the application'
    },
    ru: {
        preparing: 'Подготовка к загрузке...',
        downloadingApp: 'Загрузка файлов приложения...',
        startingApp: 'Запуск приложения...',
        failed: 'Не удалось загрузить приложение'
    }
};
const texts = TEXTS[(navigator.language || 'en').split('-')[0].toLowerCase()] ?? TEXTS.en;

const loading = createLoadingIndicator();

try {
    loading.setStatus(texts.downloadingApp);

    const dotnetRuntime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        // The runtime does not take the culture from the browser on its own, and it loads
        // satellite assemblies only when asked to - without both of these the app always falls
        // back to the neutral (English) resources. The project ships one extra culture, so
        // loading them all costs a few KB.
        .withApplicationCulture(navigator.language)
        .withConfig({ loadAllSatelliteResources: true })
        .withModuleConfig({
            onDownloadResourceProgress: (loadedCount, totalCount) => loading.setAppProgress(loadedCount, totalCount)
        })
        .create();

    loading.setStatus(texts.startingApp);

    const config = dotnetRuntime.getConfig();
    const mainTask = dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

    // From here on the managed startup code owns the overlay: it reports the project files it
    // downloads and hides the overlay once the start page is up (see AppLoadingInterop).
    // runMain() returns as soon as Avalonia is set up, long before that, so it is not a signal.
    loading.armFallbackHide();

    await mainTask;
} catch (e) {
    loading.showError(e);
    throw e;
}

function createLoadingIndicator() {
    // The two stages of a cold start, and how much of the bar each one gets. The app files are
    // the bigger download, the project files are usually served from the IndexedDB cache.
    const APP_STAGE_SHARE = 0.6;
    const PROJECT_STAGE_SHARE = 1 - APP_STAGE_SHARE;

    // If the managed side never takes over (JS module missing, or an old build), hide the overlay
    // this long after the UI appears instead of leaving it on screen forever.
    const FALLBACK_HIDE_MS = 15000;

    const root = document.getElementById('app-loading');
    const track = document.getElementById('app-loading-track');
    const fill = document.getElementById('app-loading-fill');
    const status = document.getElementById('app-loading-status');
    const percentText = document.getElementById('app-loading-percent');
    const errorText = document.getElementById('app-loading-error');

    let appFraction = 0;
    let projectFraction = 0;
    let projectStageStarted = false;
    let hidden = false;
    let failed = false;
    let pendingFrame = 0;
    let fallbackTimer = 0;

    root.classList.add('app-loading--indeterminate');
    status.textContent = texts.preparing;

    const render = () => {
        pendingFrame = 0;
        const percent = Math.round(
            100 * (appFraction * APP_STAGE_SHARE + projectFraction * PROJECT_STAGE_SHARE));
        root.classList.remove('app-loading--indeterminate');
        fill.style.width = percent + '%';
        percentText.textContent = percent + '%';
        track.setAttribute('aria-valuenow', String(percent));
    };

    const scheduleRender = () => {
        if (pendingFrame === 0 && !hidden && !failed)
            pendingFrame = requestAnimationFrame(render);
    };

    // The managed side is driving now, so the fallback must not fire.
    const claim = () => {
        projectStageStarted = true;
        appFraction = 1;
        if (fallbackTimer !== 0) {
            clearTimeout(fallbackTimer);
            fallbackTimer = 0;
        }
    };

    const api = {
        setStatus(text) {
            if (!failed) status.textContent = text;
        },

        // Called from managed code. It also means the application has taken the overlay over, so
        // the fallback below must not hide it while the application is still working: a status
        // like "connecting to the server" has to stay on screen instead of turning into a blank
        // page.
        setManagedStatus(text) {
            claim();
            api.setStatus(text);
        },

        // Stage 1: .NET runtime and assemblies. The runtime reports the resource count it knows
        // about so far, so the total grows while loading - keep the shown value monotonic.
        setAppProgress(loadedCount, totalCount) {
            if (hidden || failed || projectStageStarted || totalCount <= 0) return;

            // Cap below 1: the last resources are still being requested.
            const fraction = Math.min(0.99, loadedCount / totalCount);
            if (fraction > appFraction) appFraction = fraction;
            scheduleRender();
        },

        // Stage 2: project pages and other files, reported by the managed startup code.
        setProjectProgress(progressPercent) {
            if (hidden || failed) return;

            claim();
            const fraction = Math.min(1, Math.max(0, progressPercent / 100));
            if (fraction > projectFraction) projectFraction = fraction;
            scheduleRender();
        },

        hide() {
            if (hidden || failed) return;
            hidden = true;
            claim();

            if (pendingFrame !== 0) {
                cancelAnimationFrame(pendingFrame);
                pendingFrame = 0;
            }

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
            if (fallbackTimer !== 0) {
                clearTimeout(fallbackTimer);
                fallbackTimer = 0;
            }
            root.classList.remove('app-loading--indeterminate', 'app-loading--closing', 'app-loading--closed');
            status.textContent = texts.failed;
            errorText.textContent = (error && (error.stack || error.message)) || String(error);
            errorText.hidden = false;
            console.error(error);
        },

        // Hide once the Avalonia canvas has been painted for a while with no word from the
        // managed side. Cancelled as soon as it reports anything.
        armFallbackHide() {
            const host = document.getElementById('out');
            const found = () => host.querySelector('canvas.avalonia-canvas');

            const start = () => {
                if (projectStageStarted || hidden || failed) return;
                fallbackTimer = setTimeout(() => {
                    fallbackTimer = 0;
                    if (!projectStageStarted) api.hide();
                }, FALLBACK_HIDE_MS);
            };

            if (found()) {
                start();
                return;
            }

            const observer = new MutationObserver(() => {
                if (!found()) return;
                observer.disconnect();
                start();
            });
            observer.observe(host, { childList: true, subtree: true });
        }
    };

    // Used from managed code through _framework/appLoadingInterop.js.
    globalThis.appLoading = api;
    return api;
}
