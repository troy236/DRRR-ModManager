using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using HtmlAgilityPack;
using RingRacersModManager.Gamebanana;
using RingRacersModManager.Github;
using RingRacersModManager.StackWalk;
using RingRacersModManager.UI;

namespace RingRacersModManager;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(HashSet<Addon>))]
[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(GamebananaMod))]
[JsonSerializable(typeof(GamebananaFile[]))]
[JsonSerializable(typeof(GamebananaAPIFileGet))]
[JsonSerializable(typeof(GitHubRelease))]
public partial class ModManagerJsonContext : JsonSerializerContext { }

internal class Program {
    public const string VERSION = "1.0.0";
    public const string MessageBoardBaseURL = "https://mb.srb2.org";
    public const string GamebananaBaseURL = "https://gamebanana.com/mods";
    public static Config Config { get; set; }
    public static HttpClient HttpClient { get; set; } = new();
    public static HtmlWeb WebLoader { get; set; } = new();
    public static HashSet<Addon> Addons { get; set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
        StackHelper.Initialize();
        Console.WriteLine($"Ring Racers Mod Manager - Version {VERSION}");
        try {
            Console.Title = $"Mod Manager Console {VERSION}";
        }
        catch { }
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
            Console.WriteLine("A unhandled error occurred:");
            Exception exception = (Exception)e.ExceptionObject;
            HandleError(exception);
        };
        TaskScheduler.UnobservedTaskException += (sender, e) => {
            Console.WriteLine("A unhandled Task error occurred:");
            HandleError(e.Exception);
            e.SetObserved();
        };
        try {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WebLoader.UseCookies = true;
            WebLoader.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "mmconfig.json"))) {
                Config = JsonSerializer.Deserialize(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "mmconfig.json")), ModManagerJsonContext.Default.Config);
            }
            else {
                Config = new();
                Config.Theme = 1;
                Config.StartupAppUpdateCheck = true;
                Config.StartupAddonsUpdateCheck = true;
            }
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "mmaddons.json"))) {
                Addons = JsonSerializer.Deserialize(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "mmaddons.json")), ModManagerJsonContext.Default.HashSetAddon);
            }
            else Addons = new();
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "mmerror.txt"))) {
                Console.WriteLine("A error log has been found from a previous session. Have you sent the data to Github issues or on Discord (troy236)?");
                Console.WriteLine("If you have you can delete the mmerror.txt file in the mod manager folder");
            }
            if (args.Length > 0) {
                //TODO 1-click support, no response from Message board staff
                //Would also need to know how to add custom file protocols for Linux/Mac
                //OneClickInstall(args[0]);
                //return;
            }
        }
        catch (Exception ex) {
            Console.WriteLine("A error occurred on parsing config or addon data:");
            HandleError(ex);
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Launching UI...");
        try {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) {
            //'Unhandled' UI thread errors should get caught here
            Console.WriteLine("A unexpected UI error occurred:");
            HandleError(ex);
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Prints AOT source paths/line numbers on Windows with symbols (Need help reading DWARF file format for other systems)
    /// </summary>
    public static void HandleError(Exception exception) {
        try {
            if (!StackHelper.PrintAOTStackTrace(exception)) {
                //Not on Windows or some other failure
                string exceptionText = exception.ToString();
                Console.WriteLine(exceptionText);
                var fileStream = new FileStream(Path.Combine(AppContext.BaseDirectory, "mmerror.txt"), FileMode.Create, FileAccess.Write, FileShare.None);
                fileStream.Write(Encoding.UTF8.GetBytes(exceptionText));
                fileStream.Dispose();
            }
        }
        catch (Exception ex) {
            Console.WriteLine("Whoops! The error logging broke");
            Console.WriteLine(ex.ToString());
            //In case it breaks in PrintAOTStackTrace also print the original error we were supposed to print
            Console.WriteLine("Original error:");
            Console.WriteLine(exception.ToString());
        }
        Console.WriteLine("Please make a Issue on the Github page with the above information or contact me on Discord (troy236)");
    }
}
