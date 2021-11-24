using DevelApp.Utility.Model;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobExecutor
{
    public class MergeSortArray : IJobExecutor
    {
        public IdentifierString Name
        {
            get
            {
                return "Merge";
            }
        }

        public NamespaceString Module
        {
            get
            {
                return "SortArray";
            }
        }

        public string Description
        {
            get
            {
                return "Makes a sorting of a json array supplied in jobData";
            }
        }

        public SemanticVersionNumber Version
        { 
            get
            {
                return "0.0.1";
            }
        }

        public (bool success, string returnedJobData, string error) ExecuteJob(IJobExecutionContext jobExecutionContext, string jobData)
        {
            if (string.IsNullOrWhiteSpace(jobData))
            {
                return (success: false, returnedJobData: null, error: "Missing jobData");
            }
            if (!jobData.StartsWith("["))
            {
                return (success: false, returnedJobData: null, error: "Input is not an json array");
            }
            if (jobData.Equals("[]"))
            {
                return (success: true, returnedJobData: "[]", error: null);
            }

            string values = jobData.Substring(1, jobData.Length - 2);
            string[] arrayOfValues = values.Split(',');
            List<int> valuesOfInt = new List<int>();

            foreach(string value in arrayOfValues)
            {
                if(int.TryParse(value, out int valueAsInt))
                {
                    valuesOfInt.Add(valueAsInt);
                }
                else
                {
                    return (success: false, returnedJobData: null, error: "Input is not an json array of integers");
                }
            }

            if(valuesOfInt.Count == 1)
            { 
                return (success: true, returnedJobData: jobData , error: null);
            }

            try
            {
                int[] arrayOfInt = valuesOfInt.ToArray();
                Sort(ref arrayOfInt, 0, arrayOfInt.Length - 1);

                string returnedJobData = MakeArrayToJsonString(arrayOfInt);

                return (success: true, returnedJobData: returnedJobData, error: null);
            }
            catch (Exception ex)
            {
                return (success: false, returnedJobData: null, error: $"Sorting algorithm failed: {ex.Message}");
            }
        }

        private string MakeArrayToJsonString(int[] arrayOfInt)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            stringBuilder.AppendJoin(",", arrayOfInt);
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }



        #region Merge Sort (Devide and conquer startegy)
        // Article from https://www.geeksforgeeks.org/merge-sort/
        // variables are translated from scientific mathematical notation to programming notation meaning only counter valiables are allowed to remain a single letter
        // changed the input array to reference as default is by value

        // Merges two subarrays of []inputArray.
        // First subarray is inputArray[leftIndex..middlePoint]
        // Second subarray is inputArray[middlePoint+1..rightIndex]
        private void Merge(ref int[] inputArray, int leftIndex, int middlePoint, int rightIndex)
        {
            // Find sizes of two
            // subarrays to be merged
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

            // Initial indexes of first
            // and second subarrays
            i = 0;
            j = 0;

            // Initial index of merged
            // subarray array
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

            // Copy remaining elements
            // of leftArray[] if any
            while (i < subArrayLength1)
            {
                inputArray[k] = leftArray[i];
                i++;
                k++;
            }

            // Copy remaining elements
            // of rightArray[] if any
            while (j < subArrayLength2)
            {
                inputArray[k] = rightArray[j];
                j++;
                k++;
            }
        }

        // Main function that
        // sorts inputArray[l..r] using
        // merge()
        private void Sort(ref int[] inputArray, int leftIndex, int rightIndex)
        {
            if (leftIndex < rightIndex)
            {
                // Find the middle
                // point
                int middelPoint = leftIndex + (rightIndex - leftIndex) / 2;

                // Sort first and
                // second halves
                Sort(ref inputArray, leftIndex, middelPoint);
                Sort(ref inputArray, middelPoint + 1, rightIndex);

                // Merge the sorted halves
                Merge(ref inputArray, leftIndex, middelPoint, rightIndex);
            }
        }

        #endregion
    }
}
