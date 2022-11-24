using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace FFXIV_Meteor_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string FFXIV_BOOT_VERSION = "2010.09.18.0000";
        const string FFXIV_GAME_VERSION = "2012.09.19.0001";

        public List<ServerEntry> ServerEntries = new List<ServerEntry>();
        public ServerEntry CurrentServer = new ServerEntry();
        public string InstallPath = "";

        public List<PatchDescription> PatchFilesNeeded = new List<PatchDescription>();

        public long TotalPatchDownloadSize = 0;
        public long TotalDownloadedPatchSize = 0;

        private int CurrentPatchInstalling = 0;

        public LauncherStates LauncherState = LauncherStates.Idle;
        CancellationTokenSource UpdateCancellationTokenSource = new CancellationTokenSource();

        Dictionary<string, Dictionary<string, object>> ServerThemeDefault = new Dictionary<string, Dictionary<string, object>>();
        string Account_SessionLogin = "";
        public AccountStates AccountState = AccountStates.LoggedOut;

        public enum LauncherStates
        {
            Idle,
            LoadingLauncherSettings,
            DetectingInstallation,
            LoadingServerList,
            LoadingServerTheme,
            VerifyingFiles,
            UpdateRequired,
            UpdateInProgress,
            UpdateCancelling, // Returns to UpdateRequired
            PatchInstalling,
            PatchCancelling, // Returns to UpdateRequired
            Ready,
            ConfiguringGameSettings,
            Launching
        }

        public enum AccountStates
        {
            LoggedOut,
            LoggingIn,
            LoggedIn,
            LoggingOut
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InvokeControl(Action Action)
        {
            Application.Current.Dispatcher?.Invoke(Action);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetLauncherState(LauncherStates.LoadingLauncherSettings);
            LoadLauncherSettings();

            if (InstallPath == "")
            {
                SetLauncherState(LauncherStates.DetectingInstallation);
                // Verify a game installation path is set, if none is given, exit application
                GetInstallLocation();

                if (MessageBox.Show($"The FFXIV installation has been set to:\n[ {InstallPath} ]\n\nDo you want to keep this setting?\n\nNote: This can be changed in Settings.cfg after exiting the launcher.", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    InstallPath = PromptInstallLocation();
                    if (InstallPath == "")
                    {
                        MessageBox.Show("No installation location has been selected. The launcher will now terminate.", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                }
            }

            // Check if ffxiv_patches directory is setup
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(InstallPath, "ffxiv_patches")))
            {
                MessageBoxResult CreatePatchesDir = MessageBox.Show($"The following path does not exist. Would you like to create this now?\n[{System.IO.Path.Combine(InstallPath, "ffxiv_patches")}]\n\nThis is required for the launcher to run. Selecting \"No\" will result in launcher termination.", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (CreatePatchesDir == MessageBoxResult.Yes)
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(InstallPath, "ffxiv_patches"));
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }

            // Update Version labels with launcher and client versions
            VersionLabel.Content = $"MLVersion: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            BootVersionLabel.Content = $"Boot: {GetClientVersion("boot")}";
            GameVersionLabel.Content = $"Game: {GetClientVersion("game")}";

            BackupServerThemeDefault();

            // Load Server List from Servers.xml
            SetLauncherState(LauncherStates.LoadingServerList);
            if (System.IO.File.Exists("Servers.xml"))
            {
                if (!LoadServerList())
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                MessageBox.Show("Servers.xml is missing from the launcher directory. This is required for the launcher to function.", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            ServerListComboBox.Items.Clear();
            foreach (ServerEntry server in ServerEntries)
            {
                ServerListComboBox.Items.Add(server.Name);
            }

            // Select Default Server
            // Use the last selected server from user settings if it exists in the current ServerEntries list
            if (ServerEntries.Count > 0)
            {
                if (CurrentServer.Name != "")
                {
                    CurrentServer = ServerEntries.FirstOrDefault(x => x.Name == CurrentServer.Name, ServerEntries.First());
                }
                else
                {
                    CurrentServer = ServerEntries.First();
                }

                ServerListComboBox.SelectedItem = CurrentServer.Name;
            }

            // Apply Server specific themes to the launcher if it uses any
            SetLauncherState(LauncherStates.LoadingServerTheme);
            LoadServerTheme();

            CheckGameVersionState();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveLauncherSettings();
        }

        private bool LoadLauncherSettings()
        {
            if (File.Exists("Settings.json"))
            {
                try
                {
                    var SettingsContent = System.Text.Json.JsonSerializer.Deserialize<LauncherSettingsLayout>(File.ReadAllText("Settings.json"));

                    if (File.Exists(System.IO.Path.Combine(SettingsContent.InstallLocation, "FFXIVBoot.exe")))
                    {
                        InstallPath = SettingsContent.InstallLocation;
                    }
                    else
                    {
                        MessageBox.Show("The install location provided in Settings.json was not a valid FFXIV installation.\n\nThe launcher will now attempt to automatically detect your installation.", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    
                    CurrentServer.Name = SettingsContent.DefaultServerName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading user settings from Settings.json file.\n\n{ex.Message}", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            else
            {
                // User does not have any existing settings, a file will be created for them later
                return true;
            }

            return true;
        }

        private bool SaveLauncherSettings()
        {
            LauncherSettingsLayout SettingsContent = new LauncherSettingsLayout()
            {
                InstallLocation = InstallPath,
                DefaultServerName = CurrentServer.Name
            };

            try
            {
                File.WriteAllText("Settings.json", JsonSerializer.Serialize(SettingsContent, new JsonSerializerOptions() { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing user settings to Settings.json file.\n\n{ex.Message}", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool LoadServerList()
        {
            XmlDocument xmlDocument = new XmlDocument();

            try
            {
                xmlDocument.Load("Servers.xml");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Servers.xml.\n\n{ex.Message}", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            XmlNodeList? xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/Servers/Server");

            bool HasDuplicateEntries = false;

            foreach (XmlNode node in xmlNodeList)
            {
                string ServerName = (node.Attributes["Name"] == null ? "" : node.Attributes["Name"].Value);
                string ServerAddress = (node.Attributes["Address"] == null ? "" : node.Attributes["Address"].Value);
                string LoginUrl = (node.Attributes["LoginUrl"] == null ? "" : node.Attributes["LoginUrl"].Value);
                string ThemeUrl = (node.Attributes["ThemeUrl"] == null ? "" : node.Attributes["ThemeUrl"].Value);

                ServerEntry server = new ServerEntry(ServerName, ServerAddress, LoginUrl, ThemeUrl);

                if (ServerEntries.Any(x => x.Name == ServerName))
                {
                    HasDuplicateEntries = true;
                }
                else
                {
                    ServerEntries.Add(server);
                }
            }

            if (HasDuplicateEntries)
            {
                MessageBox.Show("Duplicate server entries were found in Servers.xml. Only the first occurrence has been added to the server list.\n\nIt is strongly recommended that you remove duplicates from this file as soon as possible.", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return true;
        }
        
        private void CheckGameVersionState()
        {
            if (!IsGameUpToDate())
            {
                SetLauncherState(LauncherStates.UpdateRequired);
                BootVersionLabel.Foreground = Brushes.Red;
                GameVersionLabel.Foreground = Brushes.Red;
            }
            else
            {
                SetLauncherState(LauncherStates.Ready);
                BootVersionLabel.Foreground = Brushes.Green;
                GameVersionLabel.Foreground = Brushes.Green;
            }
        }

        private void BackupServerThemeDefault()
        {
            var BackupList = new List<string>()
            {
                "UsernameLabel", "UsernameTextBox", "PasswordLabel",
                "PasswordTextBox", "LoginBtn", "ServerListComboBox",
                "VersionLabel", "BootVersionLabel", "GameVersionLabel"
            };
            
            foreach (var Child in MainGrid.Children)
            {
                if (Child is Control && BackupList.Contains(((Control)Child).Name))
                {
                    var Ele = ((Control)Child);

                    var ControlSettings = new Dictionary<string, object>();
                    ControlSettings.Add("left", Ele.Margin.Left);
                    ControlSettings.Add("top", Ele.Margin.Top);
                    ControlSettings.Add("right", Ele.Margin.Right);
                    ControlSettings.Add("bottom", Ele.Margin.Bottom);
                    ControlSettings.Add("width", Ele.Width);
                    ControlSettings.Add("height", Ele.Height);
                    ControlSettings.Add("foreground", Ele.Foreground);
                    ControlSettings.Add("background", Ele.Background);

                    ServerThemeDefault.Add(Ele.Name, ControlSettings);
                }
            }
        }

        private void RestoreServerThemeDefault()
        {
            foreach (var Child in ServerThemeDefault)
            {
                object? GridElement = MainGrid.FindName(Child.Key);
                if (GridElement != null)
                {
                    var Ele = ((Control)GridElement);
                    Ele.Margin = new Thickness(Convert.ToDouble(Child.Value["left"]), Convert.ToDouble(Child.Value["top"]), Convert.ToDouble(Child.Value["right"]), Convert.ToDouble(Child.Value["bottom"]));
                    Ele.Width = Convert.ToDouble(Child.Value["width"]);
                    Ele.Height = Convert.ToDouble(Child.Value["height"]);
                    Ele.Foreground = (Brush)Child.Value["foreground"];
                    Ele.Background = (Brush)Child.Value["background"];
                }
            }
        }

        private string DownloadServerTheme(string ThemeUrl)
        {
            if (System.IO.File.Exists(ThemeUrl))
            {
                try
                {
                    return System.IO.File.ReadAllText(ThemeUrl);
                }
                catch (Exception)
                {
                    return "";
                }
            }
            else
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        return client.GetStringAsync(ThemeUrl).GetAwaiter().GetResult();
                    }
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        private void LoadServerTheme()
        {
            // Attempt to load an online (or offline) config file (providing one is optional)
            if (CurrentServer.ThemeUrl != "")
            {
                string ServerThemeJson = DownloadServerTheme(CurrentServer.ThemeUrl);
                if (ServerThemeJson == "")
                {
                    RestoreServerThemeDefault();
                    BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/FFXIV_1.0_Logo.png"));
                    return;
                }

                var ThemeContent = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ServerThemeJson);

                foreach (var ThemeEntry in ThemeContent)
                {
                    if (ThemeEntry.Key != "")
                    {
                        object? GridElement = MainGrid.FindName(ThemeEntry.Key);
                        if (GridElement != null)
                        {
                            if (ThemeEntry.Key == "BackgroundImage")
                            {
                                var x = ThemeEntry.Value.Deserialize<Dictionary<string, JsonElement>>();
                                if (x.ContainsKey("source"))
                                {
                                    BackgroundImage.Source = new BitmapImage(new Uri(x["source"].ToString()));
                                }
                                continue;
                            }

                            var Ele = ((Control)GridElement);
                            var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ThemeEntry.Value);
                            foreach (var setting in settings)
                            {
                                switch (setting.Key)
                                {
                                    case "left":
                                        {
                                            Ele.Margin = new Thickness(Convert.ToDouble(setting.Value.ToString()), Ele.Margin.Top, Ele.Margin.Right, Ele.Margin.Bottom);
                                            break;
                                        }
                                    case "top":
                                        {
                                            Ele.Margin = new Thickness(Ele.Margin.Left, Convert.ToDouble(setting.Value.ToString()), Ele.Margin.Right, Ele.Margin.Bottom);
                                            break;
                                        }
                                    case "right":
                                        {
                                            Ele.Margin = new Thickness(Ele.Margin.Left, Ele.Margin.Top, Convert.ToDouble(setting.Value.ToString()), Ele.Margin.Bottom);
                                            break;
                                        }
                                    case "bottom":
                                        {
                                            Ele.Margin = new Thickness(Ele.Margin.Left, Ele.Margin.Top, Ele.Margin.Right, Convert.ToDouble(setting.Value.ToString()));
                                            break;
                                        }
                                    case "width":
                                        {
                                            Ele.Width = Convert.ToDouble(setting.Value.ToString());
                                            break;
                                        }
                                    case "height":
                                        {
                                            Ele.Height = Convert.ToDouble(setting.Value.ToString());
                                            break;
                                        }
                                    case "foreground":
                                        {
                                            Ele.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(setting.Value.ToString()));
                                            break;
                                        }
                                    case "background":
                                        {
                                            Ele.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(setting.Value.ToString()));
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Reset theme to default settings
                RestoreServerThemeDefault();
                BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/FFXIV_1.0_Logo.png"));
            }
        }

        private string PromptInstallLocation()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "FFXIVBOOT.exe|ffxivboot.exe",
                Multiselect = false,
                Title = "Select the FFXIVBoot.exe in your install directory..."
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return System.IO.Path.GetDirectoryName(openFileDialog.FileName);
            }

            return "";
        }

        /// <summary>
        /// Attempts to get a registry value.
        /// </summary>
        /// <param name="KeyName"></param>
        /// <param name="ValueName"></param>
        /// <param name="DefaultValue"></param>
        /// <returns>Returns an empty string instead of null if unable to retrieve <paramref name="KeyName"/>. Returns <paramref name="DefaultValue"/> if unable to retrieve <paramref name="ValueName"/>.</returns>
        private string RegistryGetValue(string KeyName, string? ValueName, string DefaultValue)
        {
            return (string?) Registry.GetValue(KeyName, ValueName, DefaultValue) ?? "";
        }

        private void GetInstallLocation()
        {
            // Check for registry entry of game
            string? InstallLocation = "";

            // Check in original location
            string Registry_OriginalLocation = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{F2C4E6E0-EB78-4824-A212-6DF6AF0E8E82}";
            string Registry_ModernLocation = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{F2C4E6E0-EB78-4824-A212-6DF6AF0E8E82}";

            InstallLocation = RegistryGetValue(Registry_OriginalLocation, "InstallLocation", "");

            if (string.IsNullOrEmpty(InstallLocation))
            {
                // Check in 64bit location
                InstallLocation = RegistryGetValue(Registry_ModernLocation, "InstallLocation", "");
            }
            else
            {
                InstallLocation = System.IO.Path.Combine(InstallLocation, RegistryGetValue(Registry_OriginalLocation, "DisplayName", ""));
                InstallPath = InstallLocation;
                return;
            }

            if (string.IsNullOrEmpty(InstallLocation))
            {
                // Installion not found in registry, prompt user to select the location
                InstallLocation = PromptInstallLocation();
            }
            else
            {
                InstallLocation = System.IO.Path.Combine(InstallLocation, RegistryGetValue(Registry_ModernLocation, "DisplayName", ""));
            }

            InstallPath = InstallLocation;
        }

        /// <summary>
        /// Reads the client version file specified. Defaults to "game" if no value provided.
        /// </summary>
        /// <param name="VersionType">The "game" or "boot" version to check</param>
        /// <returns>Returns the contents of the version file, or an empty string if it does not exist.</returns>
        private string GetClientVersion(string VersionType = "game")
        {
            if (VersionType.ToLower() != "game" || VersionType.ToLower() != "game")
            {
                VersionType = "game";
            }

            string VersionFile = System.IO.Path.Combine(InstallPath, $"{VersionType}.ver");
            if (System.IO.File.Exists(VersionFile))
            {
                string Version = System.IO.File.ReadAllText(VersionFile).Trim();
                return Version;
            }
            else
            {
                return "";
            }
        }

        private bool IsGameUpToDate()
        {
            return GetClientVersion("game") == FFXIV_GAME_VERSION;
        }

        public void SetStatus(string Status)
        {
            StatusLabel.Content = Status;
        }

        public void SetLauncherState(LauncherStates NewLauncherState)
        {
            LauncherState = NewLauncherState;

            InvokeControl(() =>
            {
                if (LauncherState == LauncherStates.UpdateRequired)
                {
                    LaunchBtn.Content = "[ UPDATE ]";
                }
                else if (LauncherState == LauncherStates.UpdateInProgress)
                {
                    LaunchBtn.Content = "[ CANCEL UPDATE ]";
                }
                else if (LauncherState == LauncherStates.UpdateCancelling)
                {
                    LaunchBtn.Content = "[ CANCELLING UPDATE ]";
                }
                else if (LauncherState == LauncherStates.PatchInstalling)
                {
                    LaunchBtn.Content = "[ CANCEL PATCHING ]";
                }
                else if (LauncherState == LauncherStates.PatchCancelling)
                {
                    LaunchBtn.Content = "[ CANCELLING PATCH ]";
                }
                else if (LauncherState == LauncherStates.Ready)
                {
                    LaunchBtn.Content = "[ LAUNCH ]";
                }
            });
        }

        public void SetAccountState(AccountStates NewAccountState)
        {
            AccountState = NewAccountState;

            InvokeControl(() =>
            {
                if (AccountState == AccountStates.LoggedOut)
                {
                    LoginBtn.Content = "[ LOGIN ]";
                    UsernameTextBox.IsEnabled = true;
                    PasswordTextBox.IsEnabled = true;
                    LoginBtn.IsEnabled = true;
                }
                else if (AccountState == AccountStates.LoggedIn)
                {
                    LoginBtn.Content = "[ LOGOUT ]";
                    UsernameTextBox.IsEnabled = false;
                    PasswordTextBox.IsEnabled = false;
                    LoginBtn.IsEnabled = true;
                }
                else if (AccountState == AccountStates.LoggingIn)
                {
                    LoginBtn.Content = "LOGGING IN...";
                    UsernameTextBox.IsEnabled = false;
                    PasswordTextBox.IsEnabled = false;
                    LoginBtn.IsEnabled = false;
                }
                else if (AccountState == AccountStates.LoggingOut)
                {
                    LoginBtn.Content = "LOGGING OUT...";
                    UsernameTextBox.IsEnabled = false;
                    PasswordTextBox.IsEnabled = false;
                    LoginBtn.IsEnabled = false;
                }
            });
        }

        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
            {
                return "0" + suf[0];
            }

            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        private void LaunchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LauncherState == LauncherStates.UpdateInProgress)
            {
                SetLauncherState(LauncherStates.UpdateCancelling);
                UpdateCancellationTokenSource.Cancel();
            }
            else if (LauncherState == LauncherStates.UpdateRequired)
            {
                LaunchBtn.IsEnabled = false;
                SetLauncherState(LauncherStates.VerifyingFiles);
                Task.Factory.StartNew(() =>
                {
                    VerifyPatchFiles();

                    InvokeControl(() =>
                    {
                        if (PatchFilesNeeded.Count > 0)
                        {
                            long TotalDownloadSize = PatchFilesNeeded.Sum(x => x.Size);
                            TotalPatchDownloadSize = TotalDownloadSize;
                            MessageBox.Show($"The launcher will now download {BytesToString(TotalDownloadSize)} of patch data.", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);

                            ProgressChanged += OnDownloadProgressChanged;
                            UpdateCancellationTokenSource = new CancellationTokenSource();
                            CancellationToken UpdateCancellationToken = UpdateCancellationTokenSource.Token;
                            DownloadPatchFiles(PatchFilesNeeded, UpdateCancellationToken);
                            SetLauncherState(LauncherStates.UpdateInProgress);
                        }
                        LaunchBtn.IsEnabled = true;
                    });

                    UpdateCancellationTokenSource = new CancellationTokenSource();
                    CancellationToken UpdateCancellationToken = UpdateCancellationTokenSource.Token;
                    InstallPatchFiles(UpdateCancellationToken);

                    WriteUpdatedVersionFiles();

                    InvokeControl(() =>
                    {
                        SetLauncherState(LauncherStates.Ready);
                        SetStatus("Status: Ready!");
                        StatusBar.Value = 0;
                    });
                });
            }
            else if (LauncherState == LauncherStates.PatchInstalling)
            {
                SetLauncherState(LauncherStates.PatchCancelling);
                UpdateCancellationTokenSource.Cancel();
            }
            else if (LauncherState == LauncherStates.Ready)
            {
                // Verify login process has completed first
                if (AccountState != AccountStates.LoggedIn)
                {
                    MessageBox.Show("User is not logged in. Please login first.", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                AttemptGameLaunch();
            }
        }

        private void VerifyPatchFiles()
        {
            PatchFilesNeeded = new List<PatchDescription>();

            // Check if patch files need to be downloaded
            for (int i = 0; i < PatchData.PatchDataList.Count; i++)
            {
                PatchDescription PatchFile = PatchData.PatchDataList[i];

                InvokeControl(() =>
                {
                    double ProgressPercentage = Math.Round((double)(i + 1) / PatchData.PatchDataList.Count * 100, 2);

                    SetStatus($"Verifying patch file integrity... {i + 1}/{PatchData.PatchDataList.Count}");

                    StatusBar.Value = ProgressPercentage;
                });

                string FullPatchFilePath = System.IO.Path.Combine(InstallPath, "ffxiv_patches", PatchFile.Path);
                if (System.IO.File.Exists(FullPatchFilePath))
                {
                    // Exclude file from download queue if it already exists with a validated crc32
                    uint crc = Force.Crc32.Crc32Algorithm.Compute(System.IO.File.ReadAllBytes(FullPatchFilePath));

                    // Force garbage collection up to generation 3 to prevent massive memory usage and dramatically improve processing speed of Force.Crc32 by ~400%
                    GC.Collect(3);

                    if (crc == PatchFile.CRC32)
                    {
                        continue;
                    }
                }

                PatchFilesNeeded.Add(PatchFile);
            }
        }

        public void OnDownloadProgressChanged(long TotalBytesRead, long TotalDownloadSize, double ProgressPercentage, PatchDescription PatchFile)
        {
            InvokeControl(() =>
            {
                double ProgressPercentage = Math.Round((double)TotalDownloadedPatchSize / (long)TotalPatchDownloadSize * 100, 2);
                SetStatus($"Downloading patch file [{PatchFile.Path}][{BytesToString(TotalBytesRead)}/{BytesToString(TotalDownloadSize)}]... [{BytesToString(TotalDownloadedPatchSize)} / {BytesToString(TotalPatchDownloadSize)}][{ProgressPercentage}%]");
                StatusBar.Value = ProgressPercentage;

                if (TotalDownloadedPatchSize == TotalPatchDownloadSize)
                {
                    SetLauncherState(LauncherStates.PatchInstalling);
                    ProgressChanged -= OnDownloadProgressChanged;
                }
            });
        }

        public delegate void ProgressChangedHandler(long TotalBytesRead, long TotalDownloadSize, double ProgressPercentage, PatchDescription PatchFile);

        public event ProgressChangedHandler ProgressChanged;

        private void UpdateProgress(long TotalBytesRead, long? TotalDownloadSize, PatchDescription PatchFile)
        {
            if (ProgressChanged == null)
            {
                return;
            }

            double ProgressPercentage = Math.Round((double)TotalBytesRead / (long)TotalDownloadSize * 100, 2);

            ProgressChanged(TotalBytesRead, (long)TotalDownloadSize, ProgressPercentage, PatchFile);
        }

        private async Task DownloadPatchFiles(List<PatchDescription> PatchFilesNeeded, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = new HttpClient())
            {
                for (int i = 0; i < PatchFilesNeeded.Count; i++)
                {
                    PatchDescription PatchFile = PatchFilesNeeded[i];

                    var DownloadUri = new UriBuilder(PatchData.PatchDownloadUrl);
                    DownloadUri.Path = PatchFile.Path;

                    string FullPatchFilePath = System.IO.Path.Combine(InstallPath, "ffxiv_patches", PatchFile.Path);

                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FullPatchFilePath));

                    using (var Response = await client.GetAsync(DownloadUri.ToString(), HttpCompletionOption.ResponseHeadersRead))
                    {
                        using (var FileStream = new System.IO.FileStream(FullPatchFilePath, System.IO.FileMode.Create))
                        {
                            using (var Stream = await Response.Content.ReadAsStreamAsync())
                            {
                                long? TotalDownloadSize = Response.Content.Headers.ContentLength;
                                byte[] Buffer = new byte[1024^2];
                                long TotalBytesRead = 0;
                                int BytesRead = 0;
                                while((BytesRead = await Stream.ReadAsync(Buffer, 0, Buffer.Length).ConfigureAwait(false)) != 0)
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        InvokeControl(() =>
                                        {
                                            SetLauncherState(LauncherStates.UpdateRequired);
                                            SetStatus("Status: Update Canceled!");
                                            StatusBar.Value = 0;
                                        });

                                        return;
                                    }
                                    await FileStream.WriteAsync(Buffer, 0, BytesRead).ConfigureAwait(false);
                                    TotalBytesRead += BytesRead;
                                    TotalDownloadedPatchSize += BytesRead;

                                    // Report progress back to UI
                                    UpdateProgress(TotalBytesRead, TotalDownloadSize, PatchFile);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnPatchProgressChanged(string PatchFile, string EntryFile, long CurrentEntryFile, long TotalEntryFiles)
        {
            InvokeControl(() =>
            {
                string PatchName = System.IO.Path.GetFileNameWithoutExtension(PatchFile);
                string UpdatedFile = (EntryFile != "" ? System.IO.Path.GetFileName(EntryFile) : "...");
                SetStatus($"Installing {PatchName} Patch [{CurrentPatchInstalling}/{PatchData.PatchDataList.Count}]. Updating {UpdatedFile} {CurrentEntryFile} / {TotalEntryFiles}...");
                double ProgressPercentage = Math.Round((double)CurrentPatchInstalling / PatchData.PatchDataList.Count * 100, 2);
                StatusBar.Value = ProgressPercentage;
            });
        }

        private void InstallPatchFiles(CancellationToken PatchCancellationToken = default)
        {
            CurrentPatchInstalling = 0;

            Patcher.PatchProgressChanged += OnPatchProgressChanged;
            for (int i = 0; i < PatchData.PatchDataList.Count; i++)
            {
                PatchDescription PatchFile = PatchData.PatchDataList[i];

                CurrentPatchInstalling += 1;

                string FullPatchFilePath = System.IO.Path.Combine(InstallPath, "ffxiv_patches", PatchFile.Path);
                Patcher.Execute(FullPatchFilePath, InstallPath, PatchCancellationToken);

                if (PatchCancellationToken.IsCancellationRequested)
                {
                    InvokeControl(() =>
                    {
                        SetLauncherState(LauncherStates.UpdateRequired);
                        SetStatus("Status: Update Canceled!");
                        StatusBar.Value = 0;
                    });

                    return;
                }
            }
            Patcher.PatchProgressChanged -= OnPatchProgressChanged;
        }

        private void WriteUpdatedVersionFiles()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(InstallPath, $"boot.ver"), FFXIV_BOOT_VERSION);
            System.IO.File.WriteAllText(System.IO.Path.Combine(InstallPath, $"game.ver"), FFXIV_GAME_VERSION);
        }

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AccountState == AccountStates.LoggedOut)
            {
                SetAccountState(AccountStates.LoggingIn);
                Account_SessionLogin = "";
                string SessionLogin = await AttemptSessionLogin();

                if (SessionLogin.StartsWith("sessionId="))
                {
                    Account_SessionLogin = SessionLogin;
                    SetAccountState(AccountStates.LoggedIn);
                }
                else if (SessionLogin.StartsWith("Error:"))
                {
                    SetAccountState(AccountStates.LoggedOut);
                    MessageBox.Show(SessionLogin, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    SetAccountState(AccountStates.LoggedOut);
                    throw new Exception($"Unexpected response from login server.\nResponse = {SessionLogin}");
                }
            }
            else if (AccountState == AccountStates.LoggedIn)
            {
                AttemptSessionLogout();
            }
        }

        private async Task<string> AttemptSessionLogin()
        {
            string ResponseString = "";

            using (var client = new HttpClient())
            {
                if (CurrentServer.LoginUrl != "")
                {
                    // Login Versions
                    // Version 1 = Displaying PHP webpage for login and launch operations [UNSUPPORTED]
                    // Version 2 = HTTP GET Request with credentials as query parameters
                    // Version 3 = HTTP POST Request with encoded credentials in payload

                    int Login_Version = 3;

                    if (Login_Version == 2)
                    {
                        var RequestUri = new UriBuilder(CurrentServer.LoginUrl);
                        RequestUri.Query = $"username={UsernameTextBox.Text}&password={PasswordTextBox.Password}";

                        Debug.WriteLine(RequestUri.ToString());

                        var Response = await client.GetStringAsync(RequestUri.ToString());
                        {
                            Debug.WriteLine(Response);
                            ResponseString = Response;
                        }
                    }
                    else if (Login_Version == 3)
                    {
                        string LoginDataString = $"{{\"username\": \"{UsernameTextBox.Text}\", \"password\": \"{PasswordTextBox.Password}\"}}";
                        string LoginDataEscaped = Convert.ToBase64String(Encoding.UTF8.GetBytes(LoginDataString)).Replace("+", "-").Replace("/", "_");
                        var RequestString = $"{{\"authentication\": \"{LoginDataEscaped}\"}}";

                        var RequestUri = new UriBuilder(CurrentServer.LoginUrl);
                        client.BaseAddress = new Uri(RequestUri.Scheme + "://" + RequestUri.Host);

                        var Request = new HttpRequestMessage(HttpMethod.Post, RequestUri.Path);
                        Request.Content = new StringContent(RequestString, System.Text.Encoding.UTF8, "application/json");

                        var Response = await client.SendAsync(Request);
                        {
                            Debug.WriteLine(Response);
                            ResponseString = await Response.Content.ReadAsStringAsync();
                            Debug.WriteLine(ResponseString);
                        }
                    }
                }
                else
                {
                    ResponseString = "sessionId=00000000000000000000000000000000000000000000000000000000";
                }
            }

            return ResponseString;
        }

        private void AttemptSessionLogout()
        {
            SetAccountState(AccountStates.LoggingOut);
            Account_SessionLogin = "";
            SetAccountState(AccountStates.LoggedOut);
        }

        private (string LoginToken, uint TickCount) GenerateLoginToken(string SessionString)
        {
            string SessionId = SessionString.Substring(SessionString.IndexOf("=", 0) + 1);
            // Use the following SessionId for hardcoded decryption testing: "69204b8f5d7522c9127377332317ca46b41c330d1879136bb79830c3"

            if (SessionId.Length != 56)
            {
                throw new Exception($"Session length unexpected size. Expected 56 characters, got {SessionId.Length}.");
            }

            uint CurrentTickCount = (uint)(Environment.TickCount & Int32.MaxValue);
            // Use the following CurrentTickCount for hardcoded decryption testing: 30475156

            string CommandLine = $" T ={CurrentTickCount} /LANG =en-us /REGION =2 /SERVER_UTC =1356916742 /SESSION_ID ={SessionId}";

            string EncryptionKey = (CurrentTickCount & ~0xFFFF).ToString("x8");

            Blowfish blowfish = new Blowfish(EncryptionKey);

            var commandLineAsBytes = Encoding.ASCII.GetBytes(CommandLine);

            for (int i = 0; i < ((commandLineAsBytes.Length + 1) & ~0x7); i += 8)
            {
                uint left = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(CommandLine.Substring(i)));
                string sub = CommandLine.Substring(i + 4);
                uint right = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(CommandLine.Substring(i + 4)));

                var (l, r) = blowfish.Encrypt(left, right);
                Blowfish.UintByteBreaker bytes = new Blowfish.UintByteBreaker();
                bytes.UInt = l;

                commandLineAsBytes[i] = bytes.Byte1;
                commandLineAsBytes[i + 1] = bytes.Byte2;
                commandLineAsBytes[i + 2] = bytes.Byte3;
                commandLineAsBytes[i + 3] = bytes.Byte4;

                bytes.UInt = r;
                commandLineAsBytes[i + 4] = bytes.Byte1;
                commandLineAsBytes[i + 5] = bytes.Byte2;
                commandLineAsBytes[i + 6] = bytes.Byte3;
                commandLineAsBytes[i + 7] = bytes.Byte4;
            }

            // Trim the input at first null because c++'s bas64_encode terminates early at first null
            int idx = MemoryExtensions.IndexOf<byte>(commandLineAsBytes, 0);
            var trimmedCommandLineAsBytes = commandLineAsBytes.Take(idx).ToArray();

            var base64 = Convert.ToBase64String(commandLineAsBytes);
            var EscapedBase64 = base64.Replace("+", "-").Replace("/", "_");

            Debug.WriteLine(EscapedBase64);

            return (LoginToken: EscapedBase64, TickCount: CurrentTickCount);
        }

        private void AttemptGameLaunch()
        {
            string SessionId = Account_SessionLogin;

            (string SessionToken, uint CurrentTickCount) = GenerateLoginToken(SessionId);

            long AffinityMask = 0;
            if (Environment.ProcessorCount >= 16)
            {
                // Reduce launcher thread count for game to inherit same count when the process is started
                Process Proc = Process.GetCurrentProcess();
                AffinityMask = (long)Proc.ProcessorAffinity;
                AffinityMask &= 0x3FFF; // Restrict to first 14 threads (game will crash when running on 16 or more threads)
                Proc.ProcessorAffinity = (IntPtr)AffinityMask;
            }

            var startupInfo = new NativeMethods.STARTUPINFO();
            var processInfo = new NativeMethods.PROCESS_INFORMATION();

            // Game will immediately return with Exit Code 11 if CurrentTickCount is not the real system value (should only occur if debugging decryption process with hardcoded values)
            bool success = NativeMethods.CreateProcess($"{System.IO.Path.Combine(InstallPath, "ffxivgame.exe")}", $" sqex0002{SessionToken}!////", IntPtr.Zero, IntPtr.Zero, false, NativeMethods.ProcessCreationFlags.CREATE_SUSPENDED, IntPtr.Zero, InstallPath, ref startupInfo, out processInfo);

            if (!success)
            {
                throw new Exception("Failed to create process.");
            }

            if (Environment.ProcessorCount >= 16)
            {
                // Ensure game process is restricted to only 14 threads max (it should have ideally inherited from before)
                NativeMethods.SetProcessAffinityMask(processInfo.hProcess, new UIntPtr((uint)AffinityMask));
                //NativeMethods.SetThreadAffinityMask(processInfo.hThread, new UIntPtr((uint)AffinityMask));
            }

            string Hostname = Dns.GetHostAddresses(CurrentServer.Address).FirstOrDefault(x => x.ToString().Contains(".")).ToString();

            bool patchSuccess = MemoryPatcher.ApplyPatches(Process.GetProcessById((int)processInfo.dwProcessId), processInfo.hThread, Hostname, CurrentTickCount);
            if (!patchSuccess)
            {
                throw new Exception("Failed to apply patches");
            }

            NativeMethods.ResumeThread(processInfo.hThread);
            NativeMethods.CloseHandle(processInfo.hProcess);
            NativeMethods.CloseHandle(processInfo.hThread);

            Debug.WriteLine("Process Launch Completed");
        }

        private void ServerListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountState == AccountStates.LoggedIn)
            {
                AttemptSessionLogout();
            }
            
            CurrentServer = ServerEntries[ServerListComboBox.SelectedIndex];
            LoadServerTheme();

            CheckGameVersionState();
        }
    }
}
