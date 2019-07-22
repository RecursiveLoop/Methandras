using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Amazon.EC2.Model;
using Amazon.Runtime;
using NLog;
using SharedLibrary;

namespace AccountNuke.Runners
{
    public class TerminateEC2Instances : Runner
    {
        public TerminateEC2Instances(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Terminate EC2 Instances";
        }


        public async override Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);
            var logger = LogManager.GetCurrentClassLogger();
            Parallel.ForEach(Utils.GetRegions(), (region) =>
           {
               logger.Debug($"Checking EC2 instances in region {region.DisplayName }");
               var creds = Utils.AssumeRole(RoleARN, region);
               var sessionCreds = new SessionAWSCredentials(creds.AccessKeyId, creds.SecretAccessKey, creds.SessionToken);

               Amazon.EC2.AmazonEC2Client client = new Amazon.EC2.AmazonEC2Client(sessionCreds, region);

               string nextToken = null;


               do
               {
                   var describeInstancesResult = client.DescribeInstancesAsync(new DescribeInstancesRequest { Filters = new List<Filter> { new Filter("instance-state-name", new List<string> { "running", "pending", "stopped", "stopping" }) }, NextToken = nextToken }).Result;

                   nextToken = describeInstancesResult.NextToken;

                   var instances = describeInstancesResult.Reservations.SelectMany(r => r.Instances).ToList();

                   if (instances.Count > 0)
                   {
                       logger.Debug($"Terminating {instances.Count} EC2 instance(s).");
                       var terminateResult = client.TerminateInstancesAsync(new Amazon.EC2.Model.TerminateInstancesRequest { InstanceIds = instances.Select(a => a.InstanceId).ToList() }).Result;

                       if (terminateResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                       {
                           logger.Debug($"Successfully terminated {terminateResult.TerminatingInstances.Count} EC2 instance(s).");
                       }
                   }

               } while (nextToken != null);
           });
        }
    }
}
