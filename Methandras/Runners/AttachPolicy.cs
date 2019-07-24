using Amazon;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AccountAllocator.Runners
{
    public class AttachPolicy
    {
        static string Username = "aws_user";
        NLog.ILogger logger = null;

        public AttachPolicy(NLog.ILogger logger)
        {
            this.logger = logger;
        }

        public async Task DoAction(string RoleARN)
        {
            try
            {
                var creds = SharedLibrary.Utilities.AssumeRole(RoleARN, RegionEndpoint.USEast1);
                var sessionCreds = new SessionAWSCredentials(creds.AccessKeyId, creds.SecretAccessKey, creds.SessionToken);

                Amazon.IdentityManagement.AmazonIdentityManagementServiceClient client = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient(sessionCreds);

                var attachPolicyResult = await client.AttachUserPolicyAsync(new AttachUserPolicyRequest { PolicyArn = "arn:aws:iam::aws:policy/AdministratorAccess", UserName = Username });

                logger.Debug("Attached policy to " + RoleARN);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
    }
}
