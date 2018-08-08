using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DockerWeb.Helper;
using Hangfire.Common;
using Hangfire.RecurringJobExtensions;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace JobCS
{
    public class WeatherSMS : IRecurringJob
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherSMS(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public void Execute(PerformContext context)
        {
            var cityId = context.GetJobData<string>("CityId");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://aider.meizu.com");
            Task<string> task = client.GetStringAsync($"/app/weather/listWeather?cityIds={cityId}");
            task.Wait();
            string result = task.Result;
            JObject jObj = JObject.Parse(result);
            if (jObj.Value<string>("code") != "200")
            {
                throw new Exception("返回的状态不是200！");
            }

            var timeZone = context.GetJobData<string>("TimeZone");
            DateTime nowTime_CN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZone));

            string dayInfo = "今天";
            if (nowTime_CN.Hour > 12)
            {
                dayInfo = "明天";
                nowTime_CN = nowTime_CN.AddDays(1d);
            }

            JArray jarrCityWeather = jObj.Value<JArray>("value");
            foreach (JToken jtCityWeather in jarrCityWeather)
            {
                var cityName = jtCityWeather.Value<string>("city");

                //最近几日天气情况
                JArray jarrWeathers = jtCityWeather.Value<JArray>("weathers");
                foreach (var jtDayWeather in jarrWeathers)
                {
                    DateTime tempDate = jtDayWeather.Value<DateTime>("date");
                    if (tempDate.Date == nowTime_CN.Date)
                    {
                        //白天温度，最高温度，摄氏度
                        string dayTemp = jtDayWeather.Value<string>("temp_day_c");
                        //夜晚温度，最低温度，摄氏度
                        string nightTemp = jtDayWeather.Value<string>("temp_night_c");
                        //wd：风向
                        string wd = jtDayWeather.Value<string>("wd");
                        wd = string.IsNullOrWhiteSpace(wd) ? "" : $"，风向：{wd}";
                        //ws：风力大小
                        string ws = jtDayWeather.Value<string>("ws");
                        ws = string.IsNullOrWhiteSpace(ws) ? "" : $"，风力：{ws}";
                        //weather：天气情况
                        string weather = jtDayWeather.Value<string>("weather");

                        string msg = $"{cityName}，{dayInfo}天气：{weather} ，温度：{nightTemp}~{dayTemp}℃{wd}{ws}。";
                        var toPhoneNumber = context.GetJobData<string>("ToPhoneNumber");
                        TwilioHelper.SendSms(toPhoneNumber, msg).Wait();
                        break;
                    }
                }
            }
        }
    }
}
