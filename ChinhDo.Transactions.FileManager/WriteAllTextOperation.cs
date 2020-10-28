﻿using System.IO;

namespace ChinhDo.Transactions
{
    using System.Text;

    /// <summary>
    /// Creates a file, and writes the specified contents to it.
    /// </summary>
    sealed class WriteAllTextOperation : SingleFileOperation
    {
        private readonly string contents;
        private readonly Encoding encoding;

        /// <summary>Instantiates the class.</summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="tempPath">Path to temp directory.</param>
        public WriteAllTextOperation(string tempPath, string path, string contents) : base(tempPath, path)
        {
            this.contents = contents;
        }

        /// <summary>Instantiates the class.</summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="encoding">The encoding to the file.</param>
        /// <param name="tempPath">Path to temp directory.</param>
        public WriteAllTextOperation(string tempPath, string path, string contents, Encoding encoding) : base(tempPath, path)
        {
            this.contents = contents;
            this.encoding = encoding;
        }

        public override void Execute()
        {
            if (File.Exists(path))
            {
                string temp = GetTempPathName(Path.GetExtension(path));
                File.Copy(path, temp);
                backupPath = temp;
            }

            if (encoding == null)
            {
                File.WriteAllText(path, contents);
            }
            else
            {
                File.WriteAllText(path, contents, encoding);
            }
        }
    }
}
