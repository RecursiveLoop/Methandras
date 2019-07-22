using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using NLog;
using SharedLibrary;

namespace AccountNuke.Runners
{
    public class DeleteCloudFormation : Runner
    {
        public DeleteCloudFormation(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Delete CloudFormation stacks";
        }

        public override async Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);

            var logger = LogManager.GetCurrentClassLogger();
            Parallel.ForEach(SharedLibrary.Utilities.GetRegions(), (region) =>
            {
                logger.Debug($"Checking CloudFormation stacks in region {region.DisplayName }");
                var creds = SharedLibrary.Utilities.AssumeRole(RoleARN, region);
                var sessionCreds = new SessionAWSCredentials(creds.AccessKeyId, creds.SecretAccessKey, creds.SessionToken);

                Amazon.CloudFormation.AmazonCloudFormationClient client = new Amazon.CloudFormation.AmazonCloudFormationClient(creds, region);



                var listStackResult = client.ListStacksAsync(new ListStacksRequest
                {
                    StackStatusFilter = new List<string> {

                    "CREATE_COMPLETE",
                    "CREATE_IN_PROGRESS",
                    "CREATE_FAILED",
                    "REVIEW_IN_PROGRESS",
                    "ROLLBACK_COMPLETE",
                    "UPDATE_COMPLETE",
                    "UPDATE_ROLLBACK_COMPLETE",
                    "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS",
                    "UPDATE_ROLLBACK_IN_PROGRESS"
                }
                }).Result;

                foreach (var cfStack in listStackResult.StackSummaries)
                {
                    var deleteStackResult = client.DeleteStackAsync(new DeleteStackRequest { StackName = cfStack.StackName }).Result;

                    if (deleteStackResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        logger.Debug($"Successfully deleted stack {cfStack.StackName} in {region.DisplayName}");
                    }

                }
            });
        }
    }
}
