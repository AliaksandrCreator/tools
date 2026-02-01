using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using Common;

class Program
{
    public class HtmlData
    {
        public string path { get; set; }
        public string content { get; set; }
    }

    public class JsonTranslationData
    {
        public string path { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string preRequirements { get; set; }
    }

    public class TranslationStats
    {
        public int courseInfoTotal = 0;
        public int unitInfoTotal = 0;
        public int taskHtmlTotal = 0;
        public int hintHtmlTotal = 0;

        public void Register(string fileName)
        {
            switch (fileName)
            {
                case "CourseInfo.json": courseInfoTotal++; break;
                case "UnitInfo.json": unitInfoTotal++; break;
                case "task.en.html": taskHtmlTotal++; break;
                case "hint.en.html": hintHtmlTotal++; break;
            }
        }

        public object ToReport()
        {
            return new
            {
                комментарий = "Отчёт по обработанным файлам",
                CourseInfo = new { всего = courseInfoTotal },
                UnitInfo = new { всего = unitInfoTotal },
                Html = new
                {
                    task_en_html = taskHtmlTotal,
                    hint_en_html = hintHtmlTotal
                }
            };
        }
    }

    static void Main()
    {
        if (!Directory.Exists(Configuration.CourseFolder))
        {
            Console.WriteLine("Папка не найдена: " + Configuration.CourseFolder);
            return;
        }

        var result = new List<object>();
        var stats = new TranslationStats();

        foreach (var file in Directory.EnumerateFiles(Configuration.CourseFolder, "*.*", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(file);

            if (!FileTargets.JsonAndHtml.Contains(fileName))
                continue;

            if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                stats.Register(fileName);
                result.Add(new HtmlData
                {
                    path = Path.GetRelativePath(Configuration.CourseFolder, file),
                    content = File.ReadAllText(file)
                });
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    var root = doc.RootElement;

                    var data = new JsonTranslationData
                    {
                        path = Path.GetRelativePath(Configuration.CourseFolder, file)
                    };

                    bool hasContent = false;

                    foreach (var field in FileTargets.TranslationFields)
                    {
                        if (root.TryGetProperty(field, out var prop) && prop.ValueKind == JsonValueKind.Object)
                        {
                            if (prop.TryGetProperty("en", out var enProp))
                            {
                                string value = enProp.GetString();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    switch (field)
                                    {
                                        case "name": data.name = value; break;
                                        case "description": data.description = value; break;
                                        case "preRequirements": data.preRequirements = value; break;
                                    }
                                    hasContent = true;
                                }
                            }
                        }
                    }

                    if (hasContent)
                    {
                        stats.Register(fileName);
                        result.Add(data);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке {file}: {ex.Message}");
                }
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        File.WriteAllText(Configuration.FullCollectedPath, JsonSerializer.Serialize(result, options));
        Console.WriteLine($"Готово. Результат сохранён в {Configuration.FullCollectedPath}");

        File.WriteAllText(Configuration.FullReportPath, JsonSerializer.Serialize(stats.ToReport(), options));
        Console.WriteLine($"Отчёт сохранён в {Configuration.FullReportPath}");
    }
}




