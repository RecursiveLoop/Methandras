using Amazon.EC2.Model;
using Amazon.SecurityToken.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Amazon;

namespace AccountNuke
{
    public class Utils
    {

        public static Credentials AssumeRole(string RoleARN)
        {
            Amazon.SecurityToken.AmazonSecurityTokenServiceClient stsClient = new Amazon.SecurityToken.AmazonSecurityTokenServiceClient();

            var assumeResult = stsClient.AssumeRoleAsync(new Amazon.SecurityToken.Model.AssumeRoleRequest { RoleArn = RoleARN, RoleSessionName = "mySession" });

            return assumeResult.Result.Credentials;
        }

        public static RegionEndpoint[] GetRegions()
        {
            return Amazon.RegionEndpoint.EnumerableAllRegions.ToList().Where(a => a.SystemName!="").ToArray();
        }
    }
}
