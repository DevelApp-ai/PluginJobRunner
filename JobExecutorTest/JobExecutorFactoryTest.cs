using JobRunner.JobExecutionFactory;
using System;
using System.IO;
using Xunit;
using FluentAssertions;

namespace JobExecutorTest
{
    public class JobExecutorFactoryTest
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ValidPluginPath_ShouldInitialize()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                // Act
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Assert
                factory.Should().NotBeNull();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void Constructor_InvalidPath_ShouldThrowException()
        {
            // Arrange
            var invalidPath = new Uri(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            // Act & Assert
            Action act = () => new JobExecutorFactory(invalidPath);
            act.Should().Throw<Exception>()
                .WithMessage("*Error occured when loading JobExecutors*");
        }

        #endregion

        #region GetJobExecutor Tests

        [Fact]
        public void GetJobExecutor_ValidNameFormat_ShouldReturnNull_WhenPluginNotFound()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Act
                var result = factory.GetJobExecutor("SomeNamespace.SomePlugin");

                // Assert
                result.Should().BeNull();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void GetJobExecutor_InvalidNameFormat_ShouldReturnNull()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Act
                var result = factory.GetJobExecutor("InvalidFormat");

                // Assert
                result.Should().BeNull();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void GetJobExecutor_EmptyString_ShouldReturnNull()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Act
                var result = factory.GetJobExecutor(string.Empty);

                // Assert
                result.Should().BeNull();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void GetJobExecutor_NullString_ShouldReturnNull()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Act
                var result = factory.GetJobExecutor(null);

                // Assert
                result.Should().BeNull();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldAllowMultipleCalls()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                var factory = new JobExecutorFactory(pluginPathUri);

                // Act & Assert - should not throw
                factory.Dispose();
                factory.Dispose(); // Second call should be safe
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void GetJobExecutor_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                var factory = new JobExecutorFactory(pluginPathUri);
                factory.Dispose();

                // Act & Assert
                Action act = () => factory.GetJobExecutor("Test.Plugin");
                act.Should().Throw<ObjectDisposedException>()
                    .WithMessage("*JobExecutorFactory*");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void Dispose_UsingStatement_ShouldDisposeCorrectly()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            JobExecutorFactory factory = null;

            try
            {
                // Act
                using (factory = new JobExecutorFactory(pluginPathUri))
                {
                    factory.Should().NotBeNull();
                }

                // Assert - after using block, factory should be disposed
                Action act = () => factory.GetJobExecutor("Test.Plugin");
                act.Should().Throw<ObjectDisposedException>();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void GetJobExecutor_WithException_ShouldNotCrashApplication()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Act - This should handle errors gracefully
                var result = factory.GetJobExecutor("Invalid..Name..Format");

                // Assert - Should return null, not crash
                result.Should().BeNull();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        #endregion

        #region Name Parsing Tests

        [Theory]
        [InlineData("Namespace.ClassName")]
        [InlineData("Very.Long.Namespace.Path.ClassName")]
        [InlineData("Simple.Class")]
        public void GetJobExecutor_ValidNameFormats_ShouldParseCorrectly(string fullName)
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Act - Should not throw, even if plugin not found
                var result = factory.GetJobExecutor(fullName);

                // Assert
                result.Should().BeNull(); // Plugin doesn't exist, but parsing should work
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Theory]
        [InlineData("NoNamespace")]
        [InlineData(".StartsWithDot")]
        [InlineData("EndsWithDot.")]
        [InlineData("")]
        public void GetJobExecutor_InvalidNameFormats_ShouldReturnNull(string fullName)
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            var pluginPathUri = new Uri(tempPath);

            try
            {
                using var factory = new JobExecutorFactory(pluginPathUri);

                // Act
                var result = factory.GetJobExecutor(fullName);

                // Assert
                result.Should().BeNull();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        #endregion
    }
}
