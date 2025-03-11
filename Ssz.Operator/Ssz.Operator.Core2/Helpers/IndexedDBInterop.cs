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

        [JSImport("saveFile", "IndexedDBInterop")]
        public static partial Task<bool> SaveFileAsync(string projectName, string fileId, string fileInfo, byte[] fileBlob);

        [JSImport("getFileInfo", "IndexedDBInterop")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static partial Task<object> GetFileInfoAsync(string projectName, string fileId);

        [JSImport("getFileInfos", "IndexedDBInterop")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static partial Task<object> GetFileInfosAsync(string projectName);

        [JSImport("getFile", "IndexedDBInterop")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static partial Task<object> GetFileAsync(string projectName, string fileId);

        [JSImport("deleteFile", "IndexedDBInterop")]
        public static partial Task<bool> DeleteFileAsync(string projectName, string fileId);
    }    
}