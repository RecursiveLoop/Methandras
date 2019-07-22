using Microsoft.Extensions.Logging;
using SharedLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AccountAllocator.Runners
{
    public class CreateIAMUsers : Runner
    {
        public CreateIAMUsers(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Create IAM Users";
        }

        public override async Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);
        }
    }
}
