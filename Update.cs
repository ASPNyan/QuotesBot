using System.Diagnostics;
using Octokit;
#pragma warning disable CS0618

namespace QuotesBot;

public class Update 
{
    private const string Version = "v1.2.0";
    
    public Update()
    {
        CheckUpdate();
    }
    
    private static async void CheckUpdate()
    {
        GitHubClient Client = new(new ProductHeaderValue("QuotesBot"));
        IReadOnlyList<Release> Releases = await Client.Repository.Release.GetAll("ASPNyan", "QuotesBot");

        string Latest = Releases[0].TagName;

        if (Version == Latest) return;
        
        GetUpdate();
    }

    private static void GetUpdate()
    {
        Console.WriteLine("Update/Changes Found, Press Any Key to Update, Press Nothing to Cancel.");
        bool KeyRead = Task.Factory.StartNew(Console.ReadKey).Wait(TimeSpan.FromSeconds(5.0));

        if (!KeyRead) return;

        // ReSharper disable once UnusedVariable
        UpdateFile Update = new UpdateFile();
        
        ProcessStartInfo StartInfo = new ProcessStartInfo
        {
            FileName = @"powershell.exe",
            Arguments = "-ExecutionPolicy Bypass -File ./Update.ps1",
            WorkingDirectory = Directory.GetCurrentDirectory(), 
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
        };
        
        Process Process = new Process
        {
            StartInfo = StartInfo
        };
        
        Process.Start();
    }
}

internal sealed class UpdateFile
{
    private const string UpdateFileData = "Write-Output \"Killing QuotesBot Process\"\n" +
                                          "Stop-Process -Name \"QuotesBot.exe\"\n\n" +
                                          "Clear-Host\n\n" +
                                          "Write-Output \"Getting Latest Version..\"\n" +
                                "Invoke-WebRequest -Uri https://github.com/ASPNyan/QuotesBot/releases/latest/download/QuotesBot.zip -OutFile QuotesBot.zip" +
                                "\n\nClear-Host\n\nWrite-Output \"Clearing Data..\"\n" +
                                "Remove-Item -Path QuotesBot.** -Force\nRemove-Item -Path README.md -Force\n" +
                                "Remove-Item -Path LICENSE.md -Force\n\nClear-Host\n\nWrite-Output \"Extracting Data..\"\n" +
                                "Expand-Archive -Path QuotesBot.zip -DestinationPath . -Force\n\nClear-Host\n\n" +
                                "Write-Output \"Clearing Download..\"\nRemove-Item -Path QuotesBot.zip -Force\n\n" +
                                "Clear-Host\n\nWrite-Output \"Done!\"\n\nClear-Host\n\nStart-Process -FilePath \"QuotesBot.exe\"\n\n" +
                                "exit"; // Yes, PowerShell.

    public UpdateFile()
    {
        File.Create("Update.ps1").Close();
        
        File.WriteAllText("Update.ps1", UpdateFileData);
    }
}