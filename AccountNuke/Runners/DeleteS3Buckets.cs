using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using NLog;
using System.Linq;
using Amazon;
using SharedLibrary;

namespace AccountNuke.Runners
{
    public class DeleteS3Buckets : Runner
    {
        public DeleteS3Buckets(ILogger<Runner> logger) : base(logger)
        {
            this._action = "Delete S3 Buckets";
        }

        async Task EmptyBucket(Amazon.S3.AmazonS3Client client, string BucketName)
        {
            int itemCount = 0;
            var logger = LogManager.GetCurrentClassLogger();

            do
            {
                var listObjectsResult = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = BucketName });

                itemCount = listObjectsResult.KeyCount;

                if (itemCount > 0)
                {

                    var deleteObjectsResult = await client.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = BucketName, Objects = listObjectsResult.S3Objects.Select(a => new KeyVersion { Key = a.Key, VersionId = null }).ToList() });

                    if (deleteObjectsResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
                        logger.Debug($"Successfully deleted {deleteObjectsResult.DeletedObjects.Count} objects from bucket {BucketName}.");
                }

            } while (itemCount > 0);

        }

        public override async Task DoAction(string RoleARN)
        {
            await base.DoAction(RoleARN);

            var logger = LogManager.GetCurrentClassLogger();


            Parallel.ForEach(SharedLibrary.Utilities.GetRegions(), (region) =>
            {
                logger.Debug($"Checking S3 buckets in region {region.DisplayName }");

                var creds = SharedLibrary.Utilities.AssumeRole(RoleARN, region);

                Amazon.S3.AmazonS3Client client = new Amazon.S3.AmazonS3Client(creds, region);

                var listBucketsResult = client.ListBucketsAsync(new ListBucketsRequest { }).Result;

                foreach (var bucket in listBucketsResult.Buckets)
                {
                    try
                    {
                        var bucketLocationResult = client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = bucket.BucketName }).Result;

                        var bucketRegion = RegionEndpoint.GetBySystemName(bucketLocationResult.Location.Value);

                        Amazon.S3.AmazonS3Client localClient = new Amazon.S3.AmazonS3Client(creds, bucketRegion);

                        EmptyBucket(localClient, bucket.BucketName).Wait();

                        var deleteBucketResult = localClient.DeleteBucketAsync(new DeleteBucketRequest { BucketName = bucket.BucketName, BucketRegion = bucketLocationResult.Location }).Result;


                        logger.Debug($"Bucket {bucket.BucketName} in region {region.DisplayName} deleted.");
                    }
                    catch 
                    { }
                }
            });
        }
    }
}
