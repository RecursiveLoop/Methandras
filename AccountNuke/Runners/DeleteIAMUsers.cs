using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using NLog;
using SharedLibrary;

namespace AccountNuke.Runners
{
    public class DeleteIAMUsers : Runner
    {
        public DeleteIAMUsers(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Delete IAM Users";
        }

        public override async Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);

            var logger = LogManager.GetCurrentClassLogger();

            var creds = SharedLibrary.Utilities.AssumeRole(RoleARN, RegionEndpoint.USEast1);
            var sessionCreds = new SessionAWSCredentials(creds.AccessKeyId, creds.SecretAccessKey, creds.SessionToken);

            Amazon.IdentityManagement.AmazonIdentityManagementServiceClient client = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient(sessionCreds);

            string Marker = null;

            do
            {
                var listUsersResults = await client.ListUsersAsync(new ListUsersRequest { Marker = Marker });

                foreach (var user in listUsersResults.Users)
                {
                    try
                    {
                        var getLoginProfileResult = client.GetLoginProfileAsync(new GetLoginProfileRequest { UserName = user.UserName }).Result;

                        if (getLoginProfileResult.LoginProfile != null)
                        {


                            var deleteLoginProfileResult = client.DeleteLoginProfileAsync(new DeleteLoginProfileRequest { UserName = user.UserName }).Result;

                            logger.Debug($"Deleted login profile for user {user.UserName}");

                        }
                    }
                    catch (Exception)
                    { }

                    var userPoliciesResult = client.ListAttachedUserPoliciesAsync(new ListAttachedUserPoliciesRequest { UserName = user.UserName }).Result;

                    foreach (var policy in userPoliciesResult.AttachedPolicies)
                    {
                        var detachPolicyResult = client.DetachUserPolicyAsync(new DetachUserPolicyRequest { PolicyArn = policy.PolicyArn, UserName = user.UserName }).Result;

                        if (detachPolicyResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                            logger.Debug($"Successfully detached user policy {policy.PolicyName} from user {user.UserName}");


                    }

                    var listUserPoliciesResult = client.ListUserPoliciesAsync(new ListUserPoliciesRequest { UserName = user.UserName }).Result;

                    foreach (var policy in listUserPoliciesResult.PolicyNames)
                    {
                        var deleteUserPolicyResult = client.DeleteUserPolicyAsync(new DeleteUserPolicyRequest { PolicyName = policy, UserName = user.UserName }).Result;

                        if (deleteUserPolicyResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                            logger.Debug($"Successfully deleted user policy {policy} from user {user.UserName}");

                    }

                    var listAccessKeysResult = client.ListAccessKeysAsync(new ListAccessKeysRequest { UserName = user.UserName }).Result;

                    foreach (var accessKey in listAccessKeysResult.AccessKeyMetadata)
                    {
                        var deleteAccessKeyResult = client.DeleteAccessKeyAsync(new DeleteAccessKeyRequest { AccessKeyId = accessKey.AccessKeyId, UserName = user.UserName }).Result;

                        if (deleteAccessKeyResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                            logger.Debug($"Deleted access key {accessKey.AccessKeyId} for user {user.UserName}");
                    }

                    var deleteUserResult = client.DeleteUserAsync(new DeleteUserRequest { UserName = user.UserName }).Result;

                    if (deleteUserResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                        logger.Debug($"Deleted user {user.UserName}");
                }
            } while (Marker != null);

        }
    }
}
