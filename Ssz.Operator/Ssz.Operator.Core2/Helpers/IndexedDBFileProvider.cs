using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Ssz.Operator.Core;

namespace Ssz.Operator.Core
{
    public class IndexedDBFileProvider : IFileProvider
    {
        public IndexedDBFileProvider(IndexedDBDirectory rootIndexedDBDirectory)
        {
            RootIndexedDBDirectory = rootIndexedDBDirectory;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            IndexedDBDirectory? indexedDBDirectory = RootIndexedDBDirectory;

            if (String.IsNullOrEmpty(subpath))
                return indexedDBDirectory;

            foreach (var directoryName in subpath.Split(Path.DirectorySeparatorChar))
            {
                if (String.IsNullOrEmpty(directoryName))
                    continue;

                if (!indexedDBDirectory.ChildIndexedDBDirectoriesDictionary.TryGetValue(directoryName, out indexedDBDirectory))
                    return new NonexistentDirectoryContents();
            }

            return indexedDBDirectory;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (String.IsNullOrEmpty(subpath))
                return new NonexistentFileInfo(subpath);

            IndexedDBDirectory? indexedDBDirectory = RootIndexedDBDirectory;

            var parts = subpath.Split(Path.DirectorySeparatorChar).ToArray();

            foreach (var directoryName in parts.Take(parts.Length - 1))
            {
                if (String.IsNullOrEmpty(directoryName))
                    continue;

                if (!indexedDBDirectory.ChildIndexedDBDirectoriesDictionary.TryGetValue(directoryName, out indexedDBDirectory))
                    return new NonexistentFileInfo(subpath);
            }

            if (!indexedDBDirectory.IndexedDBFilesDictionary.TryGetValue(parts[parts.Length - 1], out IndexedDBFile? indexedDBFile))
                return new NonexistentFileInfo(subpath);

            return indexedDBFile;
        }

        public IChangeToken Watch(string filter)
        {            
            return NonexistentChangeToken.Singleton;
        }

        public IndexedDBDirectory RootIndexedDBDirectory { get; }
    }

    internal class NonexistentDirectoryContents : IDirectoryContents
    {
        public bool Exists => false;        

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return Enumerable.Empty<IFileInfo>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class NonexistentFileInfo : IFileInfo
    {
        public NonexistentFileInfo(string name)
        {
            Name = name;
        }

        public bool Exists => false;        

        public bool IsDirectory => false;        

        public DateTimeOffset LastModified => DateTimeOffset.MinValue;

        public long Length => -1;

        public string Name { get; }

        public string? PhysicalPath => null;

        public Stream CreateReadStream()
        {
            throw new FileNotFoundException(Name);
        }
    }

    internal class NonexistentChangeToken : IChangeToken
    {
        public static NonexistentChangeToken Singleton => new NonexistentChangeToken();

        public bool ActiveChangeCallbacks => false;

        public bool HasChanged => false;        

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            throw new NotImplementedException();
        }
    }
}