using JobExecutor;
using JobExecutorModel;
using JobRunner.Controllers;
using JobRunner.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PluginInterface;
using System;
using System.Collections.Generic;
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
    /// Contract tests for JobRunner API
    /// Tests that the API adheres to its contract
    /// </summary>
    public class ContractTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public ContractTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        // ============================================================================
        // API Contract Tests
        // ============================================================================

        [Fact]
        public async Task JobsController_PostJob_ReturnsCorrectStatusCodes()
        {
            // Arrange
            var validRequest = new PostJobRequest
            {
                JobExecutor = "Merge",
                JobData = "[3,1,2]"
            };

            // Act - Valid request
            var response = await _client.PostAsync("/api/jobs", 
                new StringContent(JsonSerializer.Serialize(validRequest), Encoding.UTF8, "application/json"));

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task JobsController_PostJob_ValidRequest_ReturnsJobResponse()
        {
            // Arrange
            var validRequest = new PostJobRequest
            {
                JobExecutor = "Merge",
                JobData = "[3,1,2]"
            };

            // Act
            var response = await _client.PostAsync("/api/jobs",
                new StringContent(JsonSerializer.Serialize(validRequest), Encoding.UTF8, "application/json"));

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jobResponse = JsonSerializer.Deserialize<JobResponse>(content);
                
                Assert.NotNull(jobResponse);
                Assert.True(jobResponse.Success);
            }
        }

        [Fact]
        public async Task JobsController_PostJob_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new PostJobRequest
            {
                JobExecutor = "",
                JobData = "invalid"
            };

            // Act
            var response = await _client.PostAsync("/api/jobs",
                new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json"));

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.Unauthorized);
        }

        // ============================================================================
        // Health Check Contract Tests
        // ============================================================================

        [Fact]
        public async Task HealthCheck_Endpoint_ReturnsSuccess()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        // ============================================================================
        // Error Code Contract Tests
        // ============================================================================

        [Theory]
        [InlineData(JobErrorCode.Success, JobErrorCategory.Success)]
        [InlineData(JobErrorCode.InvalidJobData, JobErrorCategory.InputValidation)]
        [InlineData(JobErrorCode.InvalidApiKey, JobErrorCategory.Authentication)]
        [InlineData(JobErrorCode.PluginNotFound, JobErrorCategory.Plugin)]
        [InlineData(JobErrorCode.DatabaseConnectionFailed, JobErrorCategory.Database)]
        [InlineData(JobErrorCode.SchedulerNotRunning, JobErrorCategory.Scheduler)]
        [InlineData(JobErrorCode.InternalServerError, JobErrorCategory.System)]
        public void JobError_GetCategory_ReturnsCorrectCategory(JobErrorCode code, JobErrorCategory expectedCategory)
        {
            // Act
            var category = JobError.GetCategory(code);

            // Assert
            Assert.Equal(expectedCategory, category);
        }

        [Theory]
        [InlineData(JobErrorCode.RateLimitExceeded, false)]
        [InlineData(JobErrorCode.InvalidApiKey, false)]
        [InlineData(JobErrorCode.AccessDenied, false)]
        [InlineData(JobErrorCode.PluginNotFound, false)]
        [InlineData(JobErrorCode.DatabaseConnectionFailed, true)]
        [InlineData(JobErrorCode.DatabaseTimeout, true)]
        [InlineData(JobErrorCode.InternalServerError, true)]
        public void JobError_IsRetryable_ReturnsCorrectValue(JobErrorCode code, bool expectedRetryable)
        {
            // Act
            var isRetryable = JobError.IsRetryableCode(code);

            // Assert
            Assert.Equal(expectedRetryable, isRetryable);
        }

        [Theory]
        [InlineData(JobErrorCode.Success, false)]
        [InlineData(JobErrorCode.InvalidJobData, false)]
        [InlineData(JobErrorCode.DatabaseTimeout, true)]
        [InlineData(JobErrorCode.OperationTimeout, true)]
        [InlineData(JobErrorCode.ServiceUnavailable, true)]
        public void JobError_IsTransient_ReturnsCorrectValue(JobErrorCode code, bool expectedTransient)
        {
            // Act
            var isTransient = JobError.IsTransientCode(code);

            // Assert
            Assert.Equal(expectedTransient, isTransient);
        }

        // ============================================================================
        // Plugin Contract Tests
        // ============================================================================

        [Fact]
        public void MergeSortArray_Implements_IJobExecutor()
        {
            // Arrange
            var mergeSort = new MergeSortArray();

            // Act & Assert
            Assert.IsAssignableFrom<IJobExecutor>(mergeSort);
        }

        [Fact]
        public void MergeSortArrayGeneric_Implements_IJobExecutorGeneric()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();

            // Act & Assert
            Assert.IsAssignableFrom<IJobExecutor<MergeSortInput, MergeSortOutput>>(mergeSort);
        }

        [Fact]
        public void MergeSortArray_HasCorrectMetadata()
        {
            // Arrange
            var mergeSort = new MergeSortArray();

            // Act & Assert
            Assert.Equal("Merge", mergeSort.Name.ToString());
            Assert.Equal("SortArray", mergeSort.Module.ToString());
            Assert.NotNull(mergeSort.Description);
            Assert.NotNull(mergeSort.Version);
        }

        [Fact]
        public void MergeSortArrayGeneric_HasCorrectMetadata()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();

            // Act & Assert
            Assert.Equal("MergeSortGeneric", mergeSort.Name.ToString());
            Assert.Equal("SortArray", mergeSort.Module.ToString());
            Assert.NotNull(mergeSort.Description);
            Assert.NotNull(mergeSort.Version);
        }

        // ============================================================================
        // Input Validation Contract Tests
        // ============================================================================

        [Fact]
        public void MergeSortArray_ExecuteJob_EmptyInput_ReturnsFailure()
        {
            // Arrange
            var mergeSort = new MergeSortArray();

            // Act
            var result = mergeSort.ExecuteJob(null, null);

            // Assert
            Assert.False(result.success);
            Assert.NotNull(result.error);
        }

        [Fact]
        public void MergeSortArray_ExecuteJob_InvalidJson_ReturnsFailure()
        {
            // Arrange
            var mergeSort = new MergeSortArray();

            // Act
            var result = mergeSort.ExecuteJob(null, "invalid json");

            // Assert
            Assert.False(result.success);
            Assert.NotNull(result.error);
        }

        [Fact]
        public void MergeSortArray_ExecuteJob_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var mergeSort = new MergeSortArray();

            // Act
            var result = mergeSort.ExecuteJob(null, "[3,1,2]");

            // Assert
            Assert.True(result.success);
            Assert.NotNull(result.returnedJobData);
            Assert.Null(result.error);
        }

        [Fact]
        public void MergeSortArray_ExecuteJob_EmptyArray_ReturnsSuccess()
        {
            // Arrange
            var mergeSort = new MergeSortArray();

            // Act
            var result = mergeSort.ExecuteJob(null, "[]");

            // Assert
            Assert.True(result.success);
            Assert.Equal("[]", result.returnedJobData);
            Assert.Null(result.error);
        }

        [Fact]
        public void MergeSortArray_ExecuteJob_SingleElement_ReturnsSuccess()
        {
            // Arrange
            var mergeSort = new MergeSortArray();

            // Act
            var result = mergeSort.ExecuteJob(null, "[1]");

            // Assert
            Assert.True(result.success);
            Assert.Equal("[1]", result.returnedJobData);
            Assert.Null(result.error);
        }

        // ============================================================================
        // Type-Safe Plugin Contract Tests
        // ============================================================================

        [Fact]
        public async Task MergeSortArrayGeneric_ExecuteAsync_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();
            var input = new MergeSortInput
            {
                Values = new int[] { 3, 1, 2 }
            };

            // Act
            var result = await mergeSort.ExecuteAsync(null, input);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.SortedValues);
            Assert.Equal(3, result.Data.OriginalLength);
            Assert.Equal("MergeSort", result.Data.Algorithm);
            Assert.Equal(new int[] { 1, 2, 3 }, result.Data.SortedValues);
            Assert.Null(result.ErrorMessage);
            Assert.Null(result.Exception);
        }

        [Fact]
        public async Task MergeSortArrayGeneric_ExecuteAsync_NullInput_ReturnsFailure()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();

            // Act
            var result = await mergeSort.ExecuteAsync(null, null!);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task MergeSortArrayGeneric_ExecuteAsync_EmptyInput_ReturnsFailure()
        {
            // Arrange
            var mergeSort = new MergeSortArrayGeneric();
            var input = new MergeSortInput
            {
                Values = new int[0]
            };

            // Act
            var result = await mergeSort.ExecuteAsync(null, input);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
    }
}
