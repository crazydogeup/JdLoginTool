using System;
using System.Diagnostics;
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
            Trace.WriteLine("start");
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
                var phone = LoginService.GetPhone();
                if (string.IsNullOrEmpty(phone))
                {
                    Trace.WriteLine("未获取到手机号码,等待...");
                    Thread.Sleep(1000);
                    continue;
                }
                Trace.WriteLine("取到手机号码,开始设定手机号...");
                //填写手机号码
                SetPhone(phone);
                Thread.Sleep(1000);
                this.Browser.Dispatcher.Invoke(new Action(() =>
                {
                    this.Focus();
                }));
                Trace.WriteLine("点击获取验证码按钮...");
                var r=  await  ClickGetCaptchaButton();
                if (r)
                {
                    Trace.WriteLine("成功!");
                }
                else
                {
                    Trace.WriteLine("失败!!!");

                }
                Trace.WriteLine(r);
                var tryCount = 60;
                while (Running)
                {//开始循环向服务器请求用户的最新验证码
                    var captcha = LoginService.GetCaptcha();
                    if (string.IsNullOrEmpty(captcha))
                    {
                        if (tryCount--<1)
                        {
                            Trace.WriteLine("等不到验证码,用户操作时间过长,跳过,开始下一个用户登录等待:");
                            Task.Factory.StartNew(WaitForNewPhoneNumber);
                           return;
                        } 
                        Thread.Sleep(1000);
                        Trace.WriteLine("等待获取验证码,剩余尝试次数:"+tryCount);
                        continue;
                    }
                    //填写验证码
                    Trace.WriteLine("获取到验证码:["+ captcha+"],开始填写");
                    SetCaptcha(captcha); 
                    Trace.WriteLine("点击登录");
                    var loginResult = await ClickLoginButton();
                    if (loginResult)
                    {//获取cookie
                        Trace.WriteLine("点击登录成功");
                        var cookieStr = "";
                        this.Browser.Dispatcher.Invoke(new Action(() =>
                        {
                            while (Browser.IsLoading)
                            {
                                Thread.Sleep(500);
                            }
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
                              Browser.Address = "https://m.jd.com/";
                              Trace.WriteLine("登录完成,重置cookie");
                            }
                        }));

                    }
                    else
                    {
                        Trace.WriteLine("点击登录失败");
                    } 
                   
                 
                }
            }

        }





        private void ButtonTest_OnClick(object sender, RoutedEventArgs e)
        {
            LoginService.SendCookie("hello world");
        }

        async void SetPhone(string phone)
        {
            try
            {
                var result = await Browser.EvaluateScriptAsPromiseAsync($"var xresult = document.evaluate(`//*[@id='app']/div/div[3]/p[1]/input`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value={phone};p.dispatchEvent(new Event('input'));");
                Trace.WriteLine(result.Result);

            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }

        }

        void SetCaptcha(string captcha)
        {
            try
            {
                Browser.EvaluateScriptAsPromiseAsync($"var xresult = document.evaluate(`//*[@id=\"authcode\"]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value=\"{captcha}\";p.dispatchEvent(new Event('input'));");
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
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
                        Trace.WriteLine(title);
                        Thread.Sleep(1000);
                    }
                    Browser.EvaluateScriptAsPromiseAsync($"var b = document.getElementById('msShortcutLogin');b.click()");

                }));
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }
        async Task<bool> ClickGetCaptchaButton()
        {
            try
            {
              var result=  await Browser.EvaluateScriptAsync("document.querySelector('#app div button').click()");
             
              return result.Success;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                return false;
            }
        }
        async Task<bool> ClickLoginButton()
        {
            try
            {
             var result=   await Browser.EvaluateScriptAsync(" var xresult = document.evaluate(`//*[@id=\"app\"]/div/a[1]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.click();");

                return result.Success;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                return false;
            }
        }
    }
}
