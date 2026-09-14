using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public static partial class IndexedDBInterop
    {
        public static async Task InitializeInteropAsync()
        {
            if (OperatingSystem.IsBrowser())
            {                
                await JSHost.ImportAsync("IndexedDBInterop", "./indexedDBInterop.js"); // /_framework/indexedDBInterop.js
            }                
        }

        //[JSImport("Object.keys", "globalThis")]
        //internal static partial string[] GetObjectKeys(JSObject obj);

        //[JSImport("Object.entries", "globalThis")]
        //internal static partial (string, object)[] GetObjectEntries(JSObject obj);

        //[JSImport("getLocation", "Main")]
        //public static partial Task<string> WindowLocationHrefAsync();

        [JSImport("initialize", "IndexedDBInterop")]
        public static partial Task InitializeAsync(string projectName);        

        //[JSImport("addData", "IndexedDBInterop")]
        //public static partial Task<bool> AddData(string dbName, string storeName, object data);

        //[JSImport("getData", "IndexedDBInterop")]
        //[return: JSMarshalAs<JSType.Promise<JSType.Object>>]
        //public static partial Task<object> GetData(string dbName, string storeName, string key);

        /// <summary>
        ///     Writes the file path entry and, when <paramref name="fileBlob"/> is not null, the
        ///     content under <paramref name="contentId"/>. Pass null for a file whose content is
        ///     already stored under that id by another path.
        /// </summary>
        [JSImport("saveFile", "IndexedDBInterop")]
        public static partial Task<bool> SaveFileAsync(string projectName, string fileId, string fileInfo, string contentId, byte[]? fileBlob);

        [JSImport("getFileInfo", "IndexedDBInterop")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static partial Task<object> GetFileInfoAsync(string projectName, string fileId);

        [JSImport("getFileInfos", "IndexedDBInterop")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static partial Task<object> GetFileInfosAsync(string projectName);

        /// <summary>
        ///     Contents already in the store, so the caller knows what it need not download.
        /// </summary>
        [JSImport("getContentIds", "IndexedDBInterop")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static partial Task<object> GetContentIdsAsync(string projectName);

        [JSImport("getFile", "IndexedDBInterop")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static partial Task<object> GetFileAsync(string projectName, string contentId);

        /// <summary>
        ///     Drops a file path only: other paths may still refer to the same content.
        /// </summary>
        [JSImport("deleteFile", "IndexedDBInterop")]
        public static partial Task<bool> DeleteFileAsync(string projectName, string fileId);

        [JSImport("deleteContent", "IndexedDBInterop")]
        public static partial Task<bool> DeleteContentAsync(string projectName, string contentId);
    }    
}