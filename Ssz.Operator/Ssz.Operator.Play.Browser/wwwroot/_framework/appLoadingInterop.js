// Bridge between the managed startup code and the loading overlay built in main.js.
// Every call is tolerant of a missing overlay: the page may already have hidden it.

export function setStatus(status) {
    globalThis.appLoading?.setStatus(status);
}

export function setProjectProgress(progressPercent, details) {
    globalThis.appLoading?.setProjectProgress(progressPercent, details);
}

export function hide() {
    globalThis.appLoading?.hide();
}

export function showError(message) {
    globalThis.appLoading?.showError(message);
}
