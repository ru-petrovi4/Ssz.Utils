let CURRENT_VERSION = 3;
let DATABASE_NAME = "Ssz.Operator";
let FILES_STORE_NAME = "Files";
let FILEINFOS_STORE_NAME = "FileInfos";

export function initialize(projectName) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DATABASE_NAME, CURRENT_VERSION);
        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(projectName + FILES_STORE_NAME)) {
                db.createObjectStore(projectName + FILES_STORE_NAME, { keyPath: "id" });
            }
            if (!db.objectStoreNames.contains(projectName + FILEINFOS_STORE_NAME)) {
                db.createObjectStore(projectName + FILEINFOS_STORE_NAME, { keyPath: "id" });
            }
        };
        request.onsuccess = (event) => {
            resolve(event.target.result);
        };
        request.onerror = (event) => {
            reject(event.target.error);
        };
    });
}

export function openDatabase() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DATABASE_NAME, CURRENT_VERSION);
        request.onsuccess = (event) => {
            resolve(event.target.result);
        };
        request.onerror = (event) => {
            reject(event.target.error);
        };
    });
}

export function saveFile(projectName, fileId, fileInfo, fileBlob) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction0 = db.transaction(projectName + FILEINFOS_STORE_NAME, "readwrite");
            const store0 = transaction0.objectStore(projectName + FILEINFOS_STORE_NAME)
            store0.put({ id: fileId, fileInfo: fileInfo });

            const transaction = db.transaction(projectName + FILES_STORE_NAME, "readwrite");
            const store = transaction.objectStore(projectName + FILES_STORE_NAME);
            const request = store.put({ id: fileId, file: fileBlob });
            request.onsuccess = () => resolve(true);
            request.onerror = (event) => reject(event.target.error);
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
            request.onerror = () => reject("Error");            
        });
    });
}

export function getFile(projectName, fileId) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(projectName + FILES_STORE_NAME, "readonly");
            const store = transaction.objectStore(projectName + FILES_STORE_NAME);
            const request = store.get(fileId);            
            request.onsuccess = (event) => resolve(event.target.result);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}

export function deleteFile(projectName, fileId) {
    return openDatabase().then((db) => {
        return new Promise((resolve, reject) => {
            const transaction0 = db.transaction(projectName + FILEINFOS_STORE_NAME, "readwrite");
            const store0 = transaction0.objectStore(projectName + FILEINFOS_STORE_NAME);
            store0.delete(fileId);

            const transaction = db.transaction(projectName + FILES_STORE_NAME, "readwrite");
            const store = transaction.objectStore(projectName + FILES_STORE_NAME);
            const request = store.delete(fileId);
            request.onsuccess = () => resolve(true);
            request.onerror = (event) => reject(event.target.error);
        });
    });
}