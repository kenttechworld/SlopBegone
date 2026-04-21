using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace SlopBegone.Handlers
{
    class AiSlopHandler
    {
        public static void DisableSlop()
        {
            RemoveCopilot();
            RemoveRecall();
            BlockCopilotRegistry();
            BlockRecallRegistry();
            DisableAITasks();
            DisableAIServices();
            BlockViaWindowsUpdate();
        }

        private static void RunPowerShell(string script)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            using var proc = Process.Start(psi)!;
            proc.WaitForExit();

        }

        private static void EditRegistry(string path, string name, int value)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(path, writable: true);
                key.SetValue(name, value, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                // messagebox might not be ideal here since some packages may not exist on all versions, so just log to console
                Debug.WriteLine($"[ERR]: {ex.Message}");
            }
        }


        private static void RemoveCopilot()
        {

            string[] packages =
            [
                "Microsoft.Windows.Ai.Copilot.Provider",
                "Microsoft.Copilot",
                "MicrosoftWindows.Client.CoPilot",
                "Microsoft.Windows.Copilot",
                "Microsoft.CoPilot",
            ];

            foreach (var pkg in packages)
            {
                try
                {
                    // Remove for current user
                    RunPowerShell($"Get-AppxPackage -Name '{pkg}' | Remove-AppxPackage -ErrorAction SilentlyContinue");

                    // Remove provisioned (blocks reinstall for new users)
                    RunPowerShell($"Get-AppxProvisionedPackage -Online | Where-Object DisplayName -like '*{pkg}*' | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue");
                }
                catch (Exception ex)
                {
                    // messagebox might not be ideal here since some packages may not exist on all versions, so just log to console
                    Debug.WriteLine($"[ERR] {pkg}: {ex.Message}");
                }
            }
        }

        private static void RemoveRecall()
        {

            string[] packages =
            [
                "MicrosoftWindows.Client.AIX",
                "MicrosoftWindows.Client.Recall",
                "Microsoft.Windows.Recall",
                "Microsoft.WindowsAIHost",
            ];

            foreach (var pkg in packages)
            {
                try
                {
                    RunPowerShell($"Get-AppxPackage -Name '{pkg}' | Remove-AppxPackage -ErrorAction SilentlyContinue");
                    RunPowerShell($"Get-AppxProvisionedPackage -Online | Where-Object DisplayName -like '*{pkg}*' | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue");

                }
                catch (Exception ex)
                {
                    // messagebox might not be ideal here since some packages may not exist on all versions, so just log to console
                    Debug.WriteLine($"[ERR] {pkg}: {ex.Message}");
                }
            }

            try
            {

                // Disable Recall feature via DISM
                RunPowerShell("Disable-WindowsOptionalFeature -Online -FeatureName 'Recall' -NoRestart -ErrorAction SilentlyContinue");

            }
            catch (Exception ex)
            {
                // messagebox might not be ideal here since some packages may not exist on all versions, so just log to console
                Debug.WriteLine($"[ERR]: {ex.Message}");
            }
        }

        private static void BlockCopilotRegistry()
        {

            var settings = new (string path, string name, int value)[]
            {
            // Disable Copilot in Windows UI
            (@"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot",
            "TurnOffWindowsCopilot", 1),

            // Disable Copilot button in taskbar
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "ShowCopilotButton", 0),

            // Block Copilot in Windows Search
            (@"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            "EnableDynamicContentInWSB", 0),

            // Disable consumer features (blocks store reinstalls)
            (@"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            "DisableWindowsConsumerFeatures", 1),

            // Block Microsoft consumer experiences
            (@"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            "DisableCloudOptimizedContent", 1),

            // Prevent Copilot from being pushed via Windows Update
            (@"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            "DisableConsumerAccountStateContent", 1),
            };

            foreach (var (path, name, value) in settings)
            {
                try
                {
                    EditRegistry(path, name, value);
                    //using var key = Registry.LocalMachine.CreateSubKey(path, writable: true);
                    //key.SetValue(name, value, RegistryValueKind.DWord);
                }
                catch (Exception ex)
                {
                    // messagebox might not be ideal here since some packages may not exist on all versions, so just log to console
                    Debug.WriteLine($"[ERR]: {ex.Message}");
                }
            }
        }

        private static void BlockRecallRegistry()
        {

            var settings = new (string path, string name, int value)[]
            {
            // Disable AI/Recall snapshots
            (@"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            "DisableAIDataAnalysis", 1),

            // Disable Recall saving snapshots
            (@"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            "AllowRecallEnablement", 0),

            // Disable Bing in Search (reduces AI features re-appearing)
            (@"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            "DisableWebSearch", 1),

            // Disable cloud-based AI search suggestions
            (@"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            "ConnectedSearchUseWeb", 0),
            };

            foreach (var (path, name, value) in settings)
            {
                try
                {
                    EditRegistry(path, name, value);
                    //using var key = Registry.LocalMachine.CreateSubKey(path, writable: true);
                    //key.SetValue(name, value, RegistryValueKind.DWord);
                }
                catch (Exception ex)
                {
                    // messagebox might not be ideal here since some packages may not exist on all versions, so just log to console
                    Debug.WriteLine($"[ERR]: {ex.Message}");
                }
            }
        }

        private static void DisableAITasks()
        {

            string[] tasks =
            [
                @"\Microsoft\Windows\WindowsAI\AIXInterop",
                @"\Microsoft\Windows\WindowsAI\Recall",
                @"\Microsoft\Windows\WindowsAI\SnapshotScheduled",
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
            ];

            foreach (var task in tasks)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/Change /TN \"{task}\" /Disable",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi)!;
                    proc.WaitForExit();

                }
                catch (Exception ex)
                {
                    // messagebox might not be ideal here since some packages may not exist on all versions, so just log to console
                    Debug.WriteLine($"[ERR] {task}: {ex.Message}");
                }
            }
        }

        private static void DisableAIServices()
        {
            Console.WriteLine(">> Disabling AI/Recall services...");

            string[] services =
            [
                "AIXHost",          // Windows AI host
                "RecallService",    // Recall service
                "CopilotService",   // Copilot service
            ];

            foreach (var name in services)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "sc.exe", // sc is a built-in Windows utility for managing services
                        Arguments = $"config {name} start= disabled",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi)!;
                    proc.WaitForExit();
                }
                catch (Exception ex)
                {
                    // messagebox might not be ideal here since some services may not exist on all versions, so just log to console
                    Debug.WriteLine($"[ERR] {name}: {ex.Message}");
                }
            }
        }

        private static void BlockViaWindowsUpdate()
        {
            try
            {

                const string pathWindowsUpdate = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
                const string pathCloudContent = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";

                // Block specific upgrade targeting
                EditRegistry(pathWindowsUpdate, "DisableOSUpgrade", 1);
                EditRegistry(pathWindowsUpdate, "DontOfferThroughWUAU", 1);

                // Block AI features coming through feature updates
                EditRegistry(pathCloudContent, "DisableWindowsConsumerFeatures", 1);

            }
            catch (Exception ex)
            {
                // messagebox might not be ideal here since some services may not exist on all versions, so just log to console
                Debug.WriteLine($"[ERR]: {ex.Message}");
            }

        }

    }
}
