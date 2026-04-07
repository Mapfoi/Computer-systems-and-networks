using System.Net.Sockets;
using System.Text;

namespace SimpleProxy;

class Program
{
    private static List<string> _blockedSites = new();
    private static readonly int Port = 8888;

    static async Task Main()
    {
        // Загружаем черный список
        LoadBlockedSites();

        Console.WriteLine($"Прокси-сервер запущен на порту {Port}");
        Console.WriteLine($"Заблокировано сайтов: {_blockedSites.Count}");
        Console.WriteLine($"Настройте браузер на localhost:{Port}");
        Console.WriteLine("Нажмите Ctrl+C для выхода");
        Console.WriteLine("----------------------------------------");

        // Создаем TCP слушатель
        TcpListener listener = new TcpListener(System.Net.IPAddress.Any, Port);
        listener.Start();

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    static void LoadBlockedSites()
    {
        if (File.Exists("blocked.txt"))
        {
            _blockedSites = File.ReadAllLines("blocked.txt")
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim().ToLower())
                .ToList();
        }
        else
        {
            // Создаем пример файла, если его нет
            File.WriteAllText("blocked.txt", "facebook.com\nyoutube.com\ntwitter.com");
            _blockedSites = new List<string> { "facebook.com", "youtube.com", "twitter.com" };
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            try
            {

                // Читаем первый запрос от браузера
                byte[] buffer = new byte[8192];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                
                // Парсим URL
                string url = ExtractUrl(request);
                if (string.IsNullOrEmpty(url))
                    return;

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Запрос: {url}");

                // Проверяем блокировку
                if (IsBlocked(url))
                {
                    Console.WriteLine($"  -> ЗАБЛОКИРОВАН");
                    string blockedPage = GetBlockedPage(url);
                    byte[] response = Encoding.UTF8.GetBytes(blockedPage);
                    await stream.WriteAsync(response, 0, response.Length);
                    return;
                }

                // Проксируем запрос
                await ProxyRequest(request, stream);
                Console.WriteLine($"  -> УСПЕШНО");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    static string ExtractUrl(string request)
    {
        var lines = request.Split('\n');
        if (lines.Length == 0)
            return null;

        var parts = lines[0].Split(' ');
        if (parts.Length < 2)
            return null;

        string path = parts[1];

        string host = ExtractHost(request);
        if (string.IsNullOrEmpty(host))
            return null;

        // если уже полный URL
        if (path.StartsWith("http://"))
            return path;

        return "http://" + host + path;
    }

    static bool IsBlocked(string url)
    {
        var uri = new Uri(url);
        string host = uri.Host.ToLower();

        return _blockedSites.Any(site => host.Contains(site));
    }

    static string GetBlockedPage(string blockedUrl)
    {
        return @"HTTP/1.1 403 Forbidden
Content-Type: text/html; charset=utf-8
Content-Length: 456

<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>Доступ заблокирован</title></head>
<body>
<h1>Доступ запрещен</h1>
<p>Сайт <b>" + blockedUrl + @"</b> находится в черном списке.</p>
<p>Обратитесь к администратору.</p>
<hr>
<small>Прокси-сервер</small>
</body>
</html>";
    }

    static async Task ProxyRequest(string request, NetworkStream clientStream)
    {
        // Извлекаем хост
        string host = ExtractHost(request);
        if (string.IsNullOrEmpty(host))
            return;

        // Подключаемся к целевому серверу
        using var server = new TcpClient();
        await server.ConnectAsync(host, 80);

        using var serverStream = server.GetStream();

        // Пересылаем запрос на сервер
        byte[] requestBytes = Encoding.ASCII.GetBytes(request);
        await serverStream.WriteAsync(requestBytes, 0, requestBytes.Length);

        // Пересылаем ответ клиенту
        byte[] buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await clientStream.WriteAsync(buffer, 0, bytesRead);
        }
    }

    static string ExtractHost(string request)
    {
        // Ищем Host: example.com
        var lines = request.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(5).Trim();
            }
        }
        return null;
    }
}