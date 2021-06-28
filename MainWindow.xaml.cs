using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using JdLoginTool.Wpf.Service;

namespace JdLoginTool.Wpf
{
    public partial class MainWindow : Window
    {
        public bool Running = true;
        public MainWindow()
        {
            InitializeComponent();
            Task.Factory.StartNew(WaitForNewPhoneNumber);
            this.Closing += (o, e) => { Running = false; };
        }
        async void WaitForNewPhoneNumber()
        {

            this.Browser.Dispatcher.Invoke(new Action(() =>
            {
                if (!Browser.Address.Contains("https://m.jd.com/"))
                {
                    Browser.Address = "https://m.jd.com/";
                }
            }));
            GoToLoginPage();
            Running = true;
            while (Running)
            {   //进入登陆页面后,循环请求服务器中最新的手机号码
                var phone = LoginService.GetNewPhoneNumber();
                if (string.IsNullOrEmpty(phone))
                {
                    Thread.Sleep(1000);
                    continue;
                }
                //填写手机号码
                SetPhone(phone);
                Thread.Sleep(1000);
                this.Browser.Dispatcher.Invoke(new Action(() =>
                {
                    this.Focus();
                }));
                var r=  await  ClickGetCaptchaButton();
              Console.WriteLine(r);
                while (Running)
                {//开始循环向服务器请求用户的最新验证码
                    var captcha = LoginService.GetCaptcha();
                    if (string.IsNullOrEmpty(captcha))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    //填写验证码
                    SetCaptcha(captcha);
                    //点击登录
                    var loginResult = await ClickLoginButton();
                    if (loginResult)
                    {//获取cookie
                        var cookieStr = "";
                        this.Browser.Dispatcher.Invoke(new Action(() =>
                        {
                            var cm = Browser.WebBrowser.GetCookieManager();
                            var visitor = new TaskCookieVisitor();
                            cm.VisitAllCookies(visitor);
                            cookieStr = visitor.Task.Result
                                .Where(cookie => cookie.Name == "pt_key" || cookie.Name == "pt_pin")
                                .Aggregate(cookieStr, (current, cookie) => current + $"{cookie.Name}={cookie.Value};");
                            if (cookieStr.Contains("pt_key") && cookieStr.Contains("pt_pin"))
                            {
                                Clipboard.SetText(cookieStr);
                                LoginService.SendCookie(cookieStr);
                                Cef.GetGlobalCookieManager().DeleteCookies("", "");


                            }
                        }));

                    }
                }
            }

        }





        private void ButtonTest_OnClick(object sender, RoutedEventArgs e)
        {
            SetPhone("13250812637");
        }

        async void SetPhone(string phone)
        {
            try
            {
                var result = await Browser.EvaluateScriptAsPromiseAsync($"var xresult = document.evaluate(`//*[@id='app']/div/div[3]/p[1]/input`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value={phone};p.dispatchEvent(new Event('input'));");
                Console.WriteLine(result.Result);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        void SetCaptcha(string captcha)
        {
            try
            {
                Browser.EvaluateScriptAsPromiseAsync($"var xresult = document.evaluate(`//*[@id=\"authcode\"]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value=\"{captcha}\";");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        void GoToLoginPage()
        {
            try
            {
                Thread.Sleep(5000);
                this.Browser.Dispatcher.Invoke(new Action(() =>
                {
                    while (!Browser.Title.Contains("多快好省，购物上京东"))
                    {
                        var title = Browser.Title;
                        Console.WriteLine(title);
                        Thread.Sleep(1000);
                    }
                    Browser.EvaluateScriptAsPromiseAsync($"var b = document.getElementById('msShortcutLogin');b.click()");

                }));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        async Task<bool> ClickGetCaptchaButton()
        {
            try
            {
              var result=  await Browser.EvaluateScriptAsPromiseAsync("document.querySelector('#app div button').click()");
             
              return result.Success;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
        async Task<bool> ClickLoginButton()
        {
            try
            {
                await Browser.EvaluateScriptAsPromiseAsync(" var xresult = document.evaluate(`//*[@id=\"app\"]/div/a[1]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.click();");
                if (Browser.Title == "京东登录注册")
                {
                    return false;
                }
                Console.WriteLine(Browser.Title);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
