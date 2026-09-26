using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IssueReporterCD.Collectors;
using IssueReporterCD.Infrastructure;
using IssueReporterCD.Reporting;
using IssueReporterCD.Settings;

namespace IssueReporterCD.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly ReportSession _session;
        private readonly Func<ReporterSettings, IList<ICollector>> _createCollectors;
        private readonly IDialogService _dialogs;
        private readonly UserPrefs _prefs;
        private ReporterSettings _settings;
        private Task _collectionTask;

        private string _equipmentId;
        private string _site;
        private string _author;
        private string _occurredAt;
        private SymptomTemplate _selectedTemplate;
        private string _symptom;
        private string _reproSteps;
        private string _actionsTaken;
        private Severity _severity = Severity.Medium;
        private bool _isCollecting;
        private string _validationMessage;
        private string _statusMessage;
        private string _outputPath;

        public MainViewModel(
            ReportSession session,
            ReporterSettings settings,
            Func<ReporterSettings, IList<ICollector>> createCollectors,
            IReadOnlyList<SymptomTemplate> templates,
            IDialogService dialogs,
            UserPrefs prefs)
        {
            _session = session;
            _settings = settings;
            _createCollectors = createCollectors;
            _dialogs = dialogs;
            _prefs = prefs;

            Items = new ObservableCollection<CollectionItemViewModel>();
            ResetItems();

            Templates = templates;
            _selectedTemplate = templates.FirstOrDefault();

            _equipmentId = string.IsNullOrWhiteSpace(prefs.EquipmentId) ? Environment.MachineName : prefs.EquipmentId;
            _site = prefs.Site;
            _author = prefs.Author;
            _occurredAt = session.StartedAt.ToString("yyyy-MM-dd HH:mm");

            GenerateCommand = new AsyncRelayCommand(GenerateAsync);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, HasOutput);
            CopyOutputPathCommand = new RelayCommand(CopyOutputPath, HasOutput);
        }

        public ObservableCollection<CollectionItemViewModel> Items { get; }

        public IReadOnlyList<SymptomTemplate> Templates { get; }

        public ICommand GenerateCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand OpenOutputFolderCommand { get; }

        public ICommand CopyOutputPathCommand { get; }

        public string EquipmentId
        {
            get { return _equipmentId; }
            set { if (SetProperty(ref _equipmentId, value)) RevalidateIfShown(); }
        }

        public string Site
        {
            get { return _site; }
            set { if (SetProperty(ref _site, value)) RevalidateIfShown(); }
        }

        public string Author
        {
            get { return _author; }
            set { if (SetProperty(ref _author, value)) RevalidateIfShown(); }
        }

        public string OccurredAt
        {
            get { return _occurredAt; }
            set { if (SetProperty(ref _occurredAt, value)) RevalidateIfShown(); }
        }

        /// <summary>
        /// Selecting a template fills the symptom box with its prompts. Text the engineer already
        /// typed is only replaced after confirmation; the symptom type changes either way.
        /// </summary>
        public SymptomTemplate SelectedTemplate
        {
            get { return _selectedTemplate; }
            set
            {
                SymptomTemplate previous = _selectedTemplate;
                if (value == null || !SetProperty(ref _selectedTemplate, value))
                {
                    return;
                }

                bool untouched = string.IsNullOrWhiteSpace(Symptom)
                    || (previous != null && Symptom.Trim() == previous.Body.Trim());
                if (untouched || _dialogs.Confirm("작성한 증상 내용을 '" + value.Name + "' 템플릿으로 바꿀까요?"))
                {
                    Symptom = value.Body;
                }
            }
        }

        public string Symptom
        {
            get { return _symptom; }
            set { if (SetProperty(ref _symptom, value)) RevalidateIfShown(); }
        }

        public string ReproSteps
        {
            get { return _reproSteps; }
            set { SetProperty(ref _reproSteps, value); }
        }

        public string ActionsTaken
        {
            get { return _actionsTaken; }
            set { SetProperty(ref _actionsTaken, value); }
        }

        public bool IsSeverityHigh
        {
            get { return _severity == Severity.High; }
            set { if (value) SetSeverity(Severity.High); }
        }

        public bool IsSeverityMedium
        {
            get { return _severity == Severity.Medium; }
            set { if (value) SetSeverity(Severity.Medium); }
        }

        public bool IsSeverityLow
        {
            get { return _severity == Severity.Low; }
            set { if (value) SetSeverity(Severity.Low); }
        }

        public bool IsCollecting
        {
            get { return _isCollecting; }
            private set { SetProperty(ref _isCollecting, value); }
        }

        public string ValidationMessage
        {
            get { return _validationMessage; }
            private set { SetProperty(ref _validationMessage, value); }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            private set { SetProperty(ref _statusMessage, value); }
        }

        public string OutputPath
        {
            get { return _outputPath; }
            private set { SetProperty(ref _outputPath, value); }
        }

        /// <summary>
        /// Starts collecting in the background. Safe to call more than once.
        /// </summary>
        public void StartCollection()
        {
            if (_collectionTask == null)
            {
                _collectionTask = RunCollectorsAsync();
            }
        }

        private void ApplySettings(ReporterSettings settings)
        {
            _settings = settings;
            Task previous = _collectionTask ?? Task.FromResult(0);
            _collectionTask = RecollectAfterAsync(previous);
        }

        private async Task RecollectAfterAsync(Task previous)
        {
            try
            {
                await previous;
            }
            catch (Exception)
            {
                // Per-item failures are already shown; start over with the new settings.
            }

            ResetItems();
            await RunCollectorsAsync();
            StatusMessage = "변경된 설정으로 데이터를 다시 수집했습니다.";
        }

        private void ResetItems()
        {
            Items.Clear();
            foreach (ICollector collector in _createCollectors(_settings))
            {
                Items.Add(new CollectionItemViewModel(collector));
            }
        }

        private async Task RunCollectorsAsync()
        {
            IsCollecting = true;
            foreach (var item in Items.ToList())
            {
                item.Status = CollectionStatus.Running;
                item.Detail = "수집 중";
                try
                {
                    CollectorResult result = await item.Collector.CollectAsync(_session.WorkDir, CancellationToken.None);
                    item.Status = result.Status;
                    item.Detail = result.Detail;
                }
                catch (Exception ex)
                {
                    item.Status = CollectionStatus.Failed;
                    item.Detail = ex.Message;
                }
            }
            IsCollecting = false;
        }

        private async Task GenerateAsync()
        {
            List<string> missing = GetMissingRequiredFields();
            if (missing.Count > 0)
            {
                ValidationMessage = "필수 항목을 입력하세요: " + string.Join(", ", missing);
                return;
            }
            ValidationMessage = null;

            try
            {
                StartCollection();
                if (!_collectionTask.IsCompleted)
                {
                    StatusMessage = "데이터 수집이 끝나기를 기다리는 중...";
                    await _collectionTask;
                }

                StatusMessage = "로그 시각을 확인하는 중...";
                string logDir = _settings.LogDir.Value;
                int warningHours = _settings.LogGapWarningHours;
                LogFreshnessResult logCheck = await Task.Run(() => LogFreshness.Check(logDir, warningHours, DateTime.Now));

                bool acknowledged = false;
                if (logCheck.IsSuspicious)
                {
                    LogGapChoice choice = _dialogs.AskLogGap(logCheck.Message);
                    if (choice == LogGapChoice.OpenSettings)
                    {
                        StatusMessage = "설정을 확인한 뒤 리포트 생성을 다시 누르세요.";
                        OpenSettings();
                        return;
                    }
                    if (choice == LogGapChoice.Cancel)
                    {
                        StatusMessage = "리포트 생성을 취소했습니다.";
                        return;
                    }
                    acknowledged = true;
                }

                StatusMessage = "압축 파일을 만드는 중... (최고 압축률이라 시간이 걸릴 수 있습니다)";
                IssueReport report = BuildReport(logCheck.NewestWriteTime, acknowledged);
                string outputDir = _settings.OutputDir.Value;
                OutputPath = await Task.Run(() => ReportPackager.Package(_session, report, outputDir));
                _session.HasReport = true;
                StatusMessage = "리포트를 만들었습니다. 아래 파일을 한국 담당자에게 보내주세요.";
                SavePrefs();
            }
            catch (Exception ex)
            {
                StatusMessage = "리포트를 만들지 못했습니다: " + ex.Message;
            }
        }

        private IssueReport BuildReport(DateTime? newestLogTime, bool logGapAcknowledged)
        {
            return new IssueReport
            {
                CreatedAt = DateTime.Now,
                ReporterVersion = typeof(MainViewModel).Assembly.GetName().Version.ToString(),
                EquipmentId = EquipmentId.Trim(),
                Site = Site.Trim(),
                Author = Author.Trim(),
                OccurredAt = OccurredAt.Trim(),
                Severity = _severity,
                SymptomType = SelectedTemplate != null ? SelectedTemplate.Name : SymptomTemplate.FreeFormName,
                Symptom = Symptom,
                ReproSteps = ReproSteps,
                ActionsTaken = ActionsTaken,
                NewestLogTime = newestLogTime,
                LogGapWarningAcknowledged = logGapAcknowledged,
                Collections = Items.Select(i => new CollectionSummary(i.Name, i.Status, i.Detail)).ToList(),
            };
        }

        private List<string> GetMissingRequiredFields()
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(EquipmentId)) missing.Add("장비 ID");
            if (string.IsNullOrWhiteSpace(Site)) missing.Add("사이트");
            if (string.IsNullOrWhiteSpace(Author)) missing.Add("작성자");
            if (string.IsNullOrWhiteSpace(OccurredAt)) missing.Add("발생 시각");
            if (string.IsNullOrWhiteSpace(Symptom)) missing.Add("증상");
            return missing;
        }

        private void RevalidateIfShown()
        {
            if (ValidationMessage == null)
            {
                return;
            }

            List<string> missing = GetMissingRequiredFields();
            ValidationMessage = missing.Count > 0 ? "필수 항목을 입력하세요: " + string.Join(", ", missing) : null;
        }

        private void SetSeverity(Severity severity)
        {
            if (_severity == severity)
            {
                return;
            }

            _severity = severity;
            OnPropertyChanged(nameof(IsSeverityHigh));
            OnPropertyChanged(nameof(IsSeverityMedium));
            OnPropertyChanged(nameof(IsSeverityLow));
        }

        private void OpenSettings()
        {
            ReporterSettings updated = _dialogs.ShowSettings(_settings);
            if (updated != null)
            {
                StatusMessage = "변경된 설정으로 데이터를 다시 수집하는 중...";
                ApplySettings(updated);
            }
        }

        private void SavePrefs()
        {
            _prefs.EquipmentId = EquipmentId.Trim();
            _prefs.Site = Site.Trim();
            _prefs.Author = Author.Trim();
            _prefs.Save();
        }

        private bool HasOutput()
        {
            return !string.IsNullOrEmpty(OutputPath);
        }

        private void OpenOutputFolder()
        {
            Process.Start("explorer.exe", "/select,\"" + OutputPath + "\"");
        }

        private void CopyOutputPath()
        {
            Clipboard.SetText(OutputPath);
            StatusMessage = "파일 경로를 복사했습니다.";
        }
    }
}
