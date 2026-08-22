using JobExecutor;
using JobExecutorModel;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace JobRunner.Test
{
    /// <summary>
    /// Load tests for JobRunner API
    /// Tests performance and scalability under load
    /// </summary>
    public class LoadTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public LoadTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        // ============================================================================
        // Performance Tests
        // ============================================================================

        [Fact]
        public async Task MergeSortArray_ExecuteJob_PerformanceTest()
        {
            // Arrange
            var mergeSort = new MergeSortArray();
            var stopwatch = Stopwatch.StartNew();

            // Act - Execute multiple times
            for (int i = 0; i < 100; i++)
            {
                var result = mergeSort.ExecuteJob(null, "[3,1,2]");
                Assert.True(result.success);
            }

            stopwatch.Stop();

            // Assert - Should complete 100 executions in under 1 second
            Assert.True(stopwatch.Elapsed.TotalSeconds < 1.0, 
                $"100 executions took {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        [Fact]
        public async Task MergeSortArray_ExecuteJob_LargeArray_PerformanceTest()
        {
            // Arrange
            var mergeSort = new MergeSortArray();
            var largeArray = GenerateLargeArray(1000);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = mergeSort.ExecuteJob(null, largeArray);

            stopwatch.Stop();

            // Assert
            Assert.True(result.success);
            Assert.True(stopwatch.Elapsed.TotalSeconds < 0.5, 
                $"Sorting 1000 elements took {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        [Fact]
        public async Task MergeSortArrayGeneric_ExecuteAsync_PerformanceTest()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();
            var stopwatch = Stopwatch.StartNew();

            // Act - Execute multiple times
            for (int i = 0; i < 100; i++)
            {
                var input = new MergeSortInput
                {
                    Values = new int[] { 3, 1, 2 }
                };
                var result = await mergeSort.ExecuteAsync(null, input);
                Assert.True(result.Success);
            }

            stopwatch.Stop();

            // Assert - Should complete 100 executions in under 1 second
            Assert.True(stopwatch.Elapsed.TotalSeconds < 1.0, 
                $"100 async executions took {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        // ============================================================================
        // Concurrent Execution Tests
        // ============================================================================

        [Fact]
        public async Task MergeSortArray_ConcurrentExecution_Test()
        {
            // Arrange
            var mergeSort = new MergeSortArray();
            var tasks = new List<Task<(bool, string?, string?)>>();

            // Act - Execute concurrently
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => mergeSort.ExecuteJob(null, "[3,1,2]")));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All should succeed
            foreach (var result in results)
            {
                Assert.True(result.Item1);
                Assert.NotNull(result.Item2);
                Assert.Null(result.Item3);
            }
        }

        [Fact]
        public async Task MergeSortArrayGeneric_ConcurrentExecution_Test()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();
            var tasks = new List<Task<JobExecutionResult<MergeSortOutput>>>();

            // Act - Execute concurrently
            for (int i = 0; i < 10; i++)
            {
                var input = new MergeSortInput
                {
                    Values = new int[] { 3, 1, 2 }
                };
                tasks.Add(mergeSort.ExecuteAsync(null, input));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All should succeed
            foreach (var result in results)
            {
                Assert.True(result.Success);
                Assert.NotNull(result.Data);
            }
        }

        // ============================================================================
        // Stress Tests
        // ============================================================================

        [Fact]
        public async Task MergeSortArray_StressTest()
        {
            // Arrange
            var mergeSort = new MergeSortArray();
            var stopwatch = Stopwatch.StartNew();
            var successCount = 0;
            var errorCount = 0;

            // Act - Execute 1000 times
            for (int i = 0; i < 1000; i++)
            {
                var array = GenerateRandomArray(100);
                var result = mergeSort.ExecuteJob(null, array);
                
                if (result.success)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Equal(1000, successCount);
            Assert.Equal(0, errorCount);
            Assert.True(stopwatch.Elapsed.TotalSeconds < 5.0, 
                $"1000 executions took {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        [Fact]
        public async Task MergeSortArrayGeneric_StressTest()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();
            var stopwatch = Stopwatch.StartNew();
            var successCount = 0;
            var errorCount = 0;

            // Act - Execute 1000 times
            for (int i = 0; i < 1000; i++)
            {
                var input = new MergeSortInput
                {
                    Values = GenerateRandomArray(100)
                };
                var result = await mergeSort.ExecuteAsync(null, input);
                
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Equal(1000, successCount);
            Assert.Equal(0, errorCount);
            Assert.True(stopwatch.Elapsed.TotalSeconds < 5.0, 
                $"1000 async executions took {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        // ============================================================================
        // Memory Tests
        // ============================================================================

        [Fact]
        public void MergeSortArray_MemoryTest()
        {
            // Arrange
            var mergeSort = new MergeSortArray();
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Execute many times
            for (int i = 0; i < 1000; i++)
            {
                var array = GenerateRandomArray(1000);
                var result = mergeSort.ExecuteJob(null, array);
                Assert.True(result.success);
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            // Assert - Memory usage should be reasonable
            Assert.True(memoryUsed < 100 * 1024 * 1024, // Less than 100MB
                $"Memory usage: {memoryUsed / (1024.0 * 1024.0):F2}MB");
        }

        // ============================================================================
        // Helper Methods
        // ============================================================================

        private string GenerateLargeArray(int size)
        {
            var random = new Random();
            var array = new int[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = random.Next();
            }
            return JsonSerializer.Serialize(array);
        }

        private int[] GenerateRandomArray(int size)
        {
            var random = new Random();
            var array = new int[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = random.Next();
            }
            return array;
        }
    }
}
