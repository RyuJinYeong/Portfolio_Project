using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class SteamTestBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows &&
            report.summary.platform != BuildTarget.StandaloneWindows64)
            return;

        string source = Path.GetFullPath("steam_appid.txt");
        if (File.Exists(source))
            File.Copy(source, Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "steam_appid.txt"), true);
    }
}
