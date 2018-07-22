using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Youtube
{
    // https://github.com/cefsharp/CefSharp/blob/master/CefSharp.OffScreen.Example/Program.cs#L127

    public partial class Form1 : Form
    {
        private const string START_PAGE_URL = "https://www.youtube.com/";

        ChromiumWebBrowser browser;
        List<ConfigVO> configVOs = null;
        List<LoveConfigVO> loveConfigVOs = null;
        List<ProfileConfigVO> profileConfigVOs = null;
        List<ChannelConfigVO> channelConfigVOs = null;
        List<PasswordConfigVO> passwordConfigVOs = null;

        Dictionary<string, object> programConfig = new Dictionary<string, object>();
        Dictionary<string, object> loveConfig = new Dictionary<string, object>();
        


        public Form1()
        {
            InitializeComponent();
        }

        private void ChangeProxServer(string ip)
        {
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var rc = this.browser.GetBrowser().GetHost().RequestContext;
                var v = new Dictionary<string, object>();
                v["mode"] = "fixed_servers";
                v["server"] = ip;
                string error;
                bool success = rc.SetPreference("proxy", v, out error);                                
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 프로그램 설정파일 로드
            ProgramConfigFileInitialize("./config/program.txt");
            
            // 공통 설정파일 로드
            ConfigFileInitialize("./config/auth.txt",  "./config/comment.txt", "./config/url.txt");            

            // 브라우저 로드
            BrowserInitalize();
        }

      
        private void BrowserInitalize()
        {
            CefSettings cefSettings = new CefSettings();
            cefSettings.Locale = "ko-KR";
            cefSettings.AcceptLanguageList = "ko-KR";
            Cef.Initialize(cefSettings);
            browser = new ChromiumWebBrowser(START_PAGE_URL);
            panel1.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;            
            MainAsync();
        }

        private async void MainAsync()
        {            
            string mode = (String)programConfig["mode"];

            if (mode.Equals("comment"))
            {
                await CommentModeProcessAsync();
            }
            else if (mode.Equals("love"))
            {
                // 좋아요 관련 설정 파일 로드
                LoveConfigFileInitialize("./config/love.txt");

                await LoveModeProcessAsync();
            }
            else if (mode.Equals("test"))
            {
                await TestModeProcessAsync();
            }
            else if (mode.Equals("password"))
            {
                await PasswordModeProcessAsync();
            }
            else if (mode.Equals("profile"))
            {
                // 프로필변경 관련 설정 파일 로드
                ProfileConfigFileInitialize("./config/profile.txt");
                await ProfileModeProcessAsync();
            }
            else if (mode.Equals("make-channel"))
            {
                // 채널 관련 설정 파일 로드
                ChannelConfigFileInitialize("./config/channel.txt");
                await ChannelModeProcessAsync();
            }
        }

        private async Task ChannelModeProcessAsync()
        {
            await LoadPageAsync(browser, START_PAGE_URL);

            for (int i = 0; i < configVOs.Count; i++)
            {
                ConfigVO configVO = configVOs[i];
                ChannelConfigVO channelVO = channelConfigVOs[i];

                // 프록시 서버 변경
                ChangeProxServer(configVO.ip);
                await Task.Delay(Int32.Parse((String)programConfig["prox-change-delay"]));

                // 로그인 처리
                await LoginYoutubeAsync(configVO.username, configVO.password, configVO.getMophnNo());

                // 채널 생성 루프
                foreach (string name in channelVO.list)
                {
                    // 채널 목록으로 이동
                    await LoadPageAsync(browser, "https://www.youtube.com/channel_switcher?feature=settings&next=%2Faccount");

                    // 채널 생성
                    await CreateChannel( name);
                }
                
                // 쿠키 삭제
                DeleteCookie();
                await Task.Delay(Int32.Parse((String)programConfig["cookie-delete-delay"]));
            }

        }

        private async Task CreateChannel(string name)
        {
            browser.Focus();

            // 제이쿼리 로드
            await LoadJqueryAsync();

            // 새 채널 만들기
            await EvaluateScriptAsync("$('div.create-channel-text').click()");

            // 페이지 이동 대기
            await WaitForPageLoadingAsync();

            // 채널명 입력
            await EvaluateScriptAsync(String.Format("document.querySelector('#PlusPageName').value = '{0}'", name));

            // 새 채널 만들기
            await EvaluateScriptAsync("document.querySelector('#submitbutton').click()");

            // 채널생성 대기
            await WaitForPageLoadingAsync();
        }

        private async Task PasswordModeProcessAsync()
        {

        }

        private async Task ProfileModeProcessAsync()
        {

            await LoadPageAsync(browser, START_PAGE_URL);

            for( int i = 0; i < configVOs.Count; i++)             
            {
                ConfigVO configVO = configVOs[i];
                ProfileConfigVO profileConfigVO = profileConfigVOs[i];

                // 프록시 서버 변경
                ChangeProxServer(configVO.ip);
                await Task.Delay(Int32.Parse((String)programConfig["prox-change-delay"]));

                // 로그인 처리
                await LoginYoutubeAsync(configVO.username, configVO.password, configVO.getMophnNo());

                // 설정으로 페이지로 이동
                await LoadPageAsync(browser, "https://aboutme.google.com/u/0/#name");

                // 설정 변경
                await ChangeYoutubeProfileAsync(profileConfigVO.lastName, profileConfigVO.firstName, profileConfigVO.nickName);

                // 쿠키 삭제
                DeleteCookie();
                await Task.Delay(Int32.Parse((String)programConfig["cookie-delete-delay"]));
            }

        }    
    
        // 설정으로 이동
        private async Task ChangeYoutubeProfileAsync(string lastName, string firstName, string nickName)
        {
            await EvaluateScriptAsync(String.Format("document.querySelector(\"[aria-label = '성']\").value = '{0}'", lastName));
            await EvaluateScriptAsync(String.Format("document.querySelector(\"[aria-label = '이름']\").value = '{0}'", firstName));
            await EvaluateScriptAsync(String.Format("document.querySelector(\"[aria-label = '닉네임']\").value = '{0}'", nickName));
            await EvaluateScriptAsync("document.querySelector(\"[data-id = 'EBS5u']\").click()");
            await EvaluateScriptAsync("document.querySelector(\"[data-id = 'EBS5u']\").click()");

            await LoadPageAsync(browser, START_PAGE_URL);            
        }


        private async Task TestModeProcessAsync()
        {
            await LoadPageAsync(browser, START_PAGE_URL);
            
            await EvaluateScriptAsync("$('#text').click()");
        }

        private async Task LoveModeProcessAsync()
        {
            await LoadPageAsync(browser, START_PAGE_URL);

            foreach (ConfigVO configVO in configVOs)
            {
                // 프록시 서버 변경
                ChangeProxServer(configVO.ip);
                await Task.Delay(Int32.Parse((String)programConfig["prox-change-delay"]));

                // 로그인 처리
                await LoginYoutubeAsync(configVO.username, configVO.password, configVO.getMophnNo());

                foreach (LoveConfigVO loveConfigVO in loveConfigVOs)
                {
                    // 좋아요할 동영상 페이지로 이동
                    await LoadPageAsync(browser, loveConfigVO.url);

                    // 최근순 정렬
                    await ChangeCommmentOrderAsync();

                    // 좋아요 클릭
                    await addLoveAsync(loveConfigVO.loveComment);
                }

                // 홈으로 이동
                await MoveHome();

                // 로그 아웃
                await LogOutYoutubeAsync();

                // 쿠키 삭제
                DeleteCookie();
                await Task.Delay(Int32.Parse((String)programConfig["cookie-delete-delay"]));
            }
        }

        private async Task ChangeCommmentOrderAsync()
        {

            bool isFind = false;

            while (!isFind)
            {
                await ScrollAtCurrentPos(200, 100);

                JavascriptResponse x = await EvaluateScriptAsync("document.querySelector('#trigger') !== null");
                if ((Boolean)getResult(x))
                {
                    await EvaluateScriptAsync("document.querySelector('#trigger').click()");

                    await EvaluateScriptAsync("document.querySelector(\"#dropdown a[tabindex=\'-1\']\").click();");

                    isFind = true;
                }
            }
        }

        private async Task addLoveAsync(string loveComment)
        {
            browser.Focus();

            // 제이쿼리 로드
            await LoadJqueryAsync();

            bool isFind = false;

            while (!isFind)
            {
                //await ScrollAtCurrentPos(600, 500);

                await LoadMoreComment(99999);

                JavascriptResponse x = await EvaluateScriptAsync(String.Format("$(\"[slot='content']:contains('{0}')\").length != 0", loveComment));

                if ((Boolean)getResult(x))
                {
                    // 엘리먼트로 이동
                    await EvaluateScriptAsync(String.Format("document.scrollingElement.scrollTop = $(\"[slot='content']:contains('{0}')\").offset().top - 450", loveComment));

                    // 클릭
                    await EvaluateScriptAsync(String.Format("$(\"[slot='content']:contains('{0}')\").closest('#main').find('#like-button').click()", loveComment));
                    isFind = true;
                }
            }
        }

        private async Task LoadMoreComment(int scroll)
        {
            await EvaluateScriptAsync("document.documentElement.scrollTop =" + scroll);
        }

       // 댓글달기 모드
        private async Task CommentModeProcessAsync()
        {         
            await LoadPageAsync(browser, START_PAGE_URL);            

            string[] delays = ((string)programConfig["login-delay"]).Split(',');
            int index = 0;

            foreach (ConfigVO configVO in configVOs)
            {
                // 프록시 서버 변경
                ChangeProxServer(configVO.ip);
                await Task.Delay(Int32.Parse((String)programConfig["prox-change-delay"]));

                if (index != 0) {                    
                    int min = Int32.Parse(delays[0]);
                    int max = Int32.Parse(delays[1]);
                    await RandomWaitAsync( min, max);
                }

                // 로그인 처리
                await LoginYoutubeAsync(configVO.username, configVO.password, configVO.getMophnNo());

                // 댓글달 페이지로 이동
                await LoadPageAsync(browser, configVO.url);

                // 페이지 대기
                await WaitForPageLoadingAsync();

                // 댓글 달기
                await addCommentAsync(configVO.comment);

                // 홈으로 이동
                await MoveHome();

                // 로그 아웃
                await LogOutYoutubeAsync();

                // 쿠키 삭제
                DeleteCookie();
                await Task.Delay(Int32.Parse((String)programConfig["cookie-delete-delay"]));

                index++;
            }
        }

        private async Task RandomWaitAsync(int minValue, int maxValue)
        {
            Random r = new Random();
            int randomValue = r.Next(minValue, maxValue);
            await Task.Delay( randomValue);
        }

        // 쿠키 삭제 메소드
        private void DeleteCookie()
        {
            Cef.GetGlobalCookieManager().DeleteCookies("", "");
        }

        private async Task addCommentAsync(string comment)
        {
            browser.Focus();

            //browser.ShowDevTools();

            await ScrollElementBy("primary", 200, 1000, 1);
            //await ScrollBy(200, 1000, 2);
           
            // 댓글창이 존재하는지 체크 체크            
            await WaitForCheckScript("document.querySelector('#placeholder-area') !== null", 100);
          
            await EvaluateScriptAsync("document.querySelector('#placeholder-area').click()");
            await EvaluateScriptAsync("document.querySelector('.textarea-container #textarea').focus()");
            await EvaluateScriptAsync(String.Format("document.querySelector('.textarea-container #textarea').value = '{0}'", comment));
            await EvaluateScriptAsync("document.querySelector('.textarea-container #textarea').focus()");
            await SendKeyToBrowserAsync(0x0D);
            await EvaluateScriptAsync("document.querySelector('#submit-button').click()");

            // 댓글이 달렸는지 체크
            await WaitForCheckScript("document.querySelector('#placeholder-area[hidden]') !== null", 100);
            
            await Task.Delay(3000);
        }

        private async Task ScrollElementBy(string id ,int height, int interval, int count)
        {
            int p = 0;
            for (int i = 0; i < count; i++)
            {
                p += height;
                await EvaluateScriptAsync("document.getElementById('"+ id +"').scrollTop =" + p);
                await Task.Delay(interval);
            }
        }

        private async Task WaitForCheckScript(string script, int delay)
        {
            while (true)
            {
                JavascriptResponse x = await EvaluateScriptAsync( script);

                Console.WriteLine((Boolean)getResult(x));
                
                if ((Boolean)getResult(x))
                {
                    break;
                }
                else
                {
                    await Task.Delay(delay);
                }
            }

        }

        private async Task MoveHome() {
            await EvaluateScriptAsync("document.querySelector('#logo-icon-container').click();");
            await WaitForPageLoadingAsync();
        }
        
        private async Task SendKeyToBrowserAsync(int keyCode)
        {
            KeyEvent k = new KeyEvent();
            k.WindowsKeyCode = keyCode;
            k.FocusOnEditableField = true;
            k.IsSystemKey = false;
            k.Type = KeyEventType.Char;
            browser.GetBrowser().GetHost().SendKeyEvent(k);

            await Task.Delay(1000);
        }

        private async Task LoginYoutubeAsync( string username, string password, string mophnNo)
        {
            browser.Focus();

            await EvaluateScriptAsync("document.querySelector('#text').click()");
            await WaitForPageLoadingAsync();
            await EvaluateScriptAsync("document.querySelector('#identifierId').focus()");
            await EvaluateScriptAsync(String.Format("document.querySelector('#identifierId').value = '{0}'", username.Trim()));
            await EvaluateScriptAsync("document.querySelector('#identifierNext').click()");
            await EvaluateScriptAsync("document.querySelector('input[type=password]').focus()");
            await EvaluateScriptAsync(String.Format("document.querySelector('input[type=password]').value = '{0}'", password.Trim()));
            await EvaluateScriptAsync("document.querySelector('#passwordNext').click()");

            // 로그인 대기 시간
            await Task.Delay(3000);

            // 복구 전화번호
            JavascriptResponse x = await EvaluateScriptAsync("document.querySelector('[data-challengetype = \"13\"]') !== null");
            if ((Boolean)getResult(x)) {
                // 복구전화 클릭
                await EvaluateScriptAsync("document.querySelector('[data-challengetype = \"13\"]').click()");
                // 전화번호 입력
                await EvaluateScriptAsync("document.querySelector('.whsOnd.zHQkBf').click()");
                await EvaluateScriptAsync(String.Format("document.querySelector('.whsOnd.zHQkBf').value = '{0}'", mophnNo));
                // 다음
                await EvaluateScriptAsync("document.querySelector('.ZFr60d.CeoRYc').click()");
            };

            await WaitForPageLoadingAsync();
        }

        private object getResult(JavascriptResponse x)
        {
            return x.Success ? (x.Result ?? "null") : x.Message;
        }

        private async Task LogOutYoutubeAsync()
        {
            await EvaluateScriptAsync("document.querySelector('#avatar-btn').click()");
            await EvaluateScriptAsync("document.querySelector('a[href=\"/logout\"]').click()");
            await WaitForPageLoadingAsync();
        }

        private async Task WaitForPageLoadingAsync()
        {
            while (((IWebBrowser)browser).IsLoading)
            {                                
                await Task.Delay(10);
            }
        }

        private async Task<JavascriptResponse> EvaluateScriptAsync( string script)
        {
           Console.WriteLine(script);
           JavascriptResponse x = await browser.EvaluateScriptAsync(script);            
           await Task.Delay(Int32.Parse((String)programConfig["script-delay"]));
           return x;
        }


        public Task LoadPageAsync(IWebBrowser browser, string address = null)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<LoadingStateChangedEventArgs> handler = null;
            handler = (sender, args) =>
            {
                if (!args.IsLoading)
                {
                    browser.LoadingStateChanged -= handler;
                    tcs.TrySetResult(true);
                }
            };

            browser.LoadingStateChanged += handler;            

            if (!string.IsNullOrEmpty(address))
            {
                browser.Load(address);                
            }
            
            return tcs.Task;
        }

        private async Task LoadJqueryAsync()
        {
            await EvaluateScriptAsync("var element1 = document.createElement('script');element1.src = '//ajax.googleapis.com/ajax/libs/jquery/2.1.1/jquery.min.js';element1.type='text/javascript';document.getElementsByTagName('head')[0].appendChild(element1);");
        }

        private void ExitMenuItemClick(object sender, EventArgs e)
        {
            browser.Dispose();
            Cef.Shutdown();
            Close();
        }

        private async Task ScrollBy(int height, int interval, int count)
        {
            int p = 0;
            for (int i = 0; i < count; i++)
            {
                p += height;
                await EvaluateScriptAsync("document.documentElement.scrollTop =" + p);                
                await Task.Delay(interval);
            }
        }

        private async Task ScrollAtCurrentPos(int height, int interval)
        {
            await EvaluateScriptAsync(String.Format("document.documentElement.scrollTop = (document.documentElement.scrollTop + {0})", height));
            await Task.Delay(interval);
        }

        private void ConfigFileInitialize(string authFilePath, string commentFilePath, string urlFilePath)
        {
            string authLine, commentLine, urlLine;
            int counter = 0;

            StreamReader authFile = new StreamReader(authFilePath, Encoding.Default, true);
            StreamReader commentFile = new StreamReader(commentFilePath, Encoding.Default, true);
            StreamReader urlFile = new StreamReader(urlFilePath, Encoding.Default, true);

            List<ConfigVO> configVOs = new List<ConfigVO>();

            while (((authLine = authFile.ReadLine()) != null && !authLine.Equals("")) &&
                   ((commentLine = commentFile.ReadLine()) != null && !commentLine.Equals("")) &&
                   ((urlLine = urlFile.ReadLine()) != null && !urlLine.Equals(""))
                )
            {
                ConfigVO configVO = new ConfigVO();
                string[] s = authLine.Split('/');
                string comment = commentLine;
                string url = urlLine;

                configVO.ip = s[0];
                configVO.username = s[1];
                configVO.password = s[2];
                configVO.nickname = s[3];
                configVO.url = url;                
                configVO.comment = comment;
                configVOs.Add(configVO);

                counter++;
            }

            authFile.Close();
            commentFile.Close();
            urlFile.Close();

            this.configVOs = configVOs;
        }

        private void ProgramConfigFileInitialize(string filePath)
        {
            string line;
            int counter = 0;

            StreamReader file = new StreamReader(filePath, Encoding.Default, true);

            while (((line = file.ReadLine()) != null && !file.Equals("")))
            {
                string[] s = line.Split('=');
                programConfig.Add(s[0], s[1]);
                counter++;
            }

            file.Close();
        }

        private void LoveConfigFileInitialize(string filePath)
        {
            string line;
            int counter = 0;

            List<LoveConfigVO> loveConfigVOs = new List<LoveConfigVO>();

            StreamReader file = new StreamReader(filePath, Encoding.Default, true);

            while (((line = file.ReadLine()) != null && !file.Equals("")))
            {
                LoveConfigVO loveConfigVO = new LoveConfigVO();

                string[] s = line.Split('\t');

                loveConfigVO.url = s[0];
                loveConfigVO.loveComment = s[1];

                loveConfigVOs.Add(loveConfigVO);

                counter++;
            }

            file.Close();

            this.loveConfigVOs = loveConfigVOs;
        }

        private void ProfileConfigFileInitialize(string filePath)
        {
            string line;
            int counter = 0;

            List<ProfileConfigVO> profileConfigVOs = new List<ProfileConfigVO>();

            StreamReader file = new StreamReader(filePath, Encoding.Default, true);

            while (((line = file.ReadLine()) != null && !file.Equals("")))
            {
                ProfileConfigVO profileConfigVO = new ProfileConfigVO();

                string[] s = line.Split('\t');

                profileConfigVO.lastName = s[0];
                profileConfigVO.firstName = s[1];
                profileConfigVO.nickName = s[2];

                profileConfigVOs.Add(profileConfigVO);

                counter++;
            }

            file.Close();

            this.profileConfigVOs = profileConfigVOs;
        }

        private void ChannelConfigFileInitialize(string filePath)
        {
            string line;
            int counter = 0;

            List<ChannelConfigVO> channelConfigVOs = new List<ChannelConfigVO>();

            StreamReader file = new StreamReader(filePath, Encoding.Default, true);

            while (((line = file.ReadLine()) != null && !file.Equals("")))
            {
                ChannelConfigVO channelConfigVO = new ChannelConfigVO();
                List<string> list = new List<string>();

                string[] s = line.Split('/');

                foreach (string item in s)
                {
                    list.Add(item);
                }

                channelConfigVO.list = list;

                channelConfigVOs.Add(channelConfigVO);

                counter++;
            }

            file.Close();

            this.channelConfigVOs = channelConfigVOs;
        }


    }
}
