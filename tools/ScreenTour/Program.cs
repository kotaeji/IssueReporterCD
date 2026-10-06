using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using IssueReporterCD;
using IssueReporterCD.Collectors;
using IssueReporterCD.Reporting;
using IssueReporterCD.Settings;
using IssueReporterCD.ViewModels;
using IssueReporterCD.Views;

namespace ScreenTour
{
    /// <summary>
    /// Opens every IssueReporterCD window and dropdown with demo data and saves each as a PNG,
    /// so the UI can be reviewed without a Windows PC. Usage: ScreenTour.exe [output folder]
    /// Rendering uses RenderTargetBitmap (window content only, no title bar), which also works
    /// on CI machines without an interactive desktop.
    /// </summary>
    internal static class Program
    {
        private static readonly List<string[]> Captured = new List<string[]>();
        private static string _outDir;

        [STAThread]
        private static int Main(string[] args)
        {
            _outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "ui-screenshots");
            Directory.CreateDirectory(_outDir);
            try
            {
                Run();
                WriteIndex();
                Console.WriteLine("Captured " + Captured.Count + " screens to " + _outDir);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                WriteIndex();
                return 1;
            }
        }

        private static void Run()
        {
            // Resolve "/Assets/..." pack URIs against IssueReporterCD, not this exe.
            Application.ResourceAssembly = typeof(App).Assembly;
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();

            DemoData demo = DemoData.Create();
            var settingsService = new SettingsService(demo.Defaults, Path.Combine(demo.Root, "settings.ini"));
            ReporterSettings settings = settingsService.Load();
            ReportSession session = ReportSession.Create();
            DemoData.WritePlaceholderScreenshot(Path.Combine(session.ScreenshotDir, "screen_at_launch.png"));

            var viewModel = new MainViewModel(
                session,
                settings,
                s => CollectorFactory.Create(s, session.ScreenshotDir, null),
                SymptomTemplate.LoadAll(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SymptomTemplates")),
                new FakeDialogs(),
                new UserPrefs { EquipmentId = "AUR-0123" });

            var main = new MainWindow { DataContext = viewModel };
            main.Show();
            WaitUntil(() => !viewModel.IsCollecting && viewModel.Items.All(i => i.Status != CollectionStatus.Pending && i.Status != CollectionStatus.Running), 30000);
            Capture(main, "01_main_empty", "메인 화면 - 실행 직후 (데이터 수집 완료, 입력 전)");

            viewModel.GenerateCommand.Execute(null);
            Pump(300);
            Capture(main, "02_main_validation", "메인 화면 - 필수 항목을 비운 채 리포트 생성을 눌렀을 때");

            FillSample(viewModel);
            Capture(main, "03_main_filled", "메인 화면 - 입력 완료 (증상 유형 '알람 발생' 템플릿 사용)");

            int index = 4;
            foreach (ComboBox combo in FindAll<ComboBox>(main))
            {
                string field = BoundField(combo);
                combo.IsDropDownOpen = true;
                Pump(500);
                Capture(main, string.Format("{0:00}_dropdown_{1}", index++, field), "드롭다운 펼침 - " + DropdownLabel(field), combo);
                combo.IsDropDownOpen = false;
                Pump(200);
            }

            viewModel.GenerateCommand.Execute(null);
            WaitUntil(() => viewModel.OutputPath != null || (viewModel.StatusMessage ?? string.Empty).Contains("못했습니다"), 60000);
            Pump(300);
            Capture(main, string.Format("{0:00}_main_generated", index++), "메인 화면 - 리포트 생성 완료");

            var settingsViewModel = new SettingsViewModel(settings, _ => null);
            var settingsWindow = new SettingsWindow(settingsViewModel);
            settingsWindow.Show();
            Pump(300);
            Capture(settingsWindow, string.Format("{0:00}_settings", index++), "설정 창");

            settingsViewModel.OutputDir.Value = @"D:\IssueReports";
            settingsViewModel.LogMaxAgeDays = "abc";
            SettingsEdits ignored;
            settingsViewModel.TryGetEdits(out ignored);
            Pump(300);
            Capture(settingsWindow, string.Format("{0:00}_settings_error", index++), "설정 창 - 저장 폴더를 바꾸고(사용자 지정 표시) 잘못된 값을 넣었을 때");
            settingsWindow.Close();

            CaptureLogGap(string.Format("{0:00}_log_gap_old", index++), "로그 시각 경고 - 최근 로그가 오래됨",
                LogFreshness.Check(demo.OldLogsDir, 24, DateTime.Now).Message);
            CaptureLogGap(string.Format("{0:00}_log_gap_missing", index++), "로그 시각 경고 - 로그 폴더 없음",
                LogFreshness.Check(Path.Combine(demo.Root, "no_such_folder"), 24, DateTime.Now).Message);

            main.Close();
            session.CleanupIfReported();
        }

        private static void FillSample(MainViewModel vm)
        {
            vm.Site = "Fab-3";
            vm.Line = "L2";
            vm.Author = "홍길동";
            vm.Title = "Stop 버튼을 눌러도 Stage가 정지하지 않음";
            vm.SelectedTemplate = vm.Templates.FirstOrDefault(t => t.Name == "알람 발생") ?? vm.Templates.Last();
            vm.Symptom = "알람 코드: E-2041\n알람 메시지: Interlock timeout\n발생 모듈/위치: Stage X축\n"
                + "발생 직전 작업: Auto 운전 중 Stop 버튼 누름\n알람 해제(Reset) 후 재발 여부: 예";
            vm.Expected = "Stop 버튼을 누르면 1초 안에 모든 축이 감속 정지해야 함";
            vm.ReproSteps = "1. Auto 운전 시작\n2. Stage 이동 중 Stop 버튼 누름\n3. 10번 중 3번 정도 정지하지 않고 E-2041 발생";
            vm.ActionsTaken = "비상정지 후 Aurora 재시작으로 복구";
            vm.Frequency = Frequency.Intermittent;
            vm.Reproducible = Reproducible.Site;
            vm.OperatingMode = OperatingMode.Auto;
            vm.LifecyclePhase = LifecyclePhase.Stop;
            vm.AlarmCodes = "E-2041, W-0107";
            vm.IsSeverityHigh = true;
            Pump(300);
        }

        private static void CaptureLogGap(string name, string description, string message)
        {
            var dialog = new LogGapDialog(message);
            dialog.Show();
            Pump(300);
            Capture(dialog, name, description);
            dialog.Close();
        }

        private static void Capture(Window window, string name, string description, ComboBox openCombo = null)
        {
            Pump(200);
            var root = (FrameworkElement)window.Content;
            double width = root.ActualWidth;
            double height = root.ActualHeight;
            BitmapSource content = Render(root, width, height);

            BitmapSource popupImage = null;
            Rect popupRect = Rect.Empty;
            if (openCombo != null)
            {
                var popup = openCombo.Template.FindName("PART_Popup", openCombo) as Popup;
                var child = popup != null ? popup.Child as FrameworkElement : null;
                if (child != null && child.ActualWidth > 0)
                {
                    Point origin = openCombo.TranslatePoint(new Point(0, openCombo.ActualHeight), root);
                    popupRect = new Rect(origin, new Size(child.ActualWidth, child.ActualHeight));
                    popupImage = Render(child, child.ActualWidth, child.ActualHeight);
                    width = Math.Max(width, popupRect.Right);
                    height = Math.Max(height, popupRect.Bottom);
                }
            }

            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(window.Background ?? Brushes.White, null, new Rect(0, 0, width, height));
                dc.DrawImage(content, new Rect(0, 0, root.ActualWidth, root.ActualHeight));
                if (popupImage != null)
                {
                    dc.DrawImage(popupImage, popupRect);
                }
            }

            string file = name + ".png";
            Save(Render(visual, width, height), Path.Combine(_outDir, file));
            Captured.Add(new[] { file, description, string.Format("{0:0}x{1:0}", width, height) });
            Console.WriteLine("  " + file);
        }

        private static BitmapSource Render(Visual visual, double width, double height)
        {
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            return bitmap;
        }

        private static void Save(BitmapSource bitmap, string path)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }
        }

        private static string BoundField(ComboBox combo)
        {
            Binding binding = BindingOperations.GetBinding(combo, Selector.SelectedValueProperty)
                ?? BindingOperations.GetBinding(combo, Selector.SelectedItemProperty);
            return binding != null ? binding.Path.Path : "combo";
        }

        private static string DropdownLabel(string field)
        {
            switch (field)
            {
                case "Frequency": return "빈도";
                case "OperatingMode": return "운전 모드";
                case "LifecyclePhase": return "동작 단계";
                case "Reproducible": return "재현 여부";
                case "SelectedTemplate": return "증상 유형";
                default: return field;
            }
        }

        private static IEnumerable<T> FindAll<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var match = child as T;
                if (match != null)
                {
                    yield return match;
                }
                foreach (T descendant in FindAll<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private static void Pump(int milliseconds)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        private static void WaitUntil(Func<bool> condition, int timeoutMilliseconds)
        {
            var watch = Stopwatch.StartNew();
            while (!condition() && watch.ElapsedMilliseconds < timeoutMilliseconds)
            {
                Pump(100);
            }
        }

        private static void WriteIndex()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# IssueReporterCD UI screenshots");
            sb.AppendLine();
            sb.AppendLine("- Commit: " + (Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local"));
            sb.AppendLine("- Captured: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            sb.AppendLine("- Primary screen: " + SystemParameters.PrimaryScreenWidth + "x" + SystemParameters.PrimaryScreenHeight);
            sb.AppendLine("- Window content only (no title bar). Demo data, not a real equipment PC.");
            sb.AppendLine();
            sb.AppendLine("| File | Screen | Size |");
            sb.AppendLine("| --- | --- | --- |");
            foreach (string[] row in Captured)
            {
                sb.AppendLine("| [" + row[0] + "](" + row[0] + ") | " + row[1] + " | " + row[2] + " |");
            }
            sb.AppendLine();
            sb.AppendLine("Not captured (Windows system dialogs): template overwrite confirmation, folder picker, settings save error, unexpected error.");
            File.WriteAllText(Path.Combine(_outDir, "README.md"), sb.ToString(), new UTF8Encoding(false));
        }
    }

    internal sealed class FakeDialogs : IDialogService
    {
        public ReporterSettings ShowSettings(ReporterSettings current)
        {
            return null;
        }

        public LogGapChoice AskLogGap(string message)
        {
            return LogGapChoice.Continue;
        }

        public bool Confirm(string message)
        {
            return true;
        }
    }

    /// <summary>Folders and files that look like an Aurora installation.</summary>
    internal sealed class DemoData
    {
        public string Root { get; private set; }

        public string OldLogsDir { get; private set; }

        public NameValueCollection Defaults { get; private set; }

        public static DemoData Create()
        {
            string root = Path.Combine(Path.GetTempPath(), "IssueReporterCD-ScreenTour");
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            string logs = Write(root, @"Aurora\Logs\Aurora_20261006.log", "09:12:01.120 [INFO] Run started\n09:15:44.902 [ERROR] E-2041 Interlock timeout\n");
            Write(root, @"Aurora\Logs\Motion_20261006.log", "09:15:44.880 [WARN] Stage X decel timeout\n");
            Write(root, @"Aurora\SysError\SysError_20261006.log", "09:15:44.903 E-2041 StageX Interlock timeout\n");
            Write(root, @"Aurora\Config\Machine.xml", "<Machine Model=\"AUR-X200\" />\n");
            Write(root, @"Aurora\Config\IO\IoMap.csv", "Name,Address\nStopButton,X1.0\n");
            string oldLog = Write(root, @"OldLogs\Aurora_20260901.log", "old\n");
            File.SetLastWriteTime(oldLog, DateTime.Now.AddDays(-35));
            Write(root, @"Aurora\environment.ini",
                "platform_version=3.3.0.0\nmachine_sw_version=1.4.2\napi_level=12\nbuild_config=Release\narchitecture=x64\n"
                + "equipment_model=AUR-X200\ndata_path=D:\\AuroraData\nsequence_id=MainSequence\nsequence_revision=57\n"
                + "recipe_id=Default\nrecipe_revision=12\nmodule.WMX3=3.4.1\nmodule.MIL=10.60\n");

            return new DemoData
            {
                Root = root,
                OldLogsDir = Path.Combine(root, "OldLogs"),
                Defaults = new NameValueCollection
                {
                    { "AuroraDiscoveryFile", Path.Combine(root, @"Aurora\IssueReporter.ini") },
                    { "AuroraEnvironmentFile", Path.Combine(root, @"Aurora\environment.ini") },
                    { "AuroraLogDir", Path.GetDirectoryName(logs) },
                    { "AuroraSysErrorDir", Path.Combine(root, @"Aurora\SysError") },
                    { "AuroraConfigDir", Path.Combine(root, @"Aurora\Config") },
                    { "LogMaxAgeDays", "3" },
                    { "LogGapWarningHours", "24" },
                    { "OutputDir", Path.Combine(root, "IssueReports") },
                },
            };
        }

        public static void WritePlaceholderScreenshot(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.DimGray, null, new Rect(0, 0, 320, 180));
            }
            var bitmap = new RenderTargetBitmap(320, 180, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }
        }

        private static string Write(string root, string relativePath, string content)
        {
            string path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(true));
            return path;
        }
    }
}
