using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog;
using SharedLibrary;

namespace AccountNuke.Runners
{
    public class DeleteECSClusters : Runner
    {
        public DeleteECSClusters(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Delete ECS Clusters";
        }

        public override async Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);


            var logger = LogManager.GetCurrentClassLogger();


            Parallel.ForEach(Utils.GetRegions(), (region) =>
            {
                logger.Debug($"Checking ECS clusters in region {region.DisplayName }");

                var creds = Utils.AssumeRole(RoleARN, region);

                Amazon.ECS.AmazonECSClient client = new Amazon.ECS.AmazonECSClient(creds, region);

                string NextToken = null;

                do
                {

                    var listResult = client.ListClustersAsync(new Amazon.ECS.Model.ListClustersRequest { NextToken = NextToken }).Result;

                    NextToken = listResult.NextToken;

                    foreach (string clusterArn in listResult.ClusterArns)
                    {
                        logger.Debug($"Found ECS cluster {clusterArn} in region {region.DisplayName}");

                        var listContainerInstancesResult = client.ListContainerInstancesAsync(new Amazon.ECS.Model.ListContainerInstancesRequest { Cluster = clusterArn }).Result;

                        foreach (var containerInstanceArn in listContainerInstancesResult.ContainerInstanceArns )
                        {
                            var deregisterContainerInstanceResult = client.DeregisterContainerInstanceAsync(new Amazon.ECS.Model.DeregisterContainerInstanceRequest { Cluster = clusterArn, ContainerInstance = containerInstanceArn, Force = true }).Result;

                            logger.Debug($"Deregistered ECS container instance {containerInstanceArn}");


                        }

                        var deleteClusterResult = client.DeleteClusterAsync(new Amazon.ECS.Model.DeleteClusterRequest { Cluster = clusterArn }).Result;

                        logger.Debug($"Deleted ECS cluster {clusterArn}");

                    }


                } while (NextToken != null);


            });
        }
    }
}
