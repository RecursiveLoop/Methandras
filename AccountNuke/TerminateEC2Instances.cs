using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AccountNuke
{
    public class TerminateEC2Instances : Runner
    {
        public TerminateEC2Instances(ILogger<Runner> logger):base(logger)
        {

        }

      
        public async override Task DoAction(string RoleARN)
        {
            var creds = Utils.AssumeRole(RoleARN);
            foreach (var region in Utils.GetRegions())
            {


                Amazon.EC2.AmazonEC2Client client = new Amazon.EC2.AmazonEC2Client(creds, region);

                var describeInstancesResult = await client.DescribeInstancesAsync();

                string nextToken;

                do
                {
                    nextToken = describeInstancesResult.NextToken;

                    var instances = describeInstancesResult.Reservations.SelectMany(r => r.Instances).ToList();

                    var terminateResult = await client.TerminateInstancesAsync(new Amazon.EC2.Model.TerminateInstancesRequest { InstanceIds = instances.Select(a => a.InstanceId).ToList() });



                } while (nextToken != "");
            }
        }
    }
}
