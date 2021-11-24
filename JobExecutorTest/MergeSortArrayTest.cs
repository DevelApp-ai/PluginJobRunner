using JobExecutor;
using PluginInterface;
using System;
using Xunit;
using FluentAssertions;
using AutoFixture.Xunit2;

namespace JobExecutorTest
{
    public class MergeSortArrayTest
    {
        #region Structure
        [Fact]
        public void MergeSortArray_Start_True()
        {
            bool starts = false;
            try
            {
                MergeSortArray mergeSortArray = new MergeSortArray();
                starts = true;
            }
            catch
            {
            }
            starts.Should().BeTrue();
        }

        //TODO Non Json data

        #endregion

        #region JobData

        [Theory, AutoData]
        public void ExecuteJob_EmptyArray_True(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "[]";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeTrue();
            result.returnedJobData.Should().NotBeNullOrWhiteSpace().And.HaveLength(2).And.BeEquivalentTo(jobData);
            result.error.Should().BeNullOrWhiteSpace();
        }

        [Theory, AutoData]
        public void ExecuteJob_MissingJobData_False(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeFalse();
            result.returnedJobData.Should().BeNullOrWhiteSpace();
            result.error.Should().NotBeNullOrWhiteSpace();
        }

        [Theory, AutoData]
        public void ExecuteJob_ArrayContainingNotNumbers_False(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "[2,3,A,5]";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeFalse();
            result.returnedJobData.Should().BeNullOrWhiteSpace();
            result.error.Should().NotBeNullOrWhiteSpace();
        }

        [Theory, AutoData]
        public void ExecuteJob_JsonButNotArray_False(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = @"{""rubbish"": ""banana peel""}";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeFalse();
            result.returnedJobData.Should().BeNullOrWhiteSpace();
            result.error.Should().NotBeNullOrWhiteSpace();
        }

        [Theory, AutoData]
        public void ExecuteJob_ArrayOfOne_ArrayAsInput(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "[1]";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeTrue();
            result.returnedJobData.Should().NotBeNullOrWhiteSpace().And.HaveLength(3).And.BeEquivalentTo(jobData);
            result.error.Should().BeNullOrWhiteSpace();
        }

        [Theory, AutoData]
        public void ExecuteJob_ArrayOfAlreadySortedValues_ArrayAsInput(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "[1,2,3,4,5]";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeTrue();
            result.returnedJobData.Should().NotBeNullOrWhiteSpace().And.BeEquivalentTo(jobData);
            result.error.Should().BeNullOrWhiteSpace();
        }

        [Theory, AutoData]
        public void ExecuteJob_ArrayNormal_SortedArray(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "[5,2,1,4,3]";
            string returnedJobData = "[1,2,3,4,5]";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeTrue();
            result.returnedJobData.Should().NotBeNullOrWhiteSpace().And.BeEquivalentTo(returnedJobData);
            result.error.Should().BeNullOrWhiteSpace();
        }

        [Theory, AutoData]
        public void ExecuteJob_ArrayWithDuplicates_SortedArrayWithDublicates(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "[5,2,4,1,4,3,2]";
            string returnedJobData = "[1,2,2,3,4,4,5]";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeTrue();
            result.returnedJobData.Should().NotBeNullOrWhiteSpace().And.BeEquivalentTo(returnedJobData);
            result.error.Should().BeNullOrWhiteSpace();
        }


        [Theory, AutoData]
        public void ExecuteJob_ArrayLong_SortedArray(MergeSortArray mergeSortArray, JobExecutionContext jobExecutionContext)
        {
            string jobData = "[20,19,18,17,16,15,14,13,12,11,10,9,8,7,6,5,4,3,2,1]";
            string returnedJobData = "[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20]";

            (bool success, string returnedJobData, string error) result = mergeSortArray.ExecuteJob(jobExecutionContext, jobData);

            result.success.Should().BeTrue();
            result.returnedJobData.Should().NotBeNullOrWhiteSpace().And.BeEquivalentTo(returnedJobData);
            result.error.Should().BeNullOrWhiteSpace();
        }

        #endregion
    }
}
