using System;
using System.Windows.Input;
using IssueReporterCD.Infrastructure;
using IssueReporterCD.Settings;

namespace IssueReporterCD.ViewModels
{
    public sealed class PathFieldViewModel : ObservableObject
    {
        private readonly Func<string, string> _pickFolder;
        private string _value;

        public PathFieldViewModel(string label, PathSetting setting, Func<string, string> pickFolder)
        {
            Label = label;
            AutoValue = setting.AutoValue;
            AutoSource = setting.AutoSource;
            _value = setting.Value;
            _pickFolder = pickFolder;
            BrowseCommand = new RelayCommand(Browse);
            ResetCommand = new RelayCommand(() => Value = AutoValue, () => !IsAuto);
        }

        public string Label { get; }

        public string AutoValue { get; }

        public SettingSource AutoSource { get; }

        public ICommand BrowseCommand { get; }

        public ICommand ResetCommand { get; }

        public string Value
        {
            get { return _value; }
            set
            {
                if (SetProperty(ref _value, value))
                {
                    OnPropertyChanged(nameof(IsAuto));
                    OnPropertyChanged(nameof(SourceText));
                }
            }
        }

        public bool IsAuto
        {
            get { return SettingsService.SamePath(Value, AutoValue); }
        }

        public string SourceText
        {
            get
            {
                if (!IsAuto)
                {
                    return "사용자 지정 (자동값: " + AutoValue + ")";
                }
                return AutoSource == SettingSource.Aurora ? "자동: Aurora 제공 경로" : "자동: 기본값";
            }
        }

        private void Browse()
        {
            string picked = _pickFolder(Value);
            if (!string.IsNullOrEmpty(picked))
            {
                Value = picked;
            }
        }
    }

    public sealed class SettingsViewModel : ObservableObject
    {
        private string _logMaxAgeDays;
        private string _logGapWarningHours;
        private string _errorMessage;

        public SettingsViewModel(ReporterSettings current, Func<string, string> pickFolder)
        {
            LogDir = new PathFieldViewModel("로그 폴더", current.LogDir, pickFolder);
            SysErrorDir = new PathFieldViewModel("sys-error 폴더", current.SysErrorDir, pickFolder);
            ConfigDir = new PathFieldViewModel("설정(config) 폴더", current.ConfigDir, pickFolder);
            OutputDir = new PathFieldViewModel("리포트 저장 폴더", current.OutputDir, pickFolder);
            _logMaxAgeDays = current.LogMaxAgeDays.ToString();
            _logGapWarningHours = current.LogGapWarningHours.ToString();
            StorageInfo = "사용자 설정 파일: " + current.UserSettingsFile
                + "\nAurora 경로 파일: " + (current.AuroraDiscoveryFile ?? "(지정 안 됨)");
        }

        public PathFieldViewModel LogDir { get; }

        public PathFieldViewModel SysErrorDir { get; }

        public PathFieldViewModel ConfigDir { get; }

        public PathFieldViewModel OutputDir { get; }

        public string StorageInfo { get; }

        public string LogMaxAgeDays
        {
            get { return _logMaxAgeDays; }
            set { SetProperty(ref _logMaxAgeDays, value); }
        }

        public string LogGapWarningHours
        {
            get { return _logGapWarningHours; }
            set { SetProperty(ref _logGapWarningHours, value); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public bool TryGetEdits(out SettingsEdits edits)
        {
            edits = null;
            int days;
            int hours;
            if (!int.TryParse(LogMaxAgeDays, out days) || days < 0)
            {
                ErrorMessage = "로그 수집 기간은 0 이상의 정수로 입력하세요.";
                return false;
            }
            if (!int.TryParse(LogGapWarningHours, out hours) || hours < 0)
            {
                ErrorMessage = "로그 시간 차이 경고 기준은 0 이상의 정수로 입력하세요.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(OutputDir.Value))
            {
                ErrorMessage = "리포트 저장 폴더를 입력하세요.";
                return false;
            }

            ErrorMessage = null;
            edits = new SettingsEdits
            {
                LogDir = LogDir.Value,
                SysErrorDir = SysErrorDir.Value,
                ConfigDir = ConfigDir.Value,
                OutputDir = OutputDir.Value,
                LogMaxAgeDays = days,
                LogGapWarningHours = hours,
            };
            return true;
        }
    }
}
