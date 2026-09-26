using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IssueReporterCD.Collectors
{
    /// <summary>
    /// Reports on the screenshot taken at launch (see App.OnStartup).
    /// </summary>
    public sealed class ScreenshotCollector : ICollector
    {
        private readonly string _screenshotDir;
        private readonly string _captureError;

        public ScreenshotCollector(string screenshotDir, string captureError)
        {
            _screenshotDir = screenshotDir;
            _captureError = captureError;
        }

        public string Name
        {
            get { return "스크린샷"; }
        }

        public Task<CollectorResult> CollectAsync(string workDir, CancellationToken cancellationToken)
        {
            if (_captureError != null)
            {
                return Task.FromResult(CollectorResult.Failed(_captureError));
            }

            int count = Directory.Exists(_screenshotDir)
                ? Directory.GetFiles(_screenshotDir, "*.png").Length
                : 0;

            return Task.FromResult(count > 0
                ? CollectorResult.Done(count + "장")
                : CollectorResult.Warning("캡처된 화면 없음"));
        }
    }
}
