namespace ChinhDo.Transactions.FileManagerTest;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

public sealed class FileManagerTests
{
    private readonly TxFileManager target = new();
    private IList<string> tempPaths = [];
    #region Operations
    [Test]
    public void CanAppendAllText()
    {
        var f1 = GetTempPathName();
        const string contents = "123";
        using (var scope1 = new TransactionScope())
        {
            target.AppendAllText(f1, contents);
            scope1.Complete();
        }

        File.ReadAllText(f1).ShouldBe(contents);
    }

    [Test]
    public void AppendAllTexHandlesException()
    {
        var f1 = GetTempPathName();
        const string contents = "123";
        using var scope1 = new TransactionScope();
        using var fs = File.Open(f1, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
        Action act = () => target.AppendAllText(f1, contents);
        act.ShouldThrow<IOException>().Message.ShouldContain("The process cannot access the file");
    }

    [Test]
    public void AppendAllTextCanRollback()
    {
        var f1 = GetTempPathName();
        const string contents = "qwerty";
        using (var sc1 = new TransactionScope())
        {
            target.AppendAllText(f1, contents);
            // without specifically committing, this should roll back
        }

        File.Exists(f1).ShouldBeFalse($"{f1} should not exist");
    }

    [Test]
    public void CanCopy()
    {
        var sourceFileName = GetTempPathName();
        var destFileName = GetTempPathName();
        const string expectedText = "Test 123.";
        using (var scope1 = new TransactionScope())
        {
            File.WriteAllText(sourceFileName, expectedText);
            target.Copy(sourceFileName, destFileName, false);
            scope1.Complete();
        }

        File.ReadAllText(sourceFileName).ShouldBe(expectedText);
        File.ReadAllText(destFileName).ShouldBe(expectedText);
    }

    [Test]
    public void CanCopyAndRollback()
    {
        var sourceFileName = GetTempPathName();
        const string expectedText = "Hello 123.";
        File.WriteAllText(sourceFileName, expectedText);
        var destFileName = GetTempPathName();
        using (var scope1 = new TransactionScope())
        {
            target.Copy(sourceFileName, destFileName, false);
            // without specifically committing, this should roll back
        }

        File.Exists(destFileName).ShouldBeFalse($"{destFileName} should not exist");
    }

    [Test]
    public void CanHandleCopyErrors()
    {
        var f1 = GetTempPathName();
        var f2 = GetTempPathName();
        using (var fs = new FileStream(f2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            using (var scope1 = new TransactionScope())
            {
                target.WriteAllText(f1, "test");
                Action act = () => target.Copy(f1, f2, false);
                act.ShouldThrow<IOException>();
                //rollback
            }
        }

        File.Exists(f1).ShouldBeFalse();
    }

    [Test]
    public void CanCreateDirectory()
    {
        var d1 = GetTempPathName();
        using (var scope1 = new TransactionScope())
        {
            target.CreateDirectory(d1);
            scope1.Complete();
        }

        Directory.Exists(d1).ShouldBeTrue();
    }

    /// <summary>
    /// Validate that we are able to create nested directories and roll them back.
    /// </summary>
    [Test]
    public void CanRollbackNestedDirectories()
    {
        var baseDir = GetTempPathName();
        Directory.CreateDirectory(baseDir);
        var nested = Path.Combine(baseDir, "level1");
        nested = Path.Combine(nested, "level2");
        using (new TransactionScope())
        {
            target.CreateDirectory(nested);
            Directory.Exists(nested).ShouldBeTrue();
        }

        Directory.Exists(nested).ShouldBeFalse();
        Directory.Exists(baseDir).ShouldBeTrue();
        Directory.Delete(baseDir);
    }

    [Test]
    public void CanCreateDirectoryAndRollback()
    {
        var d1 = GetTempPathName();
        using (var scope1 = new TransactionScope())
        {
            target.CreateDirectory(d1);
        }

        Directory.Exists(d1).ShouldBeFalse();
    }

    [Test]
    public void CanDeleteDirectory()
    {
        var f1 = GetTempPathName();
        Directory.CreateDirectory(f1);
        using (var scope1 = new TransactionScope())
        {
            target.DeleteDirectory(f1);
            scope1.Complete();
        }

        Directory.Exists(f1).ShouldBeFalse();
    }

    [Test]
    public void CanDeleteDirectoryAndRollback()
    {
        var f1 = GetTempPathName();
        Directory.CreateDirectory(f1);
        using (var scope1 = new TransactionScope())
        {
            target.DeleteDirectory(f1);
        }

        Directory.Exists(f1).ShouldBeTrue();
    }

    [Test]
    public void CanDeleteFile()
    {
        var f1 = GetTempPathName();
        const string contents = "abc";
        File.WriteAllText(f1, contents);
        using (var scope1 = new TransactionScope())
        {
            target.Delete(f1);
            scope1.Complete();
        }

        File.Exists(f1).ShouldBeFalse();
    }

    [Test]
    public void CanDeleteFileAndRollback()
    {
        var f1 = GetTempPathName();
        const string contents = "abc";
        File.WriteAllText(f1, contents);
        using (var scope1 = new TransactionScope())
        {
            target.Delete(f1);
        }

        File.Exists(f1).ShouldBeTrue();
        File.ReadAllText(f1).ShouldBe(contents);
    }

    [Test]
    public void CanMoveFile()
    {
        const string contents = "abc";
        var f1 = GetTempPathName();
        var f2 = GetTempPathName();
        File.WriteAllText(f1, contents);
        using var scope1 = new TransactionScope();
        File.Exists(f1).ShouldBeTrue();
        File.Exists(f2).ShouldBeFalse();
        target.Move(f1, f2);
        scope1.Complete();
    }

    [Test]
    public void CanMoveFileAndRollback()
    {
        const string contents = "abc";
        var f1 = GetTempPathName();
        var f2 = GetTempPathName();
        File.WriteAllText(f1, contents);
        using (new TransactionScope())
        {
            File.Exists(f1).ShouldBeTrue();
            File.Exists(f2).ShouldBeFalse();
            target.Move(f1, f2);
        }

        File.ReadAllText(f1).ShouldBe(contents);
        File.Exists(f2).ShouldBeFalse();
    }

    [Test]
    public void CanMoveDirectory()
    {
        var f1 = GetTempPathName();
        var f2 = GetTempPathName();
        Directory.CreateDirectory(f1);
        using (var scope1 = new TransactionScope())
        {
            Directory.Exists(f1).ShouldBeTrue();
            Directory.Exists(f2).ShouldBeFalse();
            target.MoveDirectory(f1, f2);
            scope1.Complete();
        }

        Directory.Exists(f1).ShouldBeFalse();
        Directory.Exists(f2).ShouldBeTrue();
    }

    [Test]
    public void CanMoveDirectoryAndRollback()
    {
        var f1 = GetTempPathName();
        var f2 = GetTempPathName();
        Directory.CreateDirectory(f1);
        using (new TransactionScope())
        {
            Directory.Exists(f1).ShouldBeTrue();
            Directory.Exists(f2).ShouldBeFalse();
            target.MoveDirectory(f1, f2);
        }

        Directory.Exists(f1).ShouldBeTrue();
        Directory.Exists(f2).ShouldBeFalse();
    }

    [Test]
    public void CanSnapshot()
    {
        var f1 = GetTempPathName();
        using (var scope1 = new TransactionScope())
        {
            target.Snapshot(f1);
            target.AppendAllText(f1, "<test></test>");
        }

        File.Exists(f1).ShouldBeFalse();
    }

    [Test]
    public void CanWriteAllText()
    {
        var f1 = GetTempPathName();
        const string contents = "abcdef";
        File.WriteAllText(f1, "123");
        using (var scope1 = new TransactionScope())
        {
            target.WriteAllText(f1, contents);
            scope1.Complete();
        }

        File.ReadAllText(f1).ShouldBe(contents);
    }

    [Test]
    public void CanWriteAllTextAndRollback()
    {
        var f1 = GetTempPathName();
        const string contents1 = "123";
        const string contents2 = "abcdef";
        File.WriteAllText(f1, contents1);
        using (var scope1 = new TransactionScope())
        {
            target.WriteAllText(f1, contents2);
        }

        File.ReadAllText(f1).ShouldBe(contents1);
    }

    [Test]
    public void CanWriteAllTextWithEncoding()
    {
        string f1 = GetTempPathName();
        const string contents = "abcdef";
        File.WriteAllText(f1, "123", Encoding.UTF8);
        using (TransactionScope scope1 = new TransactionScope())
        {
            target.WriteAllText(f1, contents, Encoding.UTF8);
            scope1.Complete();
        }

        File.ReadAllText(f1).ShouldBe(contents);
    }

    [Test]
    public void CanWriteAllTextWithEncodingAndRollback()
    {
        string f1 = GetTempPathName();
        const string contents1 = "123";
        const string contents2 = "abcdef";
        File.WriteAllText(f1, contents1, Encoding.UTF8);
        using (TransactionScope scope1 = new TransactionScope())
        {
            target.WriteAllText(f1, contents2, Encoding.UTF8);
        }

        File.ReadAllText(f1).ShouldBe(contents1);
    }

    #endregion
    #region Transaction Support
    [Test]
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
                        target.WriteAllText(f1, "Test.");
                        target.WriteAllText(f2, "Test.");
                        FileInfo fi1 = new FileInfo(f1)
                        {
                            Attributes = FileAttributes.ReadOnly,
                        };
                        // rollback
                    }
                };
                act.ShouldThrow<TransactionException>().Message.ShouldContain("Failed to roll back.");
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

    [Test]
    public void CanReuseManager()
    {
        {
            var f1 = GetTempPathName();
            File.WriteAllText(f1, "Hello.");
            var f2 = GetTempPathName();
            using (var scope1 = new TransactionScope())
            {
                target.Copy(f1, f2, false);
                // rollback
            }

            File.Exists(f2).ShouldBeFalse();
        }

        {
            var f1 = GetTempPathName();
            File.WriteAllText(f1, "Hello.");
            using (var scope1 = new TransactionScope())
            {
                target.Delete(f1);
                // rollback
            }

            File.Exists(f1).ShouldBeTrue();
        }
    }

    [Test]
    public void CanSupportTransactionScopeOptionSuppress()
    {
        const string contents = "abc";
        var f1 = GetTempPathName(".txt");
        using (var scope1 = new TransactionScope(TransactionScopeOption.Suppress))
        {
            target.WriteAllText(f1, contents);
        }

        // With TransactionScopeOption.Suppress - commit is implicit so our change should have been committed
        // without us doing a scope.Complete()
        File.ReadAllText(f1).ShouldBe(contents);
    }

    [Test]
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
            target.WriteAllText(f1, f1Contents);
            using (var sc2 = new TransactionScope())
            {
                target.WriteAllText(f2, f2Contents);
                sc2.Complete();
            }

            using (var sc3 = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                target.WriteAllText(f3, f3Contents);
                sc3.Complete();
            }

            sc1.Dispose();
        }

        File.Exists(f1).ShouldBeFalse();
        File.Exists(f2).ShouldBeFalse();
        File.Exists(f3).ShouldBeTrue();
    }

    [Test]
    public void CanDoMultiThreads()
    {
        // Start each test in its own thread and repeat for a few iterations
        const int iterations = 25;
        IList<Thread> threads = [];
        var exceptions = new BlockingCollection<Exception>();
        Action[] actions = [CanAppendAllText, AppendAllTextCanRollback, CanCopy, CanCopyAndRollback, CanCreateDirectory, CanCreateDirectoryAndRollback, CanDeleteFile, CanDeleteFileAndRollback, CanMoveFile, CanMoveFileAndRollback, CanSnapshot, CanWriteAllText, CanWriteAllTextAndRollback, CanNestTransactions, ThrowException, ];
        for (int i = 0; i < iterations; i++)
        {
            foreach (Action action in actions)
            {
                Thread t = new Thread(() =>
                {
                    Launch(action, exceptions);
                });
                threads.Add(t);
            }
        }

        foreach (Thread t in threads)
        {
            t.Start();
            t.Join();
        }

        exceptions.Count.ShouldBe(iterations);
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
    [Test]
    public void ItRemovesCompletedEnlistments()
    {
        string f1 = GetTempPathName();
        const string contents = "123";
        using (TransactionScope scope1 = new TransactionScope())
        {
            target.AppendAllText(f1, contents);
            scope1.Complete();
        }

        TxFileManager.GetEnlistmentCount().ShouldBe(0);
    }

    [Test]
    public void CanSetCustomTempPath()
    {
        IFileManager fm = new TxFileManager();
        var myTempPath = "\\temp-f8417ba5";
        var d1 = fm.CreateTempDirectory();
        d1.ShouldNotContain(myTempPath);
        var f1 = fm.CreateTempFileName();
        f1.ShouldNotContain(myTempPath);
        IFileManager fm2 = new TxFileManager(myTempPath);
        var d2 = fm2.CreateTempDirectory();
        d2.ShouldContain(myTempPath);
        var f2 = fm2.CreateTempFileName();
        f2.ShouldContain(myTempPath);
        Directory.Delete(d1);
        Directory.Delete(d2);
        File.Delete(f1);
        File.Delete(f2);
    }

    [Test]
    public async Task HandlesAsync()
    {
        var scheduler = new ThreadedPerTaskScheduler();
        var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        TxFileManager fm = null;
        await RunInNewThread(() =>
        {
            fm = new TxFileManager();
            fm.WriteAllBytes("test", [1, 2, 3]);
            return Task.CompletedTask;
        });
        ts.Complete();
        ts.Dispose();
        return;
        async Task RunInNewThread(Func<Task> action)
        {
            await Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }
    }

    private class ThreadedPerTaskScheduler : System.Threading.Tasks.TaskScheduler
    {
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return[];
        }

        protected override void QueueTask(Task task)
        {
            new Thread(() => TryExecuteTask(task))
            {
                IsBackground = true
            }.Start();
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
        string tempFile = target.CreateTempFileName(extension);
        tempPaths.Add(tempFile);
        return tempFile;
    }

    private void ThrowException()
    {
        throw new Exception("Test.");
    }

    [After(Test)]
    // TODO delete any temp files
    public void Dispose()
    {
        // Delete temp files/dirs
        foreach (var item in tempPaths)
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
        numTempFiles.ShouldBe(0);
    }
    #endregion
}