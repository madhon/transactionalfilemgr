namespace ChinhDo.Transactions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Text;
    using System.Threading;
    using System.Transactions;

    /// <summary>
    /// File Resource Manager. Allows inclusion of file system operations in transactions (<see cref="System.Transactions"/>).
    /// https://www.chinhdo.com/20080825/transactional-file-manager/
    /// </summary>
    public sealed class TxFileManager : IFileManager
    {
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Create a new instance of <see cref="TxFileManager"/>.
        /// Uses the real file system and the system temp path.
        /// </summary>
        public TxFileManager()
            : this(new FileSystem())
        {
        }

        /// <summary>
        /// Create a new instance of <see cref="TxFileManager"/>.
        /// Uses the system temp path from <paramref name="fileSystem"/>.
        /// </summary>
        /// <param name="fileSystem">The file system abstraction to use.</param>
        public TxFileManager(IFileSystem fileSystem)
            : this(fileSystem, fileSystem.Path.GetTempPath())
        {
        }

        ///  <summary>Create a new instance of <see cref="TxFileManager"/> class. Feel free to create new instances or re-use existing instances.</summary>
        ///  <param name="fileSystem">The file system abstraction to use.</param>
        ///  <param name="tempPath">Path to temp directory.</param>
        public TxFileManager(IFileSystem fileSystem, string tempPath)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem), "fileSystem cannot be null.");
            }
            _fileSystem = fileSystem;

            this._tempPath = _fileSystem.Path.Combine(tempPath, "TxFileMgr-fc4eed76ee9b");

            // Only create directory if it doesn't exist to avoid unnecessary I/O
            if (!_fileSystem.Directory.Exists(_tempPath))
            {
                _fileSystem.Directory.CreateDirectory(_tempPath);
            }
        }
        
        
        

        #region IFileOperations

        public void AppendAllText(string path, string contents)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new AppendAllTextOperation(_fileSystem, this.GetTempPath, path, contents, null!));
            }
            else
            {
                _fileSystem.File.AppendAllText(path, contents);
            }
        }

        public void AppendAllText(string path, string contents, Encoding encoding)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new AppendAllTextOperation(_fileSystem, GetTempPath, path, contents, encoding));
            }
            else
            {
                _fileSystem.File.AppendAllText(path, contents, encoding);
            }
        }

        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new CopyOperation(_fileSystem, this.GetTempPath, sourceFileName, destFileName, overwrite));
            }
            else
            {
                OptimizedFileOperations.OptimizedCopy(_fileSystem, sourceFileName, destFileName, overwrite);
            }
        }

        public void CreateDirectory(string path)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new CreateDirectoryOperation(_fileSystem, GetTempPath, path));
            }
            else
            {
                _fileSystem.Directory.CreateDirectory(path);
            }
        }

        public void Delete(string path)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new DeleteFileOperation(_fileSystem, this.GetTempPath, path));
            }
            else
            {
                _fileSystem.File.Delete(path);
            }
        }

        public void DeleteDirectory(string path)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new DeleteDirectoryOperation(_fileSystem, this.GetTempPath, path));
            }
            else
            {
                _fileSystem.Directory.Delete(path, true);
            }
        }

        public void Move(string srcFileName, string destFileName)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new MoveFileOperation(_fileSystem, srcFileName, destFileName));
            }
            else
            {
                OptimizedFileOperations.OptimizedMove(_fileSystem, srcFileName, destFileName);
            }
        }

        public void MoveDirectory(string srcDirName, string destDirName) {
            if (IsInTransaction()) {
                EnlistOperation(new MoveDirectoryOperation(_fileSystem, srcDirName, destDirName));
            }
            else {
                _fileSystem.Directory.Move(srcDirName, destDirName);
            }
        }

        public void Snapshot(string fileName)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new SnapshotOperation(_fileSystem, this.GetTempPath, fileName));
            }
        }

        public void WriteAllText(string path, string contents)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new WriteAllTextOperation(_fileSystem, this.GetTempPath, path, contents));
            }
            else
            {
                _fileSystem.File.WriteAllText(path, contents);
            }
        }

        public void WriteAllText(string path, string contents, Encoding encoding)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new WriteAllTextOperation(_fileSystem, this.GetTempPath, path, contents, encoding));
            }
            else
            {
                _fileSystem.File.WriteAllText(path, contents, encoding);
            }
        }

        public void WriteAllBytes(string path, byte[] contents)
        {
            if (IsInTransaction())
            {
                EnlistOperation(new WriteAllBytesOperation(_fileSystem, this.GetTempPath, path, contents));
            }
            else
            {
                _fileSystem.File.WriteAllBytes(path, contents);
            }
        }

        #endregion

        /// <summary>Determines whether the specified path refers to a directory that exists on disk.</summary>
        /// <param name="path">The directory to check.</param>
        /// <returns>True if the directory exists.</returns>
        public bool DirectoryExists(string path)
        {
            return _fileSystem.Directory.Exists(path);
        }

        /// <summary>Determines whether the specified file exists.</summary>
        /// <param name="path">The file to check.</param>
        /// <returns>True if the file exists.</returns>
        public bool FileExists(string path)
        {
            return _fileSystem.File.Exists(path);
        }

        /// <summary>Gets the files in the specified directory.</summary>
        /// <param name="path">The directory to get files.</param>
        /// <param name="handler">The <see cref="FileEvent"/> object to call on each file found.</param>
        /// <param name="recursive">if set to <c>true</c>, include files in sub directories recursively.</param>
#pragma warning disable CA1822
        public void GetFiles(string path, FileEvent handler, bool recursive)
#pragma warning restore CA1822
        {
            
#if NETSTANDARD2_0
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

#endif
#if NET9_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(handler);
#endif

            if (!_fileSystem.Directory.Exists(path))
            {
                return;
            }

            // Use EnumerateFiles for better performance with large directories
            var files = recursive 
                ? _fileSystem.Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                : _fileSystem.Directory.EnumerateFiles(path);

            foreach (string fileName in files)
            {
                bool cancel = false;
                handler(fileName, ref cancel);
                if (cancel)
                {
                    return;
                }
            }
        }

        public string CreateTempFileName(string extension)
        {
            var g = Guid.NewGuid();
            string tempFolder = GetTempPath;
            string ret = _fileSystem.Path.Combine(tempFolder, g.ToString("N")[..16]) + extension;
            Snapshot(ret);
            return ret;
        }

        public string CreateTempFileName()
        {
            return CreateTempFileName(".tmp");
        }

        public string CreateTempDirectory()
        {
            return CreateTempDirectory(GetTempPath, string.Empty);
        }

        public string CreateTempDirectory(string parentDirectory, string prefix)
        {
            var g = Guid.NewGuid();
            string dirName = _fileSystem.Path.Combine(parentDirectory, prefix + g.ToString("N")[..16]);

            // Snapshot directory for rollback
            CreateDirectory(dirName);

            return dirName;
        }

        /// <summary>
        /// Gets the folder where we should store temporary files and folders. Override this if you want your own impl.
        /// </summary>
        /// <returns></returns>
        public string GetTempPath => this._tempPath;

        /// <summary>Get the count of _enlistments</summary>
        /// <returns></returns>
        public static int GetEnlistmentCount => _enlistments.Count;

        #region Private

        /// <summary>Dictionary of transaction enlistment objects for the current thread.</summary>
        //[ThreadStatic] <-- Is this needed?
#pragma warning disable S2223 // Non-constant static fields should not be visible
        internal static Dictionary<string, TxEnlistment> _enlistments = new Dictionary<string, TxEnlistment>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore S2223 // Non-constant static fields should not be visible
        
        #if NET9_0_OR_GREATER
        internal static readonly Lock _enlistmentsLock = new ();
        #else
        internal static readonly object _enlistmentsLock = new ();
        #endif
        
        
        
        
        private readonly string _tempPath;

        private static bool IsInTransaction()
        {
            return Transaction.Current != null;
        }

        private static void EnlistOperation(IRollbackableOperation operation)
        {
            Transaction tx = Transaction.Current;
            TxEnlistment enlistment;

            lock (_enlistmentsLock)
            {
                string txId = tx.TransactionInformation.LocalIdentifier;
                if (!_enlistments.TryGetValue(txId, out enlistment))
                {
                    enlistment = new TxEnlistment(tx);
                    _enlistments.Add(txId, enlistment);
                }

                enlistment.EnlistOperation(operation);
            }
        }

        #endregion
    }
}
