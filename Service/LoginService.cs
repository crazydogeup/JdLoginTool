using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace JdLoginTool.Wpf.Service
{
    public static class LoginService
    {
        static RestClient client = new RestClient("http://139.196.126.180:5678/api/phone");
        private static string _phone;
        public static string GetNewPhoneNumber()
        {
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            _phone = response.Content;
            if (!string.IsNullOrEmpty(_phone))
            {
                Task.Factory.StartNew(() =>
                  {
                      var n = 60;
                      while (n > 0)
                      {
                          n--;
                          Thread.Sleep(1000);
                      }
                      _phone = "";
                  });
                return _phone;
            }
            return _phone;
        }
        public static string GetCaptcha()
        {

            return "";
        }

        public static void SendCookie(string cookie)
        {

        }
    }
}