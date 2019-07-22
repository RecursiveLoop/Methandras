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

namespace Methandras
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

                    foreach (var accountId in SharedLibrary.Utilities.GetChildAccountIds(ParentOU))
                    {

                        string RoleARN = $"arn:aws:iam::{accountId}:role/{AssumeRoleName}";
                        logger.Debug($"Trying to assume role {RoleARN}");

                        var lstRunners = GetRunnersToExecute(servicesProvider);

                        List<Task> lstTasks = new List<Task>();

                        foreach (var runner in lstRunners)
                        {
                            var result = runner.DoAction(RoleARN);
                            lstTasks.Add(result);
                        }



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

        static List<Runner> GetRunnersToExecute(IServiceProvider servicesProvider)
        {
            List<Runner> lst = new List<Runner>();


            lst.Add(servicesProvider.GetRequiredService<CreateIAMUsers>());

            return lst;
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
