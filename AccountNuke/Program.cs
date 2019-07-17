using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using NLog.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using AccountNuke.Runners;

namespace AccountNuke
{
    class Program
    {
        static string ParentOU = "ou-6nx6-ce943jmp";
        static string AssumeRoleName = "OrganizationAccountAccessRole";
        static void Main(string[] args) 
        {

            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                var config = new ConfigurationBuilder()
                   .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .Build();

                var servicesProvider = BuildDi(config);
                using (servicesProvider as IDisposable)
                {
                    
                    foreach (var accountId in Utils.GetChildAccountIds(ParentOU))
                    {
                        
                        string RoleARN = $"arn:aws:iam::{accountId}:role/{AssumeRoleName}";
                        logger.Debug($"Trying to assume role {RoleARN}");
                       
                        List<Task> lstTasks = new List<Task>();
                        Runner runner = servicesProvider.GetRequiredService<TerminateEC2Instances>();
                        var result = runner.DoAction(RoleARN);
                        lstTasks.Add(result);

                        runner = servicesProvider.GetRequiredService<DeleteCloudFormation>();
                        result = runner.DoAction(RoleARN);
                        lstTasks.Add(result);

                        runner = servicesProvider.GetRequiredService<DeleteIAMUsers>();
                        result = runner.DoAction(RoleARN);
                        lstTasks.Add(result);

                        runner = servicesProvider.GetRequiredService<TerminateRDSInstances>();
                        result = runner.DoAction(RoleARN);
                        lstTasks.Add(result);

                        runner = servicesProvider.GetRequiredService<DeleteS3Buckets>();
                        result = runner.DoAction(RoleARN);
                        lstTasks.Add(result);

                        Task.WaitAll(lstTasks.ToArray());
                    }

                    Console.WriteLine("Press ANY key to exit");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                // NLog: catch any exception and log it.
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }

        private static IServiceProvider BuildDi(IConfiguration config)
        {
            return new ServiceCollection()
               .AddTransient<TerminateEC2Instances>() // Runner is the custom class
               .AddTransient<TerminateRDSInstances>()
                .AddTransient<DeleteS3Buckets>()
                .AddTransient<DeleteIAMUsers>()
                  .AddTransient<DeleteCloudFormation>()
               .AddLogging(loggingBuilder =>
               {
                   // configure Logging with NLog
                   loggingBuilder.ClearProviders();
                   loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                   loggingBuilder.AddNLog(config);  
               })
               .BuildServiceProvider();
        }
    }
}
