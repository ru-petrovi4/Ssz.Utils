// Bridge between the managed code and the page the application is embedded into: the parent
// sends the user's access token when the application starts and again every time it renews the
// session, so a long running mnemonic scheme never ends up holding an expired token.
//
// The token is passed in a message rather than in the query string: the address is kept in the
// browser history, in server logs and in the Referer header, and it cannot be updated without
// reloading the application.

const READY_MESSAGE = 'sszPlay:ready';
const ACCESS_TOKEN_MESSAGE = 'sszPlay:accessToken';

let onAccessTokenChanged = null;
let accessToken = null;

// Subscribed on module load rather than in subscribe(): the token can arrive before the managed
// side has subscribed, in which case it is handed over by subscribe() below.
globalThis.addEventListener('message', event => {
    // Only the page the application is embedded into is allowed to supply the token.
    if (event.source !== globalThis.parent) return;

    const data = event.data;
    if (!data || data.type !== ACCESS_TOKEN_MESSAGE || typeof data.accessToken !== 'string') return;
    if (data.accessToken === accessToken) return;

    accessToken = data.accessToken;

    try {
        onAccessTokenChanged?.(accessToken);
    } catch (e) {
        // A failing subscriber must not stop the next token from being delivered.
        console.error(e);
    }
});

// Called once from managed code. The callback is invoked for the current token, if one has
// already arrived, and then for every new one.
export function subscribe(callback) {
    onAccessTokenChanged = callback;

    if (accessToken !== null) callback(accessToken);

    notifyReady();
}

export function getAccessToken() {
    return accessToken ?? '';
}

// The parent cannot know when the application has finished loading, so it sends the first token
// in reply to this message. The message itself carries no secret, hence the open targetOrigin.
function notifyReady() {
    if (globalThis.parent === globalThis) return; // not embedded - there is no one to ask

    globalThis.parent.postMessage({ type: READY_MESSAGE }, '*');
}
