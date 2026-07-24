using System;
using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>
    /// Class that contains common code for those rollbackable file operations which need
    /// to backup a single file and restore it when Rollback() is called.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "<Pending>")]
    abstract class SingleFileOperation : IoOperation, IRollbackableOperation, IDisposable
    {
        protected readonly string path;
        protected string? backupPath;
        // tracks whether Dispose has been called
        private bool disposed;

        /// <summary>Constructor</summary>
        /// <param name="tempPath">Path to temp directory.</param>
        /// <param name="path">Path to the file</param>
        protected SingleFileOperation(IFileSystem fileSystem,  string tempPath, string path) : base(fileSystem, tempPath)
        {
            this.path = path;
        }

        /// <summary>
        /// Disposes the resources used by this class.
        /// </summary>
        ~SingleFileOperation()
        {
            InnerDispose();
        }

        public abstract void Execute();

        public void Rollback()
        {
            if (backupPath != null)
            {
                string directory = _fileSystem.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !_fileSystem.Directory.Exists(directory))
                {
                    OptimizedFileOperations.OptimizedCreateDirectory(_fileSystem, directory);
                }
                OptimizedFileOperations.OptimizedCopy(_fileSystem, backupPath, path, true);
            }
            else
            {
                if (_fileSystem.File.Exists(path))
                {
                    _fileSystem.File.Delete(path);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            InnerDispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resources of this class.
        /// </summary>
        private void InnerDispose()
        {
            if (!disposed)
            {
                if (backupPath != null)
                {
                    var fi = _fileSystem.FileInfo.New(backupPath);
                    if (fi.IsReadOnly)
                    {
                        fi.Attributes = FileAttributes.Normal;
                    }
                    _fileSystem.File.Delete(backupPath);
                }

                disposed = true;
            }
        }
    }
}
