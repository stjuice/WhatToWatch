using Microsoft.Playwright;

var outputPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath("imdb-session.json");
var startUrl = args.Length > 1
    ? args[1]
    : "https://www.imdb.com/";

var outputDirectory = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrWhiteSpace(outputDirectory))
{
    Directory.CreateDirectory(outputDirectory);
}

Console.WriteLine("IMDb session bootstrap");
Console.WriteLine($"Output: {outputPath}");
Console.WriteLine($"Start URL: {startUrl}");
Console.WriteLine();
Console.WriteLine("A headed Chromium window will open.");
Console.WriteLine("1. Complete any IMDb human challenge / consent / login if shown.");
Console.WriteLine("2. Confirm a normal IMDb page loads (e.g. home or a public list).");
Console.WriteLine("3. Return here and press Enter to save storageState.");
Console.WriteLine();

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
});
await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    Locale = "en-US",
});
var page = await context.NewPageAsync();
await page.GotoAsync(startUrl, new PageGotoOptions
{
    WaitUntil = WaitUntilState.DOMContentLoaded,
    Timeout = 120_000,
});

Console.WriteLine("Press Enter after the challenge is solved...");
Console.ReadLine();

await context.StorageStateAsync(new BrowserContextStorageStateOptions
{
    Path = outputPath,
});

Console.WriteLine($"Saved Playwright storageState to: {outputPath}");
Console.WriteLine();
Console.WriteLine("Next (Render):");
Console.WriteLine("  Copy this file to the service disk at /var/data/imdb-session.json");
Console.WriteLine("  Then restart the Web Service so Chromium reloads the session.");
