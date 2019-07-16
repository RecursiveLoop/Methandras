using Amazon.EC2.Model;
using Amazon.SecurityToken.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Amazon;
using Amazon.IdentityManagement;

namespace AccountNuke
{
    public class Utils
    {

        public static Credentials AssumeRole(string RoleARN,RegionEndpoint region)
        {
            Amazon.SecurityToken.AmazonSecurityTokenServiceClient stsClient = new Amazon.SecurityToken.AmazonSecurityTokenServiceClient( region);

            var assumeResult = stsClient.AssumeRoleAsync(new Amazon.SecurityToken.Model.AssumeRoleRequest { RoleArn = RoleARN ,RoleSessionName=Guid.NewGuid().ToString()});

            var creds = assumeResult.Result.Credentials;

            return creds;
        }

        public static void SetSecurityTokenServicePrefs(Credentials creds)
        {
            Amazon.IdentityManagement.AmazonIdentityManagementServiceClient client = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient(creds);

            var setResult = client.SetSecurityTokenServicePreferencesAsync(new Amazon.IdentityManagement.Model.SetSecurityTokenServicePreferencesRequest { GlobalEndpointTokenVersion = GlobalEndpointTokenVersion.V2Token }).Result;

            if (setResult.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception();

        }

        public static RegionEndpoint[] GetRegions()
        {
            return Amazon.RegionEndpoint.EnumerableAllRegions.ToList().Where(a => !a.SystemName.StartsWith("cn")
            && !a.SystemName.Contains("gov") && a.SystemName!="ap-east-1" && a.SystemName != "ap-northeast-3").ToArray();
        }

        public static string[] GetChildAccountIds(string ParentId)
        {
            Amazon.Organizations.AmazonOrganizationsClient client = new Amazon.Organizations.AmazonOrganizationsClient(RegionEndpoint.USEast1);

            string NextToken = null;

            List<string> lstAccountIds = new List<string>();

            do
            {
                var listAccountResult = client.ListAccountsForParentAsync(new Amazon.Organizations.Model.ListAccountsForParentRequest { ParentId = ParentId, NextToken = NextToken }).Result;

                NextToken = listAccountResult.NextToken;

                lstAccountIds.AddRange(listAccountResult.Accounts.Select(a => a.Id));

            } while (NextToken != null);

            return lstAccountIds.ToArray();
        }
    }
}
