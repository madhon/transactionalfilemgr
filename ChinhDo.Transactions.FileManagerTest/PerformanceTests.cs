namespace ChinhDo.Transactions.FileManagerTest;

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Transactions;

/// <summary>
/// Performance tests to demonstrate optimizations in the TxFileManager
/// </summary>
public sealed class PerformanceTests
{
    private readonly TxFileManager _txFileManager;
    private readonly string _tempDirectory;
    public PerformanceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "TxFileManagerPerfTests", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDirectory);
        _txFileManager = new TxFileManager(_tempDirectory);
    }

    [Test]
    public void MeasureLargeFileCopyPerformance()
    {
        // Create a large test file (1MB)
        const int fileSize = 1024 * 1024;
        byte[] testData = new byte[fileSize];
        RandomNumberGenerator.Fill(testData);
        string sourceFile = Path.Combine(_tempDirectory, "source.dat");
        string destFile = Path.Combine(_tempDirectory, "dest.dat");
        File.WriteAllBytes(sourceFile, testData);
        // Measure copy performance
        var stopwatch = Stopwatch.StartNew();
        _txFileManager.Copy(sourceFile, destFile, false);
        stopwatch.Stop();
        // Verify file was copied correctly
        var copiedData = File.ReadAllBytes(destFile);
        copiedData.Length.ShouldBe(testData.Length);
        // Performance should be reasonable (under 1 second for 1MB)
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000);
    }

    [Test]
    public void MeasureTransactionalOperationsPerformance()
    {
        const int operationCount = 100;
        var files = new string[operationCount];
        // Prepare test files
        for (int i = 0; i < operationCount; i++)
        {
            files[i] = Path.Combine(_tempDirectory, $"file_{i:D4}.txt");
        }

        // Measure transactional write performance
        var stopwatch = Stopwatch.StartNew();
        using (var scope = new TransactionScope())
        {
            for (int i = 0; i < operationCount; i++)
            {
                _txFileManager.WriteAllText(files[i], $"Test content {i}");
            }

            scope.Complete();
        }

        stopwatch.Stop();
        // Verify all files were created
        for (int i = 0; i < operationCount; i++)
        {
            File.Exists(files[i]).ShouldBeTrue();
        }

        // Performance should be reasonable (under 5 seconds for 100 operations)
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000);
    }

    [Test]
    public void MeasureEnlistmentCleanupPerformance()
    {
        const int transactionCount = 10;
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < transactionCount; i++)
        {
            string testFile = Path.Combine(_tempDirectory, $"cleanup_test_{i}.txt");
            using (var scope = new TransactionScope())
            {
                _txFileManager.WriteAllText(testFile, "test content");
                scope.Complete();
            }
        }

        stopwatch.Stop();
        // Verify cleanup worked properly
        TxFileManager.GetEnlistmentCount.ShouldBe(0);
        // Performance should be reasonable
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(2000);
    }

    [After(Test)]
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}