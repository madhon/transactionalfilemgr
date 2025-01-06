namespace ChinhDo.Transactions.FileManagerTest
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using FluentAssertions;
    using Xunit;

    public sealed class FileManagerTests : IDisposable
    {
        private readonly IFileManager _target;
        private IList<string> _tempPaths;

        public FileManagerTests()
        {
            _target = new TxFileManager();
            _tempPaths = new List<string>();
            // TODO delete any temp files
        }

        public void Dispose()
        {
            // Delete temp files/dirs
            foreach (string item in _tempPaths)
            {
                if (File.Exists(item))
                {
                    File.Delete(item);
                }

                if (Directory.Exists(item))
                {
                    Directory.Delete(item);
                }
            }

            var numTempFiles = Directory.GetFiles(Path.Combine(Path.GetTempPath(), "TxFileMgr-fc4eed76ee9b")).Length;

            numTempFiles.Should().Be(0);
        }

        #region Operations

        [Fact]
        public void CanAppendAllText()
        {
            var f1 = GetTempPathName();
            const string contents = "123";

            using (var scope1 = new TransactionScope())
            {
                _target.AppendAllText(f1, contents);
                scope1.Complete();
            }

            File.ReadAllText(f1).Should().Be(contents);
        }

        [Fact]
        public void AppendAllTexHandlesException()
        {
            var f1 = GetTempPathName();
            const string contents = "123";

            using (var scope1 = new TransactionScope())
            {
                using (var fs = File.Open(f1, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
                {
                    Action act = () => _target.AppendAllText(f1, contents);

                    act.Should().Throw<IOException>()
                        .Where(e => e.Message.Contains("The process cannot access the file"));
                }
            }
        }

        [Fact]
        public void AppendAllTextCanRollback()
        {
            var f1 = GetTempPathName();
            const string contents = "qwerty";
            using (var sc1 = new TransactionScope())
            {
                _target.AppendAllText(f1, contents);
                // without specifically committing, this should rollback
            }

            File.Exists(f1).Should().BeFalse($"{f1} should not exist");
        }

        [Fact]
        public void CanCopy()
        {
            var sourceFileName = GetTempPathName();
            var destFileName = GetTempPathName();

            const string expectedText = "Test 123.";
            using (var scope1 = new TransactionScope())
            {
                File.WriteAllText(sourceFileName, expectedText);
                _target.Copy(sourceFileName, destFileName, false);
                scope1.Complete();
            }

            File.ReadAllText(sourceFileName).Should().Be(expectedText);
            File.ReadAllText(destFileName).Should().Be(expectedText);
        }

        [Fact]
        public void CanCopyAndRollback()
        {
            var sourceFileName = GetTempPathName();
            const string expectedText = "Hello 123.";
            File.WriteAllText(sourceFileName, expectedText);
            var destFileName = GetTempPathName();

            using (var scope1 = new TransactionScope())
            {
                _target.Copy(sourceFileName, destFileName, false);
                // without specifically committing, this should rollback
            }

            File.Exists(destFileName).Should().BeFalse($"{destFileName} should not exist");
        }

        [Fact]
        public void CanHandleCopyErrors()
        {
            var f1 = GetTempPathName();
            var f2 = GetTempPathName();

            using (var fs = new FileStream(f2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                using (var scope1 = new TransactionScope())
                {
                    _target.WriteAllText(f1, "test");

                    Action act = () => _target.Copy(f1, f2, false);
                    act.Should().Throw<IOException>();
                    //rollback
                }
            }

            File.Exists(f1).Should().BeFalse();
        }

        [Fact]
        public void CanCreateDirectory()
        {
            var d1 = GetTempPathName();
            using (var scope1 = new TransactionScope())
            {
                _target.CreateDirectory(d1);
                scope1.Complete();
            }

            Directory.Exists(d1).Should().BeTrue();
        }

        /// <summary>
        /// Validate that we are able to create nested directotories and roll them back.
        /// </summary>
        [Fact]
        public void CanRollbackNestedDirectories()
        {
            var baseDir = GetTempPathName();
            Directory.CreateDirectory(baseDir);
            var nested = Path.Combine(baseDir, "level1");
            nested = Path.Combine(nested, "level2");
            using (new TransactionScope())
            {
                _target.CreateDirectory(nested);
                Directory.Exists(nested).Should().BeTrue();
            }
            
            Directory.Exists(nested).Should().BeFalse();
            Directory.Exists(baseDir).Should().BeTrue();
            Directory.Delete(baseDir);
        }

        [Fact]
        public void CanCreateDirectoryAndRollback()
        {
            var d1 = GetTempPathName();
            using (var scope1 = new TransactionScope())
            {
                _target.CreateDirectory(d1);
            }

            Directory.Exists(d1).Should().BeFalse();
        }

        [Fact]
        public void CanDeleteDirectory()
        {
            var f1 = GetTempPathName();
            Directory.CreateDirectory(f1);

            using (var scope1 = new TransactionScope())
            {
                _target.DeleteDirectory(f1);
                scope1.Complete();
            }
            Directory.Exists(f1).Should().BeFalse();
        }

        [Fact]
        public void CanDeleteDirectoryAndRollback()
        {
            var f1 = GetTempPathName();
            Directory.CreateDirectory(f1);

            using (var scope1 = new TransactionScope())
            {
                _target.DeleteDirectory(f1);
            }

            Directory.Exists(f1).Should().BeTrue();
        }

        [Fact]
        public void CanDeleteFile()
        {
            var f1 = GetTempPathName();
            const string contents = "abc";
            File.WriteAllText(f1, contents);

            using (var scope1 = new TransactionScope())
            {
                _target.Delete(f1);
                scope1.Complete();
            }
            File.Exists(f1).Should().BeFalse();
        }

        [Fact]
        public void CanDeleteFileAndRollback()
        {
            var f1 = GetTempPathName();
            const string contents = "abc";
            File.WriteAllText(f1, contents);

            using (var scope1 = new TransactionScope())
            {
                _target.Delete(f1);
            }

            File.Exists(f1).Should().BeTrue();
            File.ReadAllText(f1).Should().Be(contents);
        }

        [Fact]
        public void CanMoveFile()
        {
            const string contents = "abc";
            var f1 = GetTempPathName();
            var f2 = GetTempPathName();
            File.WriteAllText(f1, contents);

            using (var scope1 = new TransactionScope())
            {
                File.Exists(f1).Should().BeTrue();
                File.Exists(f2).Should().BeFalse();
                _target.Move(f1, f2);
                scope1.Complete();
            }
        }

        [Fact]
        public void CanMoveFileAndRollback()
        {
            const string contents = "abc";
            var f1 = GetTempPathName();
            var f2 = GetTempPathName();
            File.WriteAllText(f1, contents);

            using (new TransactionScope())
            {
                File.Exists(f1).Should().BeTrue();
                File.Exists(f2).Should().BeFalse();
                _target.Move(f1, f2);
            }

            File.ReadAllText(f1).Should().Be(contents);
            File.Exists(f2).Should().BeFalse();
        }
        
        [Fact]
        public void CanMoveDirectory() {
            var f1 = GetTempPathName();
            var f2 = GetTempPathName();
            Directory.CreateDirectory(f1);

            using (var scope1 = new TransactionScope()) {
                Directory.Exists(f1).Should().BeTrue();
                Directory.Exists(f2).Should().BeFalse();
                _target.MoveDirectory(f1, f2);
                scope1.Complete();
            }

            Directory.Exists(f1).Should().BeFalse();
            Directory.Exists(f2).Should().BeTrue();
        }

        [Fact]
        public void CanMoveDirectoryAndRollback() {
            var f1 = GetTempPathName();
            var f2 = GetTempPathName();
            Directory.CreateDirectory(f1);

            using (new TransactionScope()) {
                Directory.Exists(f1).Should().BeTrue();
                Directory.Exists(f2).Should().BeFalse();
                _target.MoveDirectory(f1, f2);
            }

            Directory.Exists(f1).Should().BeTrue();
            Directory.Exists(f2).Should().BeFalse();
        }

        [Fact]
        public void CanSnapshot()
        {
            var f1 = GetTempPathName();

            using (var scope1 = new TransactionScope())
            {
                _target.Snapshot(f1);
                _target.AppendAllText(f1, "<test></test>");
            }

            File.Exists(f1).Should().BeFalse();
        }

        [Fact]
        public void CanWriteAllText()
        {
            var f1 = GetTempPathName();
            const string contents = "abcdef";
            File.WriteAllText(f1, "123");

            using (var scope1 = new TransactionScope())
            {
                _target.WriteAllText(f1, contents);
                scope1.Complete();
            }

            File.ReadAllText(f1).Should().Be(contents);
        }

        [Fact]
        public void CanWriteAllTextAndRollback()
        {
            var f1 = GetTempPathName();
            const string contents1 = "123";
            const string contents2 = "abcdef";
            File.WriteAllText(f1, contents1);

            using (var scope1 = new TransactionScope())
            {
                _target.WriteAllText(f1, contents2);
            }

            File.ReadAllText(f1).Should().Be(contents1);
        }


        [Fact]
        public void CanWriteAllTextWithEncoding()
        {
            string f1 = GetTempPathName();
            const string contents = "abcdef";
            File.WriteAllText(f1, "123", Encoding.UTF8);

            using (TransactionScope scope1 = new TransactionScope())
            {
                _target.WriteAllText(f1, contents, Encoding.UTF8);
                scope1.Complete();
            }

            File.ReadAllText(f1).Should().Be(contents);
        }

        [Fact]
        public void CanWriteAllTextWithEncodingAndRollback()
        {
            string f1 = GetTempPathName();
            const string contents1 = "123";
            const string contents2 = "abcdef";
            File.WriteAllText(f1, contents1, Encoding.UTF8);

            using (TransactionScope scope1 = new TransactionScope())
            {
                _target.WriteAllText(f1, contents2, Encoding.UTF8);
            }

            File.ReadAllText(f1).Should().Be(contents1);
        }

        #endregion

        #region Transaction Support

        [Fact]
        public void ThrowExceptionIfCannotRollback()
        {
            // Run this test on Windows only
            // This test doesn't work on Ubuntu/Unix because setting file attribute to read-only does not 
            // prevent this code from deleting the file

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var f1 = GetTempPathName(".txt");
                var f2 = GetTempPathName(".txt");

                try
                {
                    Action act = () =>
                    {
                        using (var scope1 = new TransactionScope())
                        {
                            _target.WriteAllText(f1, "Test.");
                            _target.WriteAllText(f2, "Test.");

                            FileInfo fi1 = new FileInfo(f1);
                            fi1.Attributes = FileAttributes.ReadOnly;

                            // rollback
                        }
                    };

                    act.Should().Throw<TransactionException>().Where(e => e.Message.Contains("Failed to roll back."));
                }
                finally
                {
                    var fi1 = new FileInfo(f1);
                    if (fi1.Exists)
                    {
                        fi1.Attributes = FileAttributes.Normal;
                        File.Delete(f1);
                    }
                }
            }
        }

        [Fact]
        public void CanReuseManager()
        {
            {
                var f1 = GetTempPathName();
                File.WriteAllText(f1, "Hello.");
                var f2 = GetTempPathName();

                using (var scope1 = new TransactionScope())
                {
                    _target.Copy(f1, f2, false);

                    // rollback
                }
                File.Exists(f2).Should().BeFalse();
            }

            {
                var f1 = GetTempPathName();
                File.WriteAllText(f1, "Hello.");

                using (var scope1 = new TransactionScope())
                {
                    _target.Delete(f1);

                    // rollback
                }

                File.Exists(f1).Should().BeTrue();
            }
        }

        [Fact]
        public void CanSupportTransactionScopeOptionSuppress()
        {
            const string contents = "abc";
            var f1 = GetTempPathName(".txt");
            using (var scope1 = new TransactionScope(TransactionScopeOption.Suppress))
            {
                _target.WriteAllText(f1, contents);
            }

            // With TransactionScopeOption.Suppress - commit is implicit so our change should have been committed
            // without us doing a scope.Complete()
            File.ReadAllText(f1).Should().Be(contents);
        }

        [Fact]
        public void CanNestTransactions()
        {
            var f1 = GetTempPathName(".txt");
            const string f1Contents = "f1";
            var f2 = GetTempPathName(".txt");
            const string f2Contents = "f2";
            var f3 = GetTempPathName(".txt");
            const string f3Contents = "f3";

            using (var sc1 = new TransactionScope())
            {
                _target.WriteAllText(f1, f1Contents);

                using (var sc2 = new TransactionScope())
                {
                    _target.WriteAllText(f2, f2Contents);
                    sc2.Complete();
                }

                using (var sc3 = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    _target.WriteAllText(f3, f3Contents);
                    sc3.Complete();
                }

                sc1.Dispose();
            }
            File.Exists(f1).Should().BeFalse();
            File.Exists(f2).Should().BeFalse();
            File.Exists(f3).Should().BeTrue();
        }

        [Fact]
        public void CanDoMultiThreads()
        {
            // Start each test in its own thread and repeat for a few interations
            const int iterations = 25;
            IList<Thread> threads = new List<Thread>();
            BlockingCollection<Exception> exceptions = new BlockingCollection<Exception>();

            Action[] actions = new Action[] { CanAppendAllText, AppendAllTextCanRollback, CanCopy, CanCopyAndRollback,
                CanCreateDirectory, CanCreateDirectoryAndRollback, CanDeleteFile, CanDeleteFileAndRollback, CanMoveFile,
                CanMoveFileAndRollback, CanSnapshot, CanWriteAllText, CanWriteAllTextAndRollback, CanNestTransactions,
                ThrowException
            };
            for (int i = 0; i < iterations; i++)
            {
                foreach (Action action in actions)
                {
                    Thread t = new Thread(() => { Launch(action, exceptions); });
                    threads.Add(t);
                }
            }

            foreach (Thread t in threads)
            {
                t.Start();
                t.Join();
            }

            exceptions.Count.Should().Be(iterations);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private static void Launch(Action action, BlockingCollection<Exception> exceptions)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        #endregion

        #region Other

        [Fact]
        public void ItRemovesCompletedEnlistments()
        {
            string f1 = GetTempPathName();
            const string contents = "123";

            using (TransactionScope scope1 = new TransactionScope())
            {
                _target.AppendAllText(f1, contents);
                scope1.Complete();
            }

            TxFileManager.GetEnlistmentCount().Should().Be(0);
        }

        [Fact]
        public void CanSetCustomTempPath()
        {
            IFileManager fm = new TxFileManager();
            var myTempPath = "\\temp-f8417ba5";

            var d1 = fm.CreateTempDirectory();
            d1.Should().NotContain(myTempPath);

            var f1 = fm.CreateTempFileName();
            f1.Should().NotContain(myTempPath);

            IFileManager fm2 = new TxFileManager(myTempPath);
            var d2 = fm2.CreateTempDirectory();
            d2.Should().Contain(myTempPath);

            var f2 = fm2.CreateTempFileName();
            f2.Should().Contain(myTempPath);

            Directory.Delete(d1);
            Directory.Delete(d2);

            File.Delete(f1);
            File.Delete(f2);
        }


        [Fact]
        public async Task HandlesAsync()
        {
            var scheduler = new ThreadedPerTaskScheduler();
            async Task RunInNewThread(Func<Task> action)
            {
                await Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, scheduler);
            }
            var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            IFileManager fm = null;
            await RunInNewThread(() =>
            {
                fm = new TxFileManager();
                fm.WriteAllBytes("test", new byte[] { 1, 2, 3 });
                return Task.CompletedTask;
            });
            ts.Complete();
            ts.Dispose();
        }

        private class ThreadedPerTaskScheduler : TaskScheduler
        {
            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Enumerable.Empty<Task>();
            }

            protected override void QueueTask(Task task)
            {
                new Thread(() => TryExecuteTask(task)) { IsBackground = true }.Start();
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }
        }

        #endregion

        #region Private

        private string GetTempPathName(string extension = "")
        {
            string tempFile = _target.CreateTempFileName(extension);
            _tempPaths.Add(tempFile);
            return tempFile;
        }

        private void ThrowException()
        {
            throw new Exception("Test.");
        }

        #endregion

    }
}
