using IssueReporterCD.Collectors;
using IssueReporterCD.Infrastructure;

namespace IssueReporterCD.ViewModels
{
    public sealed class CollectionItemViewModel : ObservableObject
    {
        private CollectionStatus _status = CollectionStatus.Pending;
        private string _detail = "대기";

        public CollectionItemViewModel(ICollector collector)
        {
            Collector = collector;
        }

        public ICollector Collector { get; }

        public string Name
        {
            get { return Collector.Name; }
        }

        public CollectionStatus Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        public string Detail
        {
            get { return _detail; }
            set { SetProperty(ref _detail, value); }
        }
    }
}
