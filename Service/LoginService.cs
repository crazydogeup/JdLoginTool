using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Serialization.Json;

namespace JdLoginTool.Wpf.Service
{
    public static class LoginService
    {
        static RestClient client_phone = new RestClient("http://139.196.126.180:5678/api/phone");
        static RestClient client_captcha = new RestClient("http://139.196.126.180:5678/api/captcha");
        static RestClient client_upload_cookie = new RestClient("http://139.196.126.180:5678/api/cookie");
        private static string _phone;
        private static string _captcha;
        public static string GetPhone()
        {
            var request = new RestRequest(Method.GET);
            var response = client_phone.Execute(request);
            _phone = response.Content;
            if (!string.IsNullOrEmpty(_phone))
            {
                Task.Factory.StartNew(() =>
                  {
                      var n = 60;
                      while (n > 0&& !string.IsNullOrEmpty(_phone))
                      {
                          n--;
                          Trace.WriteLine("已经获取到电话号码,有效期还有:"+n);
                          Thread.Sleep(1000);
                      }
                      _phone = "";
                  });
                Trace.WriteLine("已经获取到电话号码:" + _phone);
                return _phone;
            }
            return _phone;
        }
        public static string GetCaptcha()
        { 
            Trace.WriteLine("开始获取验证码:");
            var request = new RestRequest(Method.GET);
            var response = client_captcha.Execute(request);
            _captcha = response.Content;
            if (!string.IsNullOrEmpty(_captcha))
            {
                Task.Factory.StartNew(() =>
                {
                    var n = 60;
                    while (n > 0&& !string.IsNullOrEmpty(_captcha))
                    {
                        n--;
                        Trace.WriteLine("已经获取到验证码,有效期还有:" + n);
                        Thread.Sleep(1000);
                    }
                    _captcha = "";
                });
                Trace.WriteLine("已经获取到验证码:" + _captcha);
                return _captcha;
            }
            return _captcha;
        }

        public static void SendCookie(string cookie)
        {
            Trace.WriteLine("发送cookie:");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            var Cookie = new Cookie();
            Cookie.cookie = cookie;
            request.AddParameter("application/json", $"{{\"cookie\": \"{cookie}\"}}", ParameterType.RequestBody);
            var response = client_upload_cookie.Execute(request);
            Trace.WriteLine("发送cookie结果:"+ response.StatusCode);
        }

        public class Cookie
        {
            public string cookie { get; set; } 
        }
    }
}