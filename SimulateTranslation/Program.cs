using System;                      // базовые типы
using System.Collections.Generic; // коллекции
using System.IO;                  // работа с файлами
using System.Text.Json;           // JSON-сериализация
using System.Text.Encodings.Web;  // отключение экранирования Unicode

// модель для JSON-файлов
class JsonTranslationData
{
    public string path { get; set; }               // путь к JSON-файлу
    public string id { get; set; }                 // ID
    public string name_en { get; set; }            // имя
    public string description_en { get; set; }     // описание
    public string preRequirements_en { get; set; } // требования
}

// модель для HTML-файлов
class HtmlData
{
    public string path { get; set; }     // путь к HTML-файлу
    public string content { get; set; }  // содержимое HTML
}

class Program
{
    static void Main()
    {
        // корневая папка и язык перевода
        string courseFolder = @"D:\SidTasks";
        string targetLang = "fr";

        // путь к входному файлу
        string inputPath = Path.Combine(courseFolder, "collected_for_translation.json");

        // проверка наличия входного файла
        if (!File.Exists(inputPath))
        {
            Console.WriteLine("Файл не найден: " + inputPath);
            return;
        }

        // читаем JSON как список универсальных элементов
        var rawJson = File.ReadAllText(inputPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var elements = JsonSerializer.Deserialize<List<JsonElement>>(rawJson, options);

        // инициализация результатов и счётчиков
        var simulatedJson = new List<Dictionary<string, object>>();
        int courseInfoTranslated = 0;
        int unitInfoTranslated = 0;
        HashSet<string> translatedIds = new HashSet<string>();
        int taskHtmlCount = 0;
        int hintHtmlCount = 0;

        // обработка каждого элемента из входного файла
        foreach (var el in elements)
        {
            // обработка JSON-объекта
            if (el.TryGetProperty("name_en", out _) || el.TryGetProperty("description_en", out _))
            {
                var item = JsonSerializer.Deserialize<JsonTranslationData>(el.GetRawText(), options);
                var sim = new Dictionary<string, object>();

                // копируем базовые поля
                sim["path"] = item.path;
                sim["id"] = item.id;

                bool hasContent = false;

                // симулируем перевод по каждому полю
                var fields = new[] { "name", "description", "preRequirements" };

                foreach (var field in fields)
                {
                    string enField = field + "_en";
                    string langField = field + "_" + targetLang;

                    string enValue = typeof(JsonTranslationData)
                        .GetProperty(enField)
                        ?.GetValue(item) as string;

                    sim[enField] = enValue;
                    sim[langField] = enValue;

                    if (!string.IsNullOrWhiteSpace(enValue))
                        hasContent = true;
                }

                // добавляем в результат и считаем только если были данные
                if (hasContent)
                {
                    simulatedJson.Add(sim);

                    string fileName = Path.GetFileName(item.path);
                    if (fileName == "CourseInfo.json") courseInfoTranslated++;
                    if (fileName == "UnitInfo.json") unitInfoTranslated++;
                    if (!string.IsNullOrEmpty(item.id)) translatedIds.Add(item.id);
                }
            }

            // обработка HTML-объекта
            else if (el.TryGetProperty("content", out var contentProp) && el.TryGetProperty("path", out var pathProp))
            {
                string relativePath = pathProp.GetString();
                string originalPath = Path.Combine(courseFolder, relativePath);
                string fileName = Path.GetFileName(originalPath);
                string targetName = fileName.Replace(".en.html", $".{targetLang}.html");
                string targetPath = Path.Combine(Path.GetDirectoryName(originalPath), targetName);

                // создаём симулированный HTML-файл
                File.WriteAllText(targetPath, contentProp.GetString());

                // фиксируем в симулированном JSON
                simulatedJson.Add(new Dictionary<string, object>
                {
                    ["path"] = relativePath,
                    ["content"] = contentProp.GetString()
                });

                // увеличиваем счётчики только после успешной записи
                if (fileName.Equals("task.en.html", StringComparison.OrdinalIgnoreCase)) taskHtmlCount++;
                if (fileName.Equals("hint.en.html", StringComparison.OrdinalIgnoreCase)) hintHtmlCount++;
            }
        }

        // сохраняем симулированный JSON
        string simulatedPath = Path.Combine(courseFolder, "simulated_translation.json");
        var saveOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(simulatedPath, JsonSerializer.Serialize(simulatedJson, saveOptions));
        Console.WriteLine($"Симуляция завершена: {simulatedPath}");

        // формируем отчёт по JSON
        string reportPath = Path.Combine(courseFolder, "translation_extract_report.json");
        var reportJson = new Dictionary<string, object>
        {
            ["комментарий"] = "Отчёт по симулированным переводам",
            ["CourseInfo_fr"] = courseInfoTranslated,
            ["UnitInfo_fr"] = unitInfoTranslated,
            ["Уникальных_ID_переведено"] = translatedIds.Count,
            ["Всего_файлов_переведено"] = courseInfoTranslated + unitInfoTranslated
        };

        // формируем отчёт по HTML
        var reportHtml = new Dictionary<string, object>
        {
            ["комментарий"] = "Отчёт по симулированным HTML-файлам",
            [$"task_{targetLang}_html_создано"] = taskHtmlCount,
            [$"hint_{targetLang}_html_создано"] = hintHtmlCount
        };

        // дописываем оба отчёта в файл
        File.AppendAllText(reportPath, "\n\n" + JsonSerializer.Serialize(reportJson, saveOptions));
        File.AppendAllText(reportPath, "\n\n" + JsonSerializer.Serialize(reportHtml, saveOptions));
        Console.WriteLine($"Отчёт дописан: {reportPath}");
    }
}





