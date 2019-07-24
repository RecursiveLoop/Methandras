using Amazon;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using NLog;
using SharedLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AccountAllocator.Runners
{
    public class CreateIAMUsers
    {

        static string Username = "aws_user";
        NLog.ILogger logger = null;

        public CreateIAMUsers(NLog.ILogger logger)
        {
            this.logger = logger;
        }

        public class UserType
        {
            public string AccountId { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string AccessKeyId { get; set; }

            public string SecretAccessKey { get; set; }
        }

        public async Task<UserType> DoAction(string RoleARN)
        {
            try
            {

                var creds = SharedLibrary.Utilities.AssumeRole(RoleARN, RegionEndpoint.USEast1);
                var sessionCreds = new SessionAWSCredentials(creds.AccessKeyId, creds.SecretAccessKey, creds.SessionToken);

                Amazon.IdentityManagement.AmazonIdentityManagementServiceClient client = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient(sessionCreds);

                GetUserResponse getUserResult;
                bool userFound = false;
                try
                {
                    getUserResult = await client.GetUserAsync(new GetUserRequest { UserName = Username });
                    userFound = getUserResult.User != null;
                }
                catch { }

                var newPassword = Utilities.RandomString(8);

                if (userFound)
                {
                    try
                    {
                        var getLoginProfileResult = await client.GetLoginProfileAsync(new GetLoginProfileRequest { UserName = Username });

                        if (getLoginProfileResult.LoginProfile != null)
                        {
                            var deleteLoginProfileResult = await client.DeleteLoginProfileAsync(new DeleteLoginProfileRequest { UserName = Username });

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Debug(ex.Message);
                    }
                    var listAccessKeysResult = client.ListAccessKeysAsync(new ListAccessKeysRequest { UserName = Username }).Result;

                    foreach (var accessKey in listAccessKeysResult.AccessKeyMetadata)
                    {
                        var deleteAccessKeyResult = client.DeleteAccessKeyAsync(new DeleteAccessKeyRequest { AccessKeyId = accessKey.AccessKeyId, UserName = Username }).Result;

                        if (deleteAccessKeyResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                            logger.Debug($"Deleted access key {accessKey.AccessKeyId} for user {Username}");
                    }
                }
                else
                {

                    var createUserResult = await client.CreateUserAsync(new CreateUserRequest { UserName = Username });


                }

                var attachPolicyResult = await client.AttachUserPolicyAsync(new AttachUserPolicyRequest { PolicyArn = "arn:aws:iam::aws:policy/AdministratorAccess", UserName = Username });


                var createLoginProfileResult = await client.CreateLoginProfileAsync(new CreateLoginProfileRequest { Password = newPassword, UserName = Username, PasswordResetRequired = true });

                var createAccessKeyResult = await client.CreateAccessKeyAsync(new CreateAccessKeyRequest { UserName = Username });



                UserType uType = new UserType
                {
                   
                    Username = Username,
                    Password = newPassword,
                    AccessKeyId = createAccessKeyResult.AccessKey.AccessKeyId,
                    SecretAccessKey = createAccessKeyResult.AccessKey.SecretAccessKey
                };

                return uType;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }
        }
    }
}
