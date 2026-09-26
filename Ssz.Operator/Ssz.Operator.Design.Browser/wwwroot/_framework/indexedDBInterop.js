// Schema 4: the Files store is keyed by a content id - the file name plus its modification time -
// instead of by file path. A project cannot hold two different files with that pair equal, so a
// file repeated in several directories is downloaded and stored only once. FileInfos still maps a
// file path to its modification time, which is what yields the content id.
//
// The schema number is kept in the Meta store rather than in the IndexedDB version, because the
// version also has to go up whenever a project is opened for the first time: IndexedDB only
// allows object stores to be created inside an upgrade.
let SCHEMA_VERSION = 4;
let DATABASE_NAME = "Ssz.Operator";
let FILES_STORE_NAME = "Files";
let FILEINFOS_STORE_NAME = "FileInfos";
let META_STORE_NAME = "Meta";
let SCHEMA_VERSION_KEY = "schemaVersion";

// One shared connection per page. Opening a fresh one per call would leak connections and, worse,
// block every later upgrade.
let databasePromise = null;

function open(version, upgrade) {
    return new Promise((resolve, reject) => {
        const request = version === undefined
            ? indexedDB.open(DATABASE_NAME)
            : indexedDB.open(DATABASE_NAME, version);
        if (upgrade) {
            request.onupgradeneeded = (event) => upgrade(event.target.result);
        }
        request.onsuccess = (event) => resolve(event.target.result);
        request.onerror = (event) => reject(event.target.error);
        request.onblocked = () => reject(new Error("IndexedDB upgrade is blocked by another tab of this application"));
    });
}

function setDatabase(db) {
    // Do not hold an upgrade in another tab hostage: step aside and reopen on the next call.
    db.onversionchange = () => {
        databasePromise = null;
        db.close();
    };
    db.onclose = () => {
        databasePromise = null;
    };
    databasePromise = Promise.resolve(db);
}

async function closeDatabase() {
    if (databasePromise === null)
        return;
    const db = await databasePromise.catch(() => null);
    databasePromise = null;
    if (db)
        db.close();
}

function projectStoreNames(projectName) {
    return [projectName + FILES_STORE_NAME, projectName + FILEINFOS_STORE_NAME];
}

function hasProjectStores(db, projectName) {
    return projectStoreNames(projectName).every((storeName) => db.objectStoreNames.contains(storeName));
}

function createProjectStores(db, projectName) {
    for (const storeName of projectStoreNames(projectName)) {
        if (!db.objectStoreNames.contains(storeName)) {
            db.createObjectStore(storeName, { keyPath: "id" });
        }
    }
}

function readSchemaVersion(db) {
    if (!db.objectStoreNames.contains(META_STORE_NAME))
        return Promise.resolve(0);
    return new Promise((resolve) => {
        const request = db.transaction(META_STORE_NAME, "readonly").objectStore(META_STORE_NAME).get(SCHEMA_VERSION_KEY);
        request.onsuccess = () => resolve(request.result ? request.result.value : 0);
        request.onerror = () => resolve(0);
    });
}

// Reopens one version up, which is the only moment object stores may be created or dropped.
async function upgrade(db, apply) {
    const version = db.version + 1;
    db.close();
    return await open(version, apply);
}

export async function initialize(projectName) {
    // An upgrade cannot run while a connection is open, including one made for another project.
    await closeDatabase();

    let db = await open(undefined, null);

    if (await readSchemaVersion(db) < SCHEMA_VERSION) {
        db = await upgrade(db, (upgraded) => {
            // Stores of an older schema cannot be reused - their Files keys were paths. This is
            // only a cache, so dropping it just makes this start download the project again.
            for (const storeName of Array.from(upgraded.objectStoreNames)) {
                upgraded.deleteObjectStore(storeName);
            }
            upgraded.createObjectStore(META_STORE_NAME, { keyPath: "id" })
                .put({ id: SCHEMA_VERSION_KEY, value: SCHEMA_VERSION });
            createProjectStores(upgraded, projectName);
        });
    } else if (!hasProjectStores(db, projectName)) {
        // First time this project is opened in this browser.
        db = await upgrade(db, (upgraded) => createProjectStores(upgraded, projectName));
    }

    setDatabase(db);
}

export async function openDatabase() {
    if (databasePromise === null) {
        setDatabase(await open(undefined, null));
    }
    return await databasePromise;
}

// fileBlob may be null: the content is already stored under contentId by another file path, and
// only the path entry has to be written.
export function saveFile(projectName, fileId, fileInfo, contentId, fileBlob) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const storeNames = fileBlob
                ? [projectName + FILEINFOS_STORE_NAME, projectName + FILES_STORE_NAME]
                : [projectName + FILEINFOS_STORE_NAME];
            const transaction = db.transaction(storeNames, "readwrite");

            transaction.objectStore(projectName + FILEINFOS_STORE_NAME).put({ id: fileId, fileInfo: fileInfo });
            if (fileBlob) {
                transaction.objectStore(projectName + FILES_STORE_NAME).put({ id: contentId, file: fileBlob });
            }

            // One transaction for both stores, so a path is never recorded without its content.
            transaction.oncomplete = () => resolve(true);
            transaction.onerror = (event) => reject(event.target.error);
        });
    });
}

export function getFileInfo(projectName, fileId) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(projectName + FILEINFOS_STORE_NAME, "readonly");
            const store = transaction.objectStore(projectName + FILEINFOS_STORE_NAME);
            const request = store.get(fileId);
            request.onsuccess = (event) => resolve(event.target.result);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}

export function getFileInfos(projectName) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(projectName + FILEINFOS_STORE_NAME, "readonly");
            const store = transaction.objectStore(projectName + FILEINFOS_STORE_NAME);
            const request = store.getAll();
            request.onsuccess = () => resolve(request.result);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}

// The content ids already held by the store, so the caller knows what it need not download again.
export function getContentIds(projectName) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(projectName + FILES_STORE_NAME, "readonly");
            const store = transaction.objectStore(projectName + FILES_STORE_NAME);
            const request = store.getAllKeys();
            request.onsuccess = () => resolve(request.result);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}

export function getFile(projectName, contentId) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(projectName + FILES_STORE_NAME, "readonly");
            const store = transaction.objectStore(projectName + FILES_STORE_NAME);
            const request = store.get(contentId);
            request.onsuccess = (event) => resolve(event.target.result);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}

// Drops a file path only. The content it referred to is dropped by deleteContent, because other
// paths may still be using it.
export function deleteFile(projectName, fileId) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(projectName + FILEINFOS_STORE_NAME, "readwrite");
            const store = transaction.objectStore(projectName + FILEINFOS_STORE_NAME);
            const request = store.delete(fileId);
            request.onsuccess = () => resolve(true);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}

export function deleteContent(projectName, contentId) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(projectName + FILES_STORE_NAME, "readwrite");
            const store = transaction.objectStore(projectName + FILES_STORE_NAME);
            const request = store.delete(contentId);
            request.onsuccess = () => resolve(true);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}
