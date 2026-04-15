using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace FanzinePress.Web.Services;

public class PdfService : IAsyncDisposable
{
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ILogger<PdfService> _logger;
    private readonly IConfiguration _configuration;

    public PdfService(ILogger<PdfService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsClosed: false })
            return _browser;

        await _initLock.WaitAsync();
        try
        {
            if (_browser is { IsClosed: false })
                return _browser;

            // Prefer a pre-installed system Chromium (docker image installs one).
            // Falls back to BrowserFetcher for local dev if no path is configured.
            var executablePath = _configuration["FanzinePress:ChromiumPath"]
                ?? Environment.GetEnvironmentVariable("FANZINE_CHROMIUM_PATH");

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                _logger.LogInformation("No FANZINE_CHROMIUM_PATH set, downloading Chromium via BrowserFetcher...");
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
            }
            else
            {
                _logger.LogInformation("Using system Chromium at {Path}", executablePath);
            }

            _logger.LogInformation("Launching headless browser...");
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = string.IsNullOrWhiteSpace(executablePath) ? null : executablePath,
                Args =
                [
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                ]
            });

            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<byte[]> RenderPdfAsync(string url, IEnumerable<CookieParam>? cookies = null)
    {
        var browser = await GetBrowserAsync();
        await using var page = await browser.NewPageAsync();

        if (cookies != null)
        {
            var cookieArray = cookies.ToArray();
            if (cookieArray.Length > 0)
            {
                await page.SetCookieAsync(cookieArray);
            }
        }

        // Navigate and wait for full load
        await page.GoToAsync(url, null, [WaitUntilNavigation.Load]);

        // Give CSS columns and images a moment to settle
        await Task.Delay(500);

        var pdf = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "15mm",
                Bottom = "15mm",
                Left = "15mm",
                Right = "15mm"
            }
        });

        return pdf;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is { IsClosed: false })
        {
            await _browser.CloseAsync();
            _browser.Dispose();
        }
    }
}
