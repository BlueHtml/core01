using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DockerWeb.Helper;
using Hangfire.Common;
using Hangfire.RecurringJobExtensions;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace JobCS
{
    public class WeatherSMS : IRecurringJob
    {
        public void Execute(PerformContext context)
        {
            var toPhoneNumber = context.GetJobData<string>("ToPhoneNumber");
            var msg = $"Jobs Run，Time：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}";

            TwilioHelper.SendSms(toPhoneNumber, msg).Wait();
        }
    }
}
