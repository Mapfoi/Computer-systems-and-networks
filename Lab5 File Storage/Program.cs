using System.Globalization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Корень хранилища — папка "storage" рядом с исполняемым файлом
var storageRoot = Path.Combine(app.Environment.ContentRootPath, "storage");
Console.WriteLine($"Хранилище: {storageRoot}");
Directory.CreateDirectory(storageRoot);

// Преобразуем URL-путь в физический, убирая начальный слеш
static string ToPhysicalPath(string basePath, string urlPath)
{
    // Защита от выхода за пределы хранилища: заменяем ".." и убираем ведущий '/'
    var safePath = urlPath.Replace("..", "")
                          .TrimStart('/')
                          .Replace('/', Path.DirectorySeparatorChar);
    return Path.Combine(basePath, safePath);
}

// ---------------------------------------------------------------------------
// Обработчик для КОРНЯ хранилища (пустой путь)
// ---------------------------------------------------------------------------
app.MapGet("/", () =>
{
    var entries = Directory.GetFileSystemEntries(storageRoot)
                           .Select(full => Path.GetRelativePath(storageRoot, full))
                           .OrderBy(name => name)
                           .ToArray();
    return Results.Json(entries, new JsonSerializerOptions
    {
        WriteIndented = true
    });
});

app.MapMethods("/", new[] { "HEAD" }, () =>
{
    // Корень - всегда каталог, HEAD для каталога не имеет смысла
    return Results.Ok();
});

// ---------------------------------------------------------------------------
// PUT /{**path} – загрузка файла с перезаписью или копирование по X-Copy-From
// ---------------------------------------------------------------------------
app.MapPut("/{**path}", async (string? path, HttpRequest request) =>
{
    if (string.IsNullOrWhiteSpace(path) || path.EndsWith("/"))
        return Results.BadRequest("Путь должен указывать на файл, а не на каталог.");

    var physicalPath = ToPhysicalPath(storageRoot, path);
    // Создаём недостающие родительские каталоги
    Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

    // Проверяем заголовок X-Copy-From
    if (request.Headers.TryGetValue("X-Copy-From", out var copySource))
    {
        var sourcePath = ToPhysicalPath(storageRoot, copySource.ToString());
        if (!File.Exists(sourcePath))
            return Results.NotFound($"Исходный файл не найден: {copySource}");
        File.Copy(sourcePath, physicalPath, overwrite: true);
        return Results.Ok($"Файл скопирован из {copySource} в {path}");
    }

    // Обычная загрузка данных из тела запроса
    await using var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write);
    await request.Body.CopyToAsync(fileStream);

    return Results.Ok($"Файл загружен: {path}");
});

// ---------------------------------------------------------------------------
// GET /{**path} – получение файла или списка содержимого каталога (JSON)
// ---------------------------------------------------------------------------
app.MapGet("/{**path}", (string? path) =>
{
    // Если путь пустой - перенаправляем на обработчик корня
    if (string.IsNullOrWhiteSpace(path))
    {
        return GetDirectoryListing(storageRoot);
    }

    var physicalPath = ToPhysicalPath(storageRoot, path);

    // Если физический путь — каталог, отдаём список его содержимого
    if (Directory.Exists(physicalPath))
    {
        return GetDirectoryListing(physicalPath);
    }

    // Если это файл, отдаём его содержимое
    if (File.Exists(physicalPath))
    {
        return Results.File(physicalPath);
    }

    return Results.NotFound($"Ресурс {path} не существует.");
});

// ---------------------------------------------------------------------------
// HEAD /{**path} – заголовки о файле без тела
// ---------------------------------------------------------------------------
app.MapMethods("/{**path}", new[] { "HEAD" }, (string? path) =>
{
    if (string.IsNullOrWhiteSpace(path))
        return Results.Ok(); // HEAD для корневого каталога

    var physicalPath = ToPhysicalPath(storageRoot, path);
    if (!File.Exists(physicalPath))
        return Results.NotFound();

    return Results.File(physicalPath);
});

// ---------------------------------------------------------------------------
// DELETE /{**path} – удаление файла или каталога
// ---------------------------------------------------------------------------
app.MapDelete("/{**path}", (string? path) =>
{
    if (string.IsNullOrWhiteSpace(path))
        return Results.BadRequest("Нельзя удалить корень хранилища.");

    var physicalPath = ToPhysicalPath(storageRoot, path);

    if (File.Exists(physicalPath))
    {
        File.Delete(physicalPath);
        return Results.NoContent();
    }

    if (Directory.Exists(physicalPath))
    {
        Directory.Delete(physicalPath, recursive: true);
        return Results.NoContent();
    }

    return Results.NotFound($"Ресурс {path} не существует.");
});

// ---------------------------------------------------------------------------
// Вспомогательная функция для вывода списка содержимого каталога
// ---------------------------------------------------------------------------
static IResult GetDirectoryListing(string directoryPath)
{
    var entries = Directory.GetFileSystemEntries(directoryPath)
                           .Select(full => Path.GetRelativePath(directoryPath, full))
                           .OrderBy(name => name)
                           .ToArray();
    return Results.Json(entries, new JsonSerializerOptions
    {
        WriteIndented = true
    });
}

//Запуск Kertsel
app.Run();