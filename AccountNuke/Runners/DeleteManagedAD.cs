using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.DirectoryService.Model;
using Microsoft.Extensions.Logging;
using NLog;
using SharedLibrary;

namespace AccountNuke.Runners
{
    public class DeleteManagedAD : Runner
    {
        public DeleteManagedAD(ILogger<Runner> logger) : base(logger)
        {
        }

        public override async Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);

            var logger = LogManager.GetCurrentClassLogger();


            Parallel.ForEach(Utils.GetRegions(), (region) =>
            {
                logger.Debug($"Checking Managed Directories in region {region.DisplayName }");

                var creds = Utils.AssumeRole(RoleARN, region);

                var client = new Amazon.DirectoryService.AmazonDirectoryServiceClient(creds, region);

                try
                {
                    var describeResult = client.DescribeDirectoriesAsync(new DescribeDirectoriesRequest { }).Result;

                    foreach (var directory in describeResult.DirectoryDescriptions)
                    {
                        try
                        {
                            var deleteResult = client.DeleteDirectoryAsync(new DeleteDirectoryRequest { DirectoryId = directory.DirectoryId }).Result;

                            logger.Debug($"Successfully deleted directory {directory.DirectoryId} in {region.DisplayName}");
                        }
                        catch (Exception ex2)
                        {
                            logger.Error($"An error occurred deleting directory {directory.DirectoryId} in {region.DisplayName }: {ex2.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {

                    logger.Error($"An error occurred listing directories: {ex.Message}");
                }

            });
        }
    }
}
