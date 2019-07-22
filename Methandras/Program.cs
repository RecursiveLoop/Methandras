using AccountAllocator.Runners;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using NLog.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharedLibrary;
using static AccountAllocator.Runners.CreateIAMUsers;
using System.IO;
using CsvHelper;

namespace Methandras
{
    class Program
    {
        static string ParentOU = "ou-6nx6-93xwjm3g";
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

                    using (var writer = new StreamWriter(DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv"))
                    using (var csv = new CsvWriter(writer))
                    {
                        List<UserType> lstUsers = new List<UserType>();

                        Parallel.ForEach(SharedLibrary.Utilities.GetChildAccountIds(ParentOU), (accountId) =>
                       {

                           string RoleARN = $"arn:aws:iam::{accountId}:role/{AssumeRoleName}";
                           logger.Debug($"Trying to assume role {RoleARN}");
                           var result = new CreateIAMUsers(logger).DoAction(RoleARN).Result;
                           result.AccountId = accountId;
                           lstUsers.Add(result);
                           logger.Debug($"Created new user {result.Username} in account {accountId}. ");

                       });

                        csv.WriteRecords(lstUsers);
                        csv.Flush();
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
               .AddTransient<CreateIAMUsers>()
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
