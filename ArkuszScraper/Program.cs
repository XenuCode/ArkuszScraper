// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

Console.Clear();
var optionList = new List<Option>();
optionList.Add(new Option("Polski podstawa", "https://arkusze.pl/jezyk-polski-matura-poziom-podstawowy/"));
optionList.Add(new Option("Polski rozszerzenie", "https://arkusze.pl/jezyk-polski-matura-poziom-rozszerzony/"));
optionList.Add(new Option("Matematyka podstawa", "https://arkusze.pl/matematyka-matura-poziom-podstawowy/"));
optionList.Add(new Option("Matematyka rozszerzenie", "https://arkusze.pl/matematyka-matura-poziom-rozszerzony/"));
optionList.Add(new Option("Fizyka podstawa", "https://arkusze.pl/fizyka-matura-poziom-podstawowy/"));
optionList.Add(new Option("Fizyka rozszerzenie", "https://arkusze.pl/fizyka-matura-poziom-rozszerzony/"));

var picking = true;
var cursor = 0;
ConsoleKeyInfo key;
while (picking)
{
    Console.WriteLine("!!!CHROME BROWSER REQUIRED!!!");
    Console.WriteLine("Select what to download");
    Console.WriteLine("Navigate using arrows, press enter to mark what to download");
    Console.WriteLine("Press `y` to accept and begin download");
    for (var x = 0; x < optionList.Count; x++)
    {
        var render = "";
        if (cursor == x) render += " ";
        if (optionList[x].active)
            render += "[x]: ";
        else
            render += "[ ]: ";
        render += optionList[x].text;
        Console.WriteLine(render);
    }

    key = Console.ReadKey(false);
    if (key.Key == ConsoleKey.Y) break;

    if (key.Key == ConsoleKey.Enter) optionList[cursor].active = !optionList[cursor].active;
    switch (key.Key)
    {
        case ConsoleKey.DownArrow:
        {
            if (cursor < optionList.Count - 1)
                cursor++;
            break;
        }
        case ConsoleKey.UpArrow:
        {
            if (cursor > 0)
                cursor--;
            break;
        }
    }

    Console.Clear();
}

Console.WriteLine(
    "Chrome browser will pop up If it halts then its downloading aromatization drivers :D, give it some time");
new DriverManager().SetUpDriver(new ChromeConfig());
var driver = new ChromeDriver();
driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);


for (var i = 0; i < optionList.Count; i++)
    if (optionList[i].active)
    {
        var result = Parallel.ForEachAsync(GetLinks(optionList[i].url, optionList[i].text), async (link, token) =>
        {
            await DownloadFileAsync(link, $"{optionList[i].text}/");
        });
        string[] loadingCharacters = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var index = 0;
        while (!result.IsCompleted)
        {
            ClearCurrentConsoleLine();
            Console.Write("\rDownloading " + loadingCharacters[index]);
            index = (index + 1) % loadingCharacters.Length;
            Thread.Sleep(100);
        }

        ClearCurrentConsoleLine();
        Console.WriteLine("Finished: " + optionList[i].text);
    }

static void ClearCurrentConsoleLine()
{
    var currentLineCursor = Console.CursorTop;
    Console.SetCursorPosition(0, Console.CursorTop);
    Console.Write(new string(' ', Console.WindowWidth));
    Console.SetCursorPosition(0, currentLineCursor);
}

driver.Quit();

Console.WriteLine("Finished, press eny key to close");
Console.ReadKey(false);

List<string> GetLinks(string downloadPath, string savePath)
{
    var links = new List<string>();
    driver.Navigate().GoToUrl(downloadPath);
    new WebDriverWait(driver, TimeSpan.FromSeconds(20)).Until(d => driver.FindElement(By.Id("main")));
    var elements = driver.FindElement(By.ClassName("row-hover")).FindElements(By.TagName("a"));
    var x = 0;
    foreach (var element in elements)
    {
        driver.Navigate().GoToUrl(element.GetAttribute("href"));
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        var msgs = driver.FindElements(By.CssSelector("div.msgbox.msgbox-arkusz"));
        wait.Until(d => driver.FindElement(By.CssSelector("div.msgbox.msgbox-arkusz")));
        foreach (var msg in msgs)
        {
            var link = msg.FindElement(By.TagName("a")).GetAttribute("href");
            if (!link.Contains("wzory")) links.Add(msg.FindElement(By.TagName("a")).GetAttribute("href"));
        }

        x++;
        driver.Navigate().Back();
        if (x > 4)
            break;
    }

    return links;
}

static async Task DownloadFileAsync(string fileUrl, string destinationPath)
{
    var year = ExtractYear(fileUrl);
    using (var httpClient = new HttpClient())
    {
        using (var response = await httpClient.GetAsync(fileUrl))
        {
            response.EnsureSuccessStatusCode();

            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);
            using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                   stream = new FileStream(destinationPath + year + "_" + ExtractFileName(fileUrl), FileMode.Create,
                       FileAccess.Write, FileShare.None, 1024 * 512, true))
            {
                await contentStream.CopyToAsync(stream);
            }
        }
    }
}

static string ExtractFileName(string url)
{
    var regex = new Regex(@"/([\w-]+\.pdf)$");
    var match = regex.Match(url);

    if (match.Success) return match.Groups[1].Value.Replace(ExtractYear(url), "_");

    return "No match found";
}

static string ExtractYear(string url)
{
    var regex = new Regex(@"(\d{4})");
    var match = regex.Match(url);

    if (match.Success) return match.Groups[1].Value;

    return "No match found";
}

internal class Option
{
    public bool active;
    public string text;

    public string url;

    public Option(string tText, string tUrl)
    {
        text = tText;
        url = tUrl;
    }
}