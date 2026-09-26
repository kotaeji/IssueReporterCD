using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace IssueReporterCD.Collectors
{
    public static class ScreenCapture
    {
        /// <summary>
        /// Saves a PNG covering every monitor.
        /// </summary>
        public static void CaptureVirtualScreen(string path)
        {
            Rectangle bounds = SystemInformation.VirtualScreen;
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                bitmap.Save(path, ImageFormat.Png);
            }
        }
    }
}
