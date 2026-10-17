using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Fokus.Core.Services;

public interface ISoundService
{
    void PlaySound(string? filePath, double volumeFactor = 1.0, int masterVolumePercent = 100);
    void PlayTick(double volumeFactor = 1.0, int masterVolumePercent = 100);
    void PlayDefaultChime();
}

public class SoundService : ISoundService
{
    public void PlaySound(string? filePath, double volumeFactor = 1.0, int masterVolumePercent = 100)
    {
        if (masterVolumePercent <= 0)
            return;

        Task.Run(() =>
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    using var player = new SoundPlayer(filePath);
                    player.Play();
                }
                else
                {
                    PlayDefaultChime();
                }
            }
            catch
            {
                SystemSounds.Beep.Play();
            }
        });
    }

    public void PlayTick(double volumeFactor = 1.0, int masterVolumePercent = 100)
    {
        if (masterVolumePercent <= 0)
            return;

        Task.Run(() =>
        {
            try
            {
                // Subtle Windows system asterix or low click
                SystemSounds.Asterisk.Play();
            }
            catch
            {
            }
        });
    }

    public void PlayDefaultChime()
    {
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch
        {
            SystemSounds.Beep.Play();
        }
    }
}

public interface IScriptService
{
    void RunScript(string? scriptPath);
}

public class ScriptService : IScriptService
{
    public void RunScript(string? scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            return;

        Task.Run(() =>
        {
            try
            {
                string extension = Path.GetExtension(scriptPath).ToLowerInvariant();
                ProcessStartInfo psi;

                if (string.Equals(extension, ".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = scriptPath,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                }

                using var proc = Process.Start(psi);
            }
            catch
            {
            }
        });
    }
}

public interface IMediaControlService
{
    void PauseMedia();
}

public class MediaControlService : IMediaControlService
{
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const uint KEYEVENTF_EXTENDEDKEY = 1;
    private const uint KEYEVENTF_KEYUP = 2;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    public void PauseMedia()
    {
        Task.Run(() =>
        {
            try
            {
                // Send standard Windows Play/Pause media key
                keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY, 0);
                keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }
            catch
            {
            }
        });
    }
}

public interface IStartupService
{
    bool IsAutostartEnabled();
    void SetAutostart(bool enabled);
}

public class StartupService : IStartupService
{
    private const string AppName = "Fokus";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetAutostart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null)
                return;

            if (enabled)
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
        }
    }
}
