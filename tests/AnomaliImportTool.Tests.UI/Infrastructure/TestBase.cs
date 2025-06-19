using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
#if WINDOWS
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
#endif
using Xunit;
using FluentAssertions;

namespace AnomaliImportTool.Tests.UI.Infrastructure;

/// <summary>
/// Base class for UI automation tests providing common setup, teardown, and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected IConfiguration Configuration { get; private set; }
    protected ILogger<TestBase> Logger { get; private set; }
#if WINDOWS
    protected AutomationBase? Automation { get; private set; }
    protected Application? Application { get; private set; }
#endif
    protected IWebDriver? WebDriver { get; private set; }
    protected string ScreenshotPath { get; private set; }
    protected TimeSpan DefaultTimeout { get; private set; }
    protected bool IsDisposed { get; private set; }

    public TestBase()
    {
        Configuration = LoadConfiguration();
        Logger = CreateLogger();
        ScreenshotPath = Configuration.GetValue<string>("TestConfiguration:ScreenshotPath") ?? "./Screenshots";
        DefaultTimeout = TimeSpan.FromMilliseconds(Configuration.GetValue<int>("TestConfiguration:TestTimeout", 30000));
        
        Directory.CreateDirectory(ScreenshotPath);
        
        Logger.LogInformation("UI Test base initialized. Platform: {Platform}", GetCurrentPlatform());
    }

    protected virtual async Task SetupAsync()
    {
        try
        {
            var platform = GetCurrentPlatform();
            Logger.LogInformation("Setting up UI test for platform: {Platform}", platform);

            switch (platform)
            {
                case TestPlatform.Windows:
                    await SetupWindowsAppAsync();
                    break;
                case TestPlatform.WebAssembly:
                    await SetupWebAssemblyAsync();
                    break;
                case TestPlatform.Linux:
                    await SetupLinuxAppAsync();
                    break;
                case TestPlatform.macOS:
                    await SetupMacOSAppAsync();
                    break;
                default:
                    throw new PlatformNotSupportedException($"Platform {platform} is not supported for UI testing");
            }

            // Wait for application to fully load
            var launchWait = Configuration.GetValue<int>("TestConfiguration:WaitForAppLaunch", 5000);
            await Task.Delay(launchWait);
            
            Logger.LogInformation("UI test setup completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to setup UI test");
            await TakeScreenshotAsync("setup_failure");
            throw;
        }
    }

    protected virtual async Task TeardownAsync()
    {
        try
        {
            Logger.LogInformation("Starting UI test teardown");

            if (WebDriver != null)
            {
                WebDriver.Quit();
                WebDriver.Dispose();
                WebDriver = null;
            }

#if WINDOWS
            if (Application != null)
            {
                Application.Close();
                Application.Dispose();
                Application = null;
            }

            if (Automation != null)
            {
                Automation.Dispose();
                Automation = null;
            }
#endif

            Logger.LogInformation("UI test teardown completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during UI test teardown");
        }
    }

    protected virtual async Task TakeScreenshotAsync(string name)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"{name}_{timestamp}";

            if (WebDriver != null && WebDriver is ITakesScreenshot screenshotDriver)
            {
                var screenshot = screenshotDriver.GetScreenshot();
                var filePath = Path.Combine(ScreenshotPath, $"{filename}.png");
                screenshot.SaveAsFile(filePath);
                Logger.LogInformation("Screenshot saved: {FilePath}", filePath);
            }
#if WINDOWS
            else if (Application != null)
            {
                try
                {
                    var filePath = Path.Combine(ScreenshotPath, $"{filename}.png");
                    var mainWindow = Application.GetMainWindow(Automation);
                    mainWindow.CaptureToFile(filePath);
                    Logger.LogInformation("App screenshot taken: {Name}", filename);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to take app screenshot: {Name}", filename);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to take screenshot: {Name}", name);
        }
    }

    protected virtual async Task WaitForElementAsync(string elementName, TimeSpan? timeout = null)
    {
        var waitTime = timeout ?? DefaultTimeout;
        var endTime = DateTime.Now.Add(waitTime);

        while (DateTime.Now < endTime)
        {
            try
            {
#if WINDOWS
                if (Application != null && Automation != null)
                {
                    var mainWindow = Application.GetMainWindow(Automation);
                    var element = mainWindow.FindFirstDescendant(cf => cf.ByName(elementName));
                    if (element != null)
                    {
                        Logger.LogDebug("Element found: {ElementName}", elementName);
                        return;
                    }
                }
                else 
#endif
                if (WebDriver != null)
                {
                    var wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(1));
                    wait.Until(driver => driver.FindElement(By.Name(elementName)));
                    Logger.LogDebug("Web element found: {ElementName}", elementName);
                    return;
                }
            }
            catch (WebDriverTimeoutException)
            {
                // Continue waiting
            }
            catch (TimeoutException)
            {
                // Continue waiting
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Element '{elementName}' not found within {waitTime.TotalSeconds} seconds");
    }

    protected virtual async Task ClickElementAsync(string elementName)
    {
        try
        {
            await WaitForElementAsync(elementName);

#if WINDOWS
            if (Application != null && Automation != null)
            {
                var mainWindow = Application.GetMainWindow(Automation);
                var element = mainWindow.FindFirstDescendant(cf => cf.ByName(elementName));
                element?.Click();
                Logger.LogDebug("Clicked element: {ElementName}", elementName);
            }
            else 
#endif
            if (WebDriver != null)
            {
                var element = WebDriver.FindElement(By.Name(elementName));
                element.Click();
                Logger.LogDebug("Clicked web element: {ElementName}", elementName);
            }

            // Small delay to allow UI to respond
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to click element: {ElementName}", elementName);
            await TakeScreenshotAsync($"click_failure_{elementName}");
            throw;
        }
    }

    protected virtual async Task EnterTextAsync(string elementName, string text)
    {
        try
        {
            await WaitForElementAsync(elementName);

#if WINDOWS
            if (Application != null && Automation != null)
            {
                var mainWindow = Application.GetMainWindow(Automation);
                var element = mainWindow.FindFirstDescendant(cf => cf.ByName(elementName));
                if (element != null)
                {
                    element.Focus();
                    element.AsTextBox().Text = text;
                    Logger.LogDebug("Entered text in element: {ElementName}", elementName);
                }
            }
            else 
#endif
            if (WebDriver != null)
            {
                var element = WebDriver.FindElement(By.Name(elementName));
                element.Clear();
                element.SendKeys(text);
                Logger.LogDebug("Entered text in web element: {ElementName}", elementName);
            }

            await Task.Delay(300);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to enter text in element: {ElementName}", elementName);
            await TakeScreenshotAsync($"text_entry_failure_{elementName}");
            throw;
        }
    }

    protected virtual async Task AssertElementExistsAsync(string elementName, TimeSpan? timeout = null)
    {
        try
        {
            await WaitForElementAsync(elementName, timeout);
            Logger.LogDebug("Element assertion passed: {ElementName}", elementName);
        }
        catch (TimeoutException)
        {
            await TakeScreenshotAsync($"assertion_failure_{elementName}");
            throw new AssertionFailedException($"Element '{elementName}' does not exist");
        }
    }

    protected virtual async Task AssertElementTextAsync(string elementName, string expectedText)
    {
        try
        {
            await WaitForElementAsync(elementName);
            string actualText = "";

#if WINDOWS
            if (Application != null && Automation != null)
            {
                var mainWindow = Application.GetMainWindow(Automation);
                var element = mainWindow.FindFirstDescendant(cf => cf.ByName(elementName));
                actualText = element?.Name ?? "";
            }
            else 
#endif
            if (WebDriver != null)
            {
                var element = WebDriver.FindElement(By.Name(elementName));
                actualText = element.Text;
            }

            actualText.Should().Be(expectedText, $"Element '{elementName}' should have text '{expectedText}'");
            Logger.LogDebug("Element text assertion passed: {ElementName} = '{Text}'", elementName, actualText);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Element text assertion failed: {ElementName}", elementName);
            await TakeScreenshotAsync($"text_assertion_failure_{elementName}");
            throw;
        }
    }

    private async Task SetupWindowsAppAsync()
    {
        var appPath = Configuration.GetValue<string>("TestConfiguration:ApplicationPath:Windows");
        if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
        {
            throw new FileNotFoundException($"Windows application not found at: {appPath}");
        }

#if WINDOWS
        Automation = new UIA3Automation();
        Application = FlaUI.Core.Application.Launch(appPath);
        
        // Wait for the application to start
        await Task.Delay(2000);
#else
        // On non-Windows platforms, just launch the process
        System.Diagnostics.Process.Start(appPath);
        await Task.Delay(3000);
#endif
        
        Logger.LogInformation("Windows app started: {AppPath}", appPath);
    }

    private async Task SetupWebAssemblyAsync()
    {
        var url = Configuration.GetValue<string>("TestConfiguration:WebAssemblyUrl", "http://localhost:5000");
        var browserType = Configuration.GetValue<string>("TestConfiguration:PlatformSettings:WebAssembly:BrowserType", "Chrome");

        var options = new ChromeOptions();
        var arguments = Configuration.GetSection("TestConfiguration:PlatformSettings:WebAssembly:BrowserArguments").Get<string[]>();
        if (arguments != null)
        {
            options.AddArguments(arguments);
        }

        WebDriver = new ChromeDriver(options);
        WebDriver.Navigate().GoToUrl(url);
        WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(Configuration.GetValue<int>("TestConfiguration:ImplicitWait", 10000));

        Logger.LogInformation("WebAssembly app started in browser: {Url}", url);
    }

    private async Task SetupLinuxAppAsync()
    {
        var appPath = Configuration.GetValue<string>("TestConfiguration:ApplicationPath:Linux");
        if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
        {
            throw new FileNotFoundException($"Linux application not found at: {appPath}");
        }

        // For Linux, FlaUI is not available, so we'll use basic process launching
        // In a real scenario, you might use tools like AutoItX or browser automation
        Logger.LogWarning("Native UI automation on Linux requires platform-specific tools. Using basic process launch.");
        
        var process = System.Diagnostics.Process.Start(appPath);
        await Task.Delay(3000); // Wait for app to start
        
        Logger.LogInformation("Linux app started: {AppPath}", appPath);
    }

    private async Task SetupMacOSAppAsync()
    {
        var appPath = Configuration.GetValue<string>("TestConfiguration:ApplicationPath:macOS");
        if (string.IsNullOrEmpty(appPath) || !Directory.Exists(appPath))
        {
            throw new DirectoryNotFoundException($"macOS application not found at: {appPath}");
        }

        // For macOS, FlaUI is not available, so we'll use basic process launching
        // In a real scenario, you might use tools like Appium or native macOS automation
        Logger.LogWarning("Native UI automation on macOS requires platform-specific tools. Using basic process launch.");
        
        var process = System.Diagnostics.Process.Start("open", appPath);
        await Task.Delay(3000); // Wait for app to start
        
        Logger.LogInformation("macOS app started: {AppPath}", appPath);
    }

    private TestPlatform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TestPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return TestPlatform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return TestPlatform.macOS;

        // Default to WebAssembly for cross-platform testing
        return TestPlatform.WebAssembly;
    }

    private IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("TestConfiguration.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables("ANOMALI_TEST_")
            .Build();
    }

    private ILogger<TestBase> CreateLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole()
                   .SetMinimumLevel(Enum.Parse<LogLevel>(Configuration.GetValue<string>("TestConfiguration:LogLevel", "Information")));
        });

        return loggerFactory.CreateLogger<TestBase>();
    }

    public virtual void Dispose()
    {
        if (!IsDisposed)
        {
            TeardownAsync().GetAwaiter().GetResult();
            IsDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

public enum TestPlatform
{
    Windows,
    Linux,
    macOS,
    WebAssembly
}

public class AssertionFailedException : Exception
{
    public AssertionFailedException(string message) : base(message) { }
    public AssertionFailedException(string message, Exception innerException) : base(message, innerException) { }
} 