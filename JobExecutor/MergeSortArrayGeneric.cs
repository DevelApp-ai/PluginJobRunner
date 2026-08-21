using DevelApp.Utility.Model;
using PluginInterface;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace JobExecutor
{
    /// <summary>
    /// Type-safe implementation of merge sort array job executor
    /// Implements IJobExecutor<TInput, TOutput> for better type safety
    /// </summary>
    public class MergeSortArrayGeneric : IJobExecutor<MergeSortInput, MergeSortOutput>
    {
        /// <summary>
        /// Plugin name
        /// </summary>
        public IdentifierString Name => new IdentifierString("MergeSortGeneric");

        /// <summary>
        /// Plugin module
        /// </summary>
        public NamespaceString Module => new NamespaceString("SortArray");

        /// <summary>
        /// Plugin description
        /// </summary>
        public string? Description => "Sorts an array of integers using merge sort algorithm with type-safe input/output";

        /// <summary>
        /// Plugin version
        /// </summary>
        public SemanticVersionNumber Version => SemanticVersionNumber.Parse("1.0.0");

        /// <summary>
        /// Executes the merge sort job with type-safe input and output
        /// </summary>
        /// <param name="context">Execution context</param>
        /// <param name="input">Merge sort input containing array to sort</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job execution result with sorted array</returns>
        public async Task<JobExecutionResult<MergeSortOutput>> ExecuteAsync(
            IJobExecutionContext? context,
            MergeSortInput input,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate input
                if (input?.Values == null || input.Values.Length == 0)
                {
                    return JobExecutionResult<MergeSortOutput>.CreateFailure(
                        "Input array is null or empty",
                        null,
                        stopwatch.Elapsed);
                }

                // Make a copy of the array to sort (avoid modifying input)
                int[] arrayToSort = new int[input.Values.Length];
                Array.Copy(input.Values, arrayToSort, input.Values.Length);

                // Sort the array
                await Task.Run(() => Sort(ref arrayToSort, 0, arrayToSort.Length - 1), cancellationToken);

                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Create output
                var output = new MergeSortOutput
                {
                    SortedValues = arrayToSort,
                    OriginalLength = input.Values.Length,
                    Algorithm = "MergeSort"
                };

                return JobExecutionResult<MergeSortOutput>.CreateSuccess(output, stopwatch.Elapsed);
            }
            catch (OperationCanceledException)
            {
                return JobExecutionResult<MergeSortOutput>.CreateFailure(
                    "Execution was cancelled",
                    null,
                    stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return JobExecutionResult<MergeSortOutput>.CreateFailure(
                    $"Sorting algorithm failed: {ex.Message}",
                    ex,
                    stopwatch.Elapsed);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        #region Merge Sort Algorithm

        // Merges two subarrays of []inputArray.
        // First subarray is inputArray[leftIndex..middlePoint]
        // Second subarray is inputArray[middlePoint+1..rightIndex]
        private void Merge(ref int[] inputArray, int leftIndex, int middlePoint, int rightIndex)
        {
            // Find sizes of two subarrays to be merged
            int subArrayLength1 = middlePoint - leftIndex + 1;
            int subArrayLength2 = rightIndex - middlePoint;

            // Create temp arrays
            int[] leftArray = new int[subArrayLength1];
            int[] rightArray = new int[subArrayLength2];
            int i, j;

            // Copy data to temp arrays
            for (i = 0; i < subArrayLength1; ++i)
            {
                leftArray[i] = inputArray[leftIndex + i];
            }
            for (j = 0; j < subArrayLength2; ++j)
            {
                rightArray[j] = inputArray[middlePoint + 1 + j];
            }

            // Merge the temp arrays
            i = 0;
            j = 0;
            int k = leftIndex;

            while (i < subArrayLength1 && j < subArrayLength2)
            {
                if (leftArray[i] <= rightArray[j])
                {
                    inputArray[k] = leftArray[i];
                    i++;
                }
                else
                {
                    inputArray[k] = rightArray[j];
                    j++;
                }
                k++;
            }

            // Copy remaining elements of leftArray[] if any
            while (i < subArrayLength1)
            {
                inputArray[k] = leftArray[i];
                i++;
                k++;
            }

            // Copy remaining elements of rightArray[] if any
            while (j < subArrayLength2)
            {
                inputArray[k] = rightArray[j];
                j++;
                k++;
            }
        }

        // Main function that sorts inputArray[l..r] using merge()
        private void Sort(ref int[] inputArray, int leftIndex, int rightIndex)
        {
            if (leftIndex < rightIndex)
            {
                // Find the middle point
                int middlePoint = leftIndex + (rightIndex - leftIndex) / 2;

                // Sort first and second halves
                Sort(ref inputArray, leftIndex, middlePoint);
                Sort(ref inputArray, middlePoint + 1, rightIndex);

                // Merge the sorted halves
                Merge(ref inputArray, leftIndex, middlePoint, rightIndex);
            }
        }

        #endregion
    }
}
