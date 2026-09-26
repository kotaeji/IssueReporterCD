using System.Threading;
using System.Threading.Tasks;

namespace IssueReporterCD.Collectors
{
    public enum CollectionStatus
    {
        Pending,
        Running,
        Done,
        Warning,
        Failed,
    }

    public sealed class CollectorResult
    {
        private CollectorResult(CollectionStatus status, string detail)
        {
            Status = status;
            Detail = detail;
        }

        public CollectionStatus Status { get; }

        public string Detail { get; }

        public static CollectorResult Done(string detail)
        {
            return new CollectorResult(CollectionStatus.Done, detail);
        }

        public static CollectorResult Warning(string detail)
        {
            return new CollectorResult(CollectionStatus.Warning, detail);
        }

        public static CollectorResult Failed(string detail)
        {
            return new CollectorResult(CollectionStatus.Failed, detail);
        }
    }

    /// <summary>
    /// Gathers one kind of data into the report's working folder.
    /// </summary>
    public interface ICollector
    {
        string Name { get; }

        Task<CollectorResult> CollectAsync(string workDir, CancellationToken cancellationToken);
    }
}
