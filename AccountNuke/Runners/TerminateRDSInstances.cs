using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.RDS.Model;
using Microsoft.Extensions.Logging;
using NLog;
using SharedLibrary;

namespace AccountNuke.Runners
{
    public class TerminateRDSInstances : Runner
    {
        public TerminateRDSInstances(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Terminate RDS Instances";
        }

        public override async Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);

            var logger = LogManager.GetCurrentClassLogger();

            Parallel.ForEach(SharedLibrary.Utilities.GetRegions(), (region) =>
            {

                logger.Debug($"Checking RDS instances in region {region.DisplayName }");
                var creds = SharedLibrary.Utilities.AssumeRole(RoleARN, region);

                Amazon.RDS.AmazonRDSClient client = new Amazon.RDS.AmazonRDSClient(creds, region);

                var describeResult = client.DescribeDBInstancesAsync(new DescribeDBInstancesRequest { MaxRecords = 100 }).Result;

                foreach (var instance in describeResult.DBInstances)
                {
                    if (instance.DBInstanceStatus != "deleting")
                    {
                        var deleteDBInstanceResult = client.DeleteDBInstanceAsync(new DeleteDBInstanceRequest { DBInstanceIdentifier = instance.DBInstanceIdentifier }).Result;

                        if (deleteDBInstanceResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                            logger.Debug($"Deleted RDS instance {instance.DBInstanceIdentifier} in region {region.DisplayName }");
                    }
                }

            });


        }
    }
}
