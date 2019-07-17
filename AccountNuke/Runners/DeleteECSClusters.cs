using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AccountNuke.Runners
{
    public class DeleteECSClusters : Runner
    {
        public DeleteECSClusters(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Delete ECS Clusters";
        }
    }
}
