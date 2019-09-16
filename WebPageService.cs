using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using ExCSS;
using PuppeteerSharp;

namespace WebPageUpdated
{
    public enum SelectorType
    {
        Unspecified,
        Css,
        Xpath
    }

    public class WebPageService : IDisposable
    {
        private Page _page;
        private Browser _browser;

        public string PathToScreenshot { get; private set; }
        private WebPageService() { }

        private async Task<WebPageService> InitializeAsync(string webpageUrl, bool useLocalChrome = false)
        {
            if (!useLocalChrome)
            {
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            }

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });

            _page = await _browser.NewPageAsync();

            await _page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1280,
                Height = 720
            });

            await GoToPage(webpageUrl);

            return this;
        }

        public static Task<WebPageService> LoadPage(string webpageUrl)
        {
            if (string.IsNullOrWhiteSpace(webpageUrl))
            {
                throw new ArgumentNullException("webpageUrl", "Please specify a web page URL.");
            }

            var wp = new WebPageService();
            return wp.InitializeAsync(webpageUrl);
        }

        public async Task<WebPageService> GoToPage(string webPageUrl)
        {
            await _page.GoToAsync(webPageUrl);
            return this;
        }

        public async Task<string> GetMd5ValueOfElement(string pathOfElement)
        {
            ElementHandle selector;
            string val = null;

            switch (GetSelectorType(pathOfElement))
            {
                case SelectorType.Unspecified:
                case SelectorType.Css:
                    pathOfElement = !string.IsNullOrWhiteSpace(pathOfElement) ? pathOfElement : "body";
                    selector = await _page.QuerySelectorAsync(pathOfElement);
                    val = await _page.EvaluateFunctionAsync<string>("e => { e.style.border = '5px solid yellow'; return e.textContent }", selector);
                    break;
                case SelectorType.Xpath:
                    selector = (await _page.XPathAsync(pathOfElement))[0];
                    val = await _page.EvaluateFunctionAsync<string>("e => { e.style.border = '5px solid yellow'; return e.textContent }", selector);
                    break;
            }

            return GetMD5(val);
        }

        public async Task<string> TakeScreenshot()
        {
            var screenshotPath = GetTempFilePathWithExtension(".jpg");

            await _page.ScreenshotAsync(screenshotPath, new ScreenshotOptions()
            {
                Quality = 95,
                //  FullPage = true,
                Type = ScreenshotType.Jpeg
            });

            // Base64PageScreenshot = Convert.ToBase64String(File.ReadAllBytes(screenshotPath));
            PathToScreenshot = screenshotPath;

            return PathToScreenshot;
        }

        public static SelectorType GetSelectorType(string selector)
        {
            SelectorType sel = SelectorType.Unspecified;
            try
            {
                XPathExpression.Compile(selector);
                sel = SelectorType.Xpath;
            }
            catch (XPathException) { }

            try
            {
                var parser = new StylesheetParser();
                var stylesheet = parser.ParseSelector(selector);
                if (stylesheet == null)
                {
                    throw new ArgumentException();
                }
                sel = SelectorType.Css;
            }
            catch (ArgumentException) { }

            return sel;
        }



        private static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString() + extension;
            return Path.Combine(path, fileName);
        }

        private static string GetMD5(string str)
        {
            // Use input string to calculate MD5 hash
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(str));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        public void Dispose()
        {
            Task.Run(() => _browser.CloseAsync());
        }
    }
}
