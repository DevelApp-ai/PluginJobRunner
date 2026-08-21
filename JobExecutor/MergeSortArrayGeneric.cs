using DevelApp.Utility.Model;
using PluginInterface;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JobExecutor
{
    /// <summary>
    /// Type-safe implementation of merge sort job executor
    /// Implements IJobExecutor<TInput, TOutput> for strongly-typed job execution
    /// </summary>
    public class MergeSortArrayGeneric : IJobExecutor<MergeSortInput, MergeSortOutput>
    {
        public IdentifierString Name
        {
            get
            {
                return "MergeSortGeneric";
            }
        }

        public NamespaceString Module
        {
            get
            {
                return "SortArray";
            }
        }

        public string? Description
        {
            get
            {
                return "Type-safe sorting of an integer array using merge sort algorithm";
            }
        }

        public SemanticVersionNumber Version
        {
            get
            {
                return "1.0.0";
            }
        }

        /// <summary>
        /// Executes the merge sort job with type-safe input and output
        /// </summary>
        /// <param name="context">Execution context</param>
        /// <param name="input">Input data containing array to sort</param>
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

                // Make a copy to avoid modifying the original
                var arrayCopy = new int[input.Values.Length];
                Array.Copy(input.Values, arrayCopy, input.Values.Length);

                // Sort the array
                Sort(ref arrayCopy, 0, arrayCopy.Length - 1);

                // Create output
                var output = new MergeSortOutput
                {
                    SortedValues = arrayCopy,
                    OriginalLength = input.Values.Length,
                    Algorithm = "MergeSort"
                };

                return JobExecutionResult<MergeSortOutput>.CreateSuccess(
                    output,
                    stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                context?.Logger?.LogError(ex, "Merge sort failed");
                return JobExecutionResult<MergeSortOutput>.CreateFailure(
                    $"Sorting algorithm failed: {ex.Message}",
                    ex,
                    stopwatch.Elapsed);
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
