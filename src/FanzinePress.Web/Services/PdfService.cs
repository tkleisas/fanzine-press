using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace FanzinePress.Web.Services;

public class PdfService : IAsyncDisposable
{
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ILogger<PdfService> _logger;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
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

            _logger.LogInformation("Downloading Chromium if needed...");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            _logger.LogInformation("Launching headless browser...");
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox"]
            });

            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<byte[]> RenderPdfAsync(string url)
    {
        var browser = await GetBrowserAsync();
        await using var page = await browser.NewPageAsync();

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
