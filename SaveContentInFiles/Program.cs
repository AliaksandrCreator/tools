using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using Common;

class Program
{
    static void Main()
    {
        if (!File.Exists(Configuration.FullCollectedPath))
        {
            Console.WriteLine("Файл не найден: " + Configuration.FullCollectedPath);
            return;
        }

        var rawJson = File.ReadAllText(Configuration.FullCollectedPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var elements = JsonSerializer.Deserialize<List<JsonElement>>(rawJson, options);

        int jsonApplied = 0;
        int taskHtmlCreated = 0;
        int hintHtmlCreated = 0;

        foreach (var el in elements)
        {
            if (!el.TryGetProperty("path", out var pathProp)) continue;

            string relativePath = pathProp.GetString();
            string fullPath = Path.Combine(Configuration.CourseFolder, relativePath);
            string fileName = Path.GetFileName(fullPath);

            // HTML-файл
            if (el.TryGetProperty("content", out var contentProp))
            {
                string targetName = fileName.Replace(".en.html", $".{Configuration.TargetLang}.html");
                string targetPath = Path.Combine(Path.GetDirectoryName(fullPath), targetName);

                File.WriteAllText(targetPath, contentProp.GetString());

                if (fileName == "task.en.html") taskHtmlCreated++;
                if (fileName == "hint.en.html") hintHtmlCreated++;
                continue;
            }

            // JSON-файл
            if (!File.Exists(fullPath)) continue;

            var originalText = File.ReadAllText(fullPath);
            var original = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(originalText, options);
            var updated = new Dictionary<string, object>();

            foreach (var kvp in original)
            {
                string key = kvp.Key;
                var value = kvp.Value;

                if (IsTranslationField(key) && value.ValueKind == JsonValueKind.Object)
                {
                    var subDict = JsonSerializer.Deserialize<Dictionary<string, string>>(value.GetRawText());

                    if (subDict.ContainsKey("en") && !subDict.ContainsKey(Configuration.TargetLang))
                        subDict[Configuration.TargetLang] = subDict["en"] ?? "";

                    updated[key] = subDict;
                }
                else
                {
                    updated[key] = value.Deserialize<object>();
                }
            }

            var saveOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            File.WriteAllText(fullPath, JsonSerializer.Serialize(updated, saveOptions));
            jsonApplied++;
        }

        var reportJsonApply = new Dictionary<string, object>
        {
            ["комментарий"] = "Отчёт по применённым JSON-переводам",
            ["JSON_файлов_обновлено"] = jsonApplied
        };

        var reportHtmlApply = new Dictionary<string, object>
        {
            ["комментарий"] = "Отчёт по применённым HTML-переводам",
            [$"task_{Configuration.TargetLang}_html_вставлено"] = taskHtmlCreated,
            [$"hint_{Configuration.TargetLang}_html_вставлено"] = hintHtmlCreated
        };

        var saveOptionsFinal = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        File.AppendAllText(Configuration.FullReportPath, "\n\n" + JsonSerializer.Serialize(reportJsonApply, saveOptionsFinal));
        File.AppendAllText(Configuration.FullReportPath, "\n\n" + JsonSerializer.Serialize(reportHtmlApply, saveOptionsFinal));
        Console.WriteLine($"Отчёт дописан: {Configuration.FullReportPath}");
    }

    static bool IsTranslationField(string key)
    {
        foreach (var field in FileTargets.TranslationFields)
        {
            if (field == key)
                return true;
        }
        return false;
    }
}



