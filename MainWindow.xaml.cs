using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Windows.Controls;
using System.IO.Compression;
using BeamNGLauncher.Pages;

namespace BeamNGLauncher
{
    public partial class MainWindow : Window
    {
        public class ModItem
        {
            public string FileName { get; set; }
            public string FullPath { get; set; }
        }

        private readonly ObservableCollection<ModItem> Mods = new ObservableCollection<ModItem>();
        private readonly ObservableCollection<string> ModPngEntries = new ObservableCollection<string>();
        private string BeamNGExePath;

        private string StartupIniPath;
        private string IniUserPathRaw;
        private string ResolvedUserPath;

        private bool _uiReady = false;

        private LaunchPage LaunchPageView => LaunchPageControl;
        private ModsPage ModsPageView => ModsPageControl;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LocateBeamNGExe_FromSteamLibraries();
            LoadStartupIni_UserPath();

            HookLaunchPageEvents();
            HookModsPageEvents();

            LoadMods();
            LoadLevelsAndVehiclesFromGameContent();

            _uiReady = true;
            RefreshArgsPreview();

            SetActivePage("Launch");
        }

        private void HookComboBoxTextChanged(ComboBox cb)
        {
            if (cb == null) return;
            cb.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(AnyOptionTextChanged));
        }

        private void HookLaunchPageEvents()
        {
            var page = LaunchPageView;
            if (page == null) return;

            page.GfxCombo.SelectionChanged += AnyOptionSelectionChanged;
            page.HeadlessCheck.Checked += AnyOptionChanged;
            page.HeadlessCheck.Unchecked += AnyOptionChanged;
            page.LevelCombo.SelectionChanged += AnyOptionSelectionChanged;
            page.VehicleCombo.SelectionChanged += AnyOptionSelectionChanged;
            page.ConsoleCheck.Checked += AnyOptionChanged;
            page.ConsoleCheck.Unchecked += AnyOptionChanged;
            page.CefDevCheck.Checked += AnyOptionChanged;
            page.CefDevCheck.Unchecked += AnyOptionChanged;
            page.CrashDefaultOption.Checked += AnyOptionChanged;
            page.CrashFullOption.Checked += AnyOptionChanged;
            page.CrashNoneOption.Checked += AnyOptionChanged;
            page.LuaStdinCheck.Checked += AnyOptionChanged;
            page.LuaStdinCheck.Unchecked += AnyOptionChanged;
            page.LuaDebugCheck.Checked += AnyOptionChanged;
            page.LuaDebugCheck.Unchecked += AnyOptionChanged;
            page.TcomCheck.Checked += AnyOptionChanged;
            page.TcomCheck.Unchecked += AnyOptionChanged;
            page.TcomDebugCheck.Checked += AnyOptionChanged;
            page.TcomDebugCheck.Unchecked += AnyOptionChanged;

            page.LuaChunkInput.TextChanged += AnyOptionTextChanged;
            page.ExecInput.TextChanged += AnyOptionTextChanged;
            page.TportInput.TextChanged += AnyOptionTextChanged;
            page.TcomListenIpInput.TextChanged += AnyOptionTextChanged;
            page.ExtraArgsInput.TextChanged += AnyOptionTextChanged;

            HookComboBoxTextChanged(page.LevelCombo);
            HookComboBoxTextChanged(page.VehicleCombo);

            page.LaunchButtonControl.Click += Launch_Click;
        }

        private void HookModsPageEvents()
        {
            var page = ModsPageView;
            if (page == null) return;

            page.ModsList.SelectionChanged += ModsList_SelectionChanged;
            page.ModsList.DragEnter += ModsList_DragEnter;
            page.ModsList.Drop += ModsList_Drop;
            page.RefreshModsButtonControl.Click += RefreshMods_Click;
            page.InstallZipButtonControl.Click += InstallZip_Click;
            page.OpenModsFolderButtonControl.Click += OpenModsFolder_Click;

            page.ModsList.ItemsSource = Mods;
            page.ModPngList.ItemsSource = ModPngEntries;
        }

        // ====== 统一刷新预览 ======
        private void AnyOptionChanged(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;
            RefreshArgsPreview();
        }

        private void AnyOptionTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_uiReady) return;
            RefreshArgsPreview();
        }

        private void AnyOptionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_uiReady) return;
            RefreshArgsPreview();
        }

        private void RefreshArgsPreview()
        {
            if (LaunchArgsTextBox == null) return;
            LaunchArgsTextBox.Text = BuildArgumentsFromUI();
        }

        // =========================
        // 从安装目录 content\levels / content\vehicles 读取 zip
        // =========================
        private void LoadLevelsAndVehiclesFromGameContent()
        {
            if (string.IsNullOrWhiteSpace(BeamNGExePath) || !File.Exists(BeamNGExePath))
                return;

            string gameDir = Path.GetDirectoryName(BeamNGExePath);
            if (string.IsNullOrWhiteSpace(gameDir) || !Directory.Exists(gameDir))
                return;

            string levelsDir = Path.Combine(gameDir, "content", "levels");
            string vehiclesDir = Path.Combine(gameDir, "content", "vehicles");

            var page = LaunchPageView;
            if (page == null) return;

            FillLevelComboFromContentDir(page.LevelCombo, levelsDir);
            FillVehicleComboFromContentDir(page.VehicleCombo, vehiclesDir);
        }

        // levels：优先解析 zip 内 levels/<name>/...；兜底：zip 文件名(去扩展名)
        private void FillLevelComboFromContentDir(ComboBox combo, string dir)
        {
            if (combo == null) return;

            combo.Items.Clear();
            combo.Items.Add("(不指定)");

            if (!Directory.Exists(dir))
            {
                combo.SelectedIndex = 0;
                return;
            }

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 兜底：把包名(去扩展名)加进去（zip/7z/pak 都能显示）
            var allPackages = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                                       .Where(f =>
                                       {
                                           var ext = Path.GetExtension(f).ToLowerInvariant();
                                           return ext == ".zip" || ext == ".7z" || ext == ".pak";
                                       })
                                       .ToList();

            foreach (var p in allPackages)
            {
                var name = Path.GetFileNameWithoutExtension(p);
                if (!string.IsNullOrWhiteSpace(name))
                    set.Add(name);
            }

            // 增强：只对 zip 解析内部结构
            foreach (var zipPath in allPackages.Where(p => Path.GetExtension(p).Equals(".zip", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    using (var fs = File.OpenRead(zipPath))
                    using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        foreach (var entry in za.Entries)
                        {
                            var full = (entry.FullName ?? "").Replace('\\', '/');
                            if (!full.StartsWith("levels/", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var parts = full.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var levelName = parts[1];
                                if (!string.IsNullOrWhiteSpace(levelName))
                                    set.Add(levelName);
                            }
                        }
                    }
                }
                catch
                {
                    // zip 读失败：忽略，保留兜底文件名
                }
            }

            foreach (var name in set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                combo.Items.Add(name);

            combo.SelectedIndex = 0;
        }

        // vehicles：优先解析 zip 内 vehicles/<name>/...；兜底：zip 文件名(去扩展名)
        private void FillVehicleComboFromContentDir(ComboBox combo, string dir)
        {
            if (combo == null) return;

            combo.Items.Clear();
            combo.Items.Add("(不指定)");

            if (!Directory.Exists(dir))
            {
                combo.SelectedIndex = 0;
                return;
            }

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var allPackages = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                                       .Where(f =>
                                       {
                                           var ext = Path.GetExtension(f).ToLowerInvariant();
                                           return ext == ".zip" || ext == ".7z" || ext == ".pak";
                                       })
                                       .ToList();

            foreach (var p in allPackages)
            {
                var name = Path.GetFileNameWithoutExtension(p);
                if (!string.IsNullOrWhiteSpace(name))
                    set.Add(name);
            }

            foreach (var zipPath in allPackages.Where(p => Path.GetExtension(p).Equals(".zip", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    using (var fs = File.OpenRead(zipPath))
                    using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        foreach (var entry in za.Entries)
                        {
                            var full = (entry.FullName ?? "").Replace('\\', '/');
                            if (!full.StartsWith("vehicles/", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var parts = full.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var vehName = parts[1];
                                if (!string.IsNullOrWhiteSpace(vehName))
                                    set.Add(vehName);
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            foreach (var name in set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                combo.Items.Add(name);

            combo.SelectedIndex = 0;
        }

        // ====== 拼接启动参数（包含 level/vehicle 下拉） ======
        private string BuildArgumentsFromUI()
        {
            var args = new List<string>();
            var page = LaunchPageView;
            if (page == null) return string.Empty;

            // -gfx
            var selected = page.GfxCombo != null ? (page.GfxCombo.SelectedItem as ComboBoxItem) : null;
            var gfxTag = selected != null ? (selected.Tag as string) : "default";
            //if (string.IsNullOrEmpty(generic: gfxTag)) gfxTag = "default";

            if (gfxTag == "dx11") args.Add("-gfx dx11");
            else if (gfxTag == "vk") args.Add("-gfx vk");
            else if (gfxTag == "null") args.Add("-gfx null");

            // switches
            if (page.ConsoleCheck != null && page.ConsoleCheck.IsChecked == true) args.Add("-console");
            if (page.CefDevCheck != null && page.CefDevCheck.IsChecked == true) args.Add("-cefdev");
            if (page.HeadlessCheck != null && page.HeadlessCheck.IsChecked == true) args.Add("-headless");
            if (page.LuaStdinCheck != null && page.LuaStdinCheck.IsChecked == true) args.Add("-luastdin");
            if (page.LuaDebugCheck != null && page.LuaDebugCheck.IsChecked == true) args.Add("-luadebug");

            // crash report
            if (page.CrashFullOption != null && page.CrashFullOption.IsChecked == true) args.Add("-fullcrashreport");
            else if (page.CrashNoneOption != null && page.CrashNoneOption.IsChecked == true) args.Add("-nocrashreport");

            // level / vehicle：用 ComboBox.Text（支持选中+手打）
            var level = page.LevelCombo != null ? (page.LevelCombo.Text ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(level) && level != "(不指定)")
                args.Add("-level " + QuoteIfNeeded(level));

            var vehicle = page.VehicleCombo != null ? (page.VehicleCombo.Text ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(vehicle) && vehicle != "(不指定)")
                args.Add("-vehicle " + QuoteIfNeeded(vehicle));

            // lua / exec
            var lua = page.LuaChunkInput != null ? (page.LuaChunkInput.Text ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(lua))
                args.Add("-lua " + QuoteIfNeeded(lua));

            var exec = page.ExecInput != null ? (page.ExecInput.Text ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(exec))
                args.Add("-exec " + QuoteIfNeeded(exec));

            // tcom
            if (page.TcomCheck != null && page.TcomCheck.IsChecked == true)
            {
                args.Add("-tcom");

                var portText = page.TportInput != null ? (page.TportInput.Text ?? "").Trim() : "";
                if (!string.IsNullOrEmpty(portText))
                    args.Add("-tport " + portText);

                var ip = page.TcomListenIpInput != null ? (page.TcomListenIpInput.Text ?? "").Trim() : "";
                if (!string.IsNullOrEmpty(ip))
                    args.Add("-tcom-listen-ip " + QuoteIfNeeded(ip));

                if (page.TcomDebugCheck != null && page.TcomDebugCheck.IsChecked == true)
                    args.Add("-tcom-debug");
            }

            // 额外参数
            var extra = page.ExtraArgsInput != null ? (page.ExtraArgsInput.Text ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(extra))
                args.Add(extra);

            // startup.ini userpath（保留你原逻辑）
            if (!string.IsNullOrWhiteSpace(ResolvedUserPath))
            {
                if (ResolvedUserPath == @".\")
                    args.Add("-nouserpath");
                else
                    args.Add("-userpath " + QuoteIfNeeded(ResolvedUserPath));
            }

            return string.Join(" ", args);
        }

        private string QuoteIfNeeded(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Contains(" ") || s.Contains("\t") || s.Contains(";"))
            {
                s = s.Replace("\"", "\\\"");
                return "\"" + s + "\"";
            }
            return s;
        }

        // ====== 启动 ======
        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(BeamNGExePath) || !File.Exists(BeamNGExePath))
            {
                MessageBox.Show("BeamNG.drive.exe 未找到");
                return;
            }

            // tport 校验
            var page = LaunchPageView;
            if (page != null && page.TcomCheck != null && page.TcomCheck.IsChecked == true)
            {
                var portText = page.TportInput != null ? (page.TportInput.Text ?? "").Trim() : "";
                if (!string.IsNullOrEmpty(portText))
                {
                    int port;
                    if (!int.TryParse(portText, out port) || port < 1 || port > 65535)
                    {
                        MessageBox.Show("tport 无效：请输入 1~65535 的端口号");
                        return;
                    }
                }
            }

            string args = BuildArgumentsFromUI();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = BeamNGExePath,
                    WorkingDirectory = Path.GetDirectoryName(BeamNGExePath),
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message);
            }
        }

        // =========================
        // SteamLibrary 查找 BeamNG.drive.exe
        // =========================
        private void LocateBeamNGExe_FromSteamLibraries()
        {
            BeamNGExePath = null;

            var libraries = GetSteamLibraries();
            foreach (var libRoot in libraries)
            {
                string exe = Path.Combine(libRoot, @"steamapps\common\BeamNG.drive\BeamNG.drive.exe");
                if (File.Exists(exe))
                {
                    BeamNGExePath = exe;

                    string ver = GetProductVersion(exe);
                    if (VersionText != null) VersionText.Text = $"BeamNG.drive | Version {ver}";
                    if (PathText != null) PathText.Text = $"Path: {exe}";
                    return;
                }
            }

            if (VersionText != null) VersionText.Text = "BeamNG.drive | Version -";
            if (PathText != null) PathText.Text = "Path: 未找到 BeamNG.drive.exe";
        }

        private List<string> GetSteamLibraries()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string steamPath = TryGetSteamPathFromRegistry();
            if (!string.IsNullOrWhiteSpace(steamPath) && Directory.Exists(steamPath))
            {
                set.Add(NormalizePath(steamPath));

                string vdf = Path.Combine(steamPath, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdf))
                {
                    foreach (var p in ParseLibraryFoldersVdf(vdf))
                        if (Directory.Exists(p)) set.Add(NormalizePath(p));
                }
            }

            string[] fallbackSteamRoots =
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
                @"D:\Steam",
                @"E:\Steam"
            };

            foreach (var root in fallbackSteamRoots)
            {
                if (!Directory.Exists(root)) continue;
                set.Add(NormalizePath(root));

                string vdf = Path.Combine(root, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdf))
                {
                    foreach (var p in ParseLibraryFoldersVdf(vdf))
                        if (Directory.Exists(p)) set.Add(NormalizePath(p));
                }
            }

            return set.ToList();
        }

        private string TryGetSteamPathFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    var v = key != null ? (key.GetValue("SteamPath") as string) : null;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            catch { }

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    var v = key != null ? (key.GetValue("InstallPath") as string) : null;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            catch { }

            return null;
        }

        private List<string> ParseLibraryFoldersVdf(string vdfPath)
        {
            var result = new List<string>();
            string text = File.ReadAllText(vdfPath);

            foreach (Match m in Regex.Matches(text, "\"path\"\\s*\"([^\"]+)\""))
            {
                var p = UnescapeVdfPath(m.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(p)) result.Add(p);
            }

            foreach (Match m in Regex.Matches(text, "\"\\d+\"\\s*\"([^\"]+)\""))
            {
                var p = UnescapeVdfPath(m.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(p)) result.Add(p);
            }

            return result;
        }

        private string UnescapeVdfPath(string v) { return v.Replace(@"\\", @"\"); }

        private string NormalizePath(string p)
        {
            try { return Path.GetFullPath(p.Trim().TrimEnd('\\')); }
            catch { return p.Trim().TrimEnd('\\'); }
        }

        private string GetProductVersion(string exePath)
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(exePath);
                if (!string.IsNullOrEmpty(info.ProductVersion))
                    return info.ProductVersion;
                return info.FileVersion ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        // =========================
        // startup.ini -> UserPath
        // =========================
        private void LoadStartupIni_UserPath()
        {
            IniUserPathRaw = null;
            ResolvedUserPath = null;
            StartupIniPath = null;

            string ini1 = null;
            if (!string.IsNullOrEmpty(BeamNGExePath))
            {
                var gameDir = Path.GetDirectoryName(BeamNGExePath);
                ini1 = Path.Combine(gameDir ?? "", "startup.ini");
            }

            string ini2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.ini");

            if (ini1 != null && File.Exists(ini1)) StartupIniPath = ini1;
            else if (File.Exists(ini2)) StartupIniPath = ini2;

            if (StartupIniPath == null) return;

            try
            {
                string[] lines = File.ReadAllLines(StartupIniPath);
                bool inFilesystem = false;

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith(";")) continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        inFilesystem = line.Equals("[filesystem]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (!inFilesystem) continue;

                    if (line.StartsWith("UserPath", StringComparison.OrdinalIgnoreCase))
                    {
                        var idx = line.IndexOf('=');
                        if (idx >= 0)
                        {
                            IniUserPathRaw = line.Substring(idx + 1).Trim();
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(IniUserPathRaw))
                    return;

                if (IniUserPathRaw == @".\" || IniUserPathRaw == "." || IniUserPathRaw == @"./")
                {
                    ResolvedUserPath = @".\";
                    return;
                }

                if (Path.IsPathRooted(IniUserPathRaw))
                {
                    ResolvedUserPath = IniUserPathRaw;
                }
                else
                {
                    var iniDir = Path.GetDirectoryName(StartupIniPath);
                    ResolvedUserPath = Path.GetFullPath(Path.Combine(iniDir ?? "", IniUserPathRaw));
                }
            }
            catch
            {
            }
        }

        // =========================
        // mods（用户目录 current\mods）
        // =========================
        private void LoadMods()
        {
            Mods.Clear();

            string modsDir = GetModsDirectory();

            if (ModsPageView != null && ModsPageView.ModsPathText != null)
                ModsPageView.ModsPathText.Text = $"Mods 路径：{modsDir}";

            if (!Directory.Exists(modsDir))
            {
                ResetSelectedModDetails();
                return;
            }

            foreach (var zip in Directory.GetFiles(modsDir, "*.zip"))
            {
                Mods.Add(new ModItem
                {
                    FileName = Path.GetFileName(zip),
                    FullPath = zip
                });
            }

            ResetSelectedModDetails();
        }

        private string GetModsDirectory()
        {
            if (!string.IsNullOrWhiteSpace(ResolvedUserPath))
            {
                if (ResolvedUserPath == @".\")
                {
                    var gameDir = Path.GetDirectoryName(BeamNGExePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                    return Path.Combine(gameDir, "mods");
                }

                return Path.Combine(ResolvedUserPath, "mods");
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"BeamNG\BeamNG.drive\current\mods"
            );
        }

        private void ResetSelectedModDetails()
        {
            if (ModsPageView != null)
            {
                if (ModsPageView.SelectedModNameText != null) ModsPageView.SelectedModNameText.Text = "未选择 Mod";
                if (ModsPageView.SelectedModSizeText != null) ModsPageView.SelectedModSizeText.Text = "大小：-";
                if (ModsPageView.SelectedModVehicleText != null) ModsPageView.SelectedModVehicleText.Text = "识别的车辆目录：-";
                if (ModsPageView.SelectedModPngCountText != null) ModsPageView.SelectedModPngCountText.Text = "PNG 数量：-";
            }
            ModPngEntries.Clear();
        }

        private void ModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ModsPageView != null ? ModsPageView.ModsList.SelectedItem as ModItem : null;
            UpdateSelectedModDetails(item);
        }

        private void UpdateSelectedModDetails(ModItem item)
        {
            ModPngEntries.Clear();

            if (item == null)
            {
                ResetSelectedModDetails();
                return;
            }

            var fileInfo = new FileInfo(item.FullPath);
            if (ModsPageView != null)
            {
                if (ModsPageView.SelectedModNameText != null) ModsPageView.SelectedModNameText.Text = item.FileName;
                if (ModsPageView.SelectedModSizeText != null) ModsPageView.SelectedModSizeText.Text = $"大小：{FormatFileSize(fileInfo.Length)}";
            }

            var vehicleRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pngEntries = new List<string>();

            try
            {
                using (var fs = File.OpenRead(item.FullPath))
                using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (var entry in za.Entries)
                    {
                        var full = (entry.FullName ?? "").Replace('\\', '/');

                        if (full.StartsWith("vehicles/", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = full.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                                vehicleRoots.Add(parts[1]);
                        }

                        if (full.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            pngEntries.Add(full);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取 Mod 失败：" + ex.Message);
            }

            foreach (var png in pngEntries.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                ModPngEntries.Add(png);

            if (ModsPageView != null)
            {
                if (ModsPageView.SelectedModVehicleText != null)
                    ModsPageView.SelectedModVehicleText.Text = vehicleRoots.Count > 0
                        ? $"识别的车辆目录：{string.Join(\", \", vehicleRoots.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}"
                        : "识别的车辆目录：-";

                if (ModsPageView.SelectedModPngCountText != null)
                    ModsPageView.SelectedModPngCountText.Text = $"PNG 数量：{ModPngEntries.Count}";
            }
        }

        private string FormatFileSize(long bytes)
        {
            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return $"{size:0.##} {units[unitIndex]}";
        }

        private void RefreshMods_Click(object sender, RoutedEventArgs e)
        {
            LoadMods();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageKey)
            {
                SetActivePage(pageKey);
            }
        }

        private void SetActivePage(string pageKey)
        {
            if (LaunchPageControl == null || ModsPageControl == null || Page3Control == null || Page4Control == null)
                return;

            LaunchPageControl.Visibility = pageKey == "Launch" ? Visibility.Visible : Visibility.Collapsed;
            ModsPageControl.Visibility = pageKey == "Mods" ? Visibility.Visible : Visibility.Collapsed;
            Page3Control.Visibility = pageKey == "Page3" ? Visibility.Visible : Visibility.Collapsed;
            Page4Control.Visibility = pageKey == "Page4" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ModsList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ModsList_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
                return;

            InstallZipFiles(files);
        }

        private void InstallZip_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Zip Files (*.zip)|*.zip",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                InstallZipFiles(dialog.FileNames);
            }
        }

        private void InstallZipFiles(IEnumerable<string> files)
        {
            var zipFiles = files.Where(f => string.Equals(Path.GetExtension(f), ".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            if (!zipFiles.Any())
            {
                MessageBox.Show("未检测到 .zip 文件。");
                return;
            }

            string modsDir = GetModsDirectory();
            Directory.CreateDirectory(modsDir);

            foreach (var file in zipFiles)
            {
                try
                {
                    string destPath = Path.Combine(modsDir, Path.GetFileName(file));
                    destPath = GetAvailableFilePath(destPath);

                    var sourceFull = Path.GetFullPath(file);
                    var destFull = Path.GetFullPath(destPath);
                    if (string.Equals(sourceFull, destFull, StringComparison.OrdinalIgnoreCase))
                        continue;

                    File.Copy(file, destPath, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"安装失败：{Path.GetFileName(file)}\n{ex.Message}");
                }
            }

            LoadMods();
        }

        private string GetAvailableFilePath(string targetPath)
        {
            if (!File.Exists(targetPath))
                return targetPath;

            string dir = Path.GetDirectoryName(targetPath) ?? "";
            string fileName = Path.GetFileNameWithoutExtension(targetPath);
            string ext = Path.GetExtension(targetPath);

            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{fileName} ({i}){ext}");
                i++;
            } while (File.Exists(candidate));

            return candidate;
        }

        private void OpenModsFolder_Click(object sender, RoutedEventArgs e)
        {
            string modsDir = GetModsDirectory();
            if (!Directory.Exists(modsDir))
                Directory.CreateDirectory(modsDir);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = modsDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开目录失败：" + ex.Message);
            }
        }

        // =========================
        // 标题栏拖动/窗口按钮
        // =========================
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }
        private void Minimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
    }
}
