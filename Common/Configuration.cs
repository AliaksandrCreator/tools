namespace Common
{
    public static class Configuration
    {
        public const string CourseFolder = @"D:\SidTasks";
        public const string TargetLang = "fr";
        public const string CollectedPath = "collected_for_translation.json";
        public const string ReportPath = "translation_extract_report.json";

        public static string FullCollectedPath
        {
            get { return Path.Combine(CourseFolder, CollectedPath); }
        }

        public static string FullReportPath
        {
            get { return Path.Combine(CourseFolder, ReportPath); }
        }
    }

    public static class FileTargets
    {
        public static readonly HashSet<string> JsonAndHtml = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CourseInfo.json",
            "UnitInfo.json",
            "task.en.html",
            "hint.en.html"
        };

        public static readonly string[] TranslationFields =
        {
            "name",
            "description",
            "preRequirements"
        };
    }
}



