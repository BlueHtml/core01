using System.Net.Http;
using System.Threading.Tasks;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace DockerWeb.Helper
{
    public class TwilioHelper
    {
        static string fromPhoneNumber;
        static TwilioRestClient twilioRestClient;

        public static void Init(string accountSid, string authToken, string fromPhoneNumber, HttpClient httpClient = null)
        {
            TwilioHelper.fromPhoneNumber = fromPhoneNumber;

            twilioRestClient = new TwilioRestClient(
               accountSid,
               authToken,
               httpClient: new Twilio.Http.SystemNetHttpClient(httpClient)
           );
        }

        public static async Task<MessageResource> SendSms(string toPhoneNumber, string smsMsg)
        {
            return await MessageResource.CreateAsync(
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(fromPhoneNumber),
                body: smsMsg,
                // Here's where you inject the custom client
                client: twilioRestClient
            );
        }
    }
}
