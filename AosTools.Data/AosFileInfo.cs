using System.IO;
using System.Reflection;

namespace AosTools.Data {

    /// <summary>
    /// Information about currently processed file.
    /// </summary>
    public class AosFileInfo {

        public readonly string AosToolsVersion;

        public string InputPath { get; private set; }
        public string OutputPath { get; private set; }

        public string FileName { get; private set; }
        public string FileExtension { get; private set; }
        public string FileNameNoExt { get; private set; }

        public Mode FileMode { get; private set; }

        public AosFileInfo(string inputPath, string outputPath, Mode fileMode) {
            AosToolsVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            InputPath = inputPath;
            OutputPath = outputPath;
            FileMode = fileMode;

            if (fileMode == Mode.File) {
                FileName = Path.GetFileName(InputPath);
                FileExtension = Path.GetExtension(InputPath);
                FileNameNoExt = Path.GetFileNameWithoutExtension(InputPath);
            }
        }

    }

    /// <summary>
    /// Current file mode.
    /// </summary>
    public enum Mode {
        File,
        Directory
    }

}