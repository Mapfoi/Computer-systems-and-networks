using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chat_Server
{
    public partial class Form1 : Form
    {
        private const int UdpDiscoveryPort = 50000;
        private const string ServerName = "ChatServer";

        private readonly object _syncRoot = new();
        private readonly Dictionary<Socket, ClientInfo> _clients = new();

        private Socket? _udpSocket;
        private Thread? _udpThread;
        private bool _running;

        private string HistoryFilePath =>
            Path.Combine(Application.StartupPath, "server_history.txt");

        public Form1()
        {
            InitializeComponent();
            LoadHistory();
        }

        private void LoadHistory()
        {
            if (File.Exists(HistoryFilePath))
            {
                txtLog.Text = File.ReadAllText(HistoryFilePath, Encoding.UTF8);
            }
        }

        private void AppendLog(string text)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {text}";
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(line)));
                return;
            }

            txtLog.AppendText(line + Environment.NewLine);
            File.AppendAllText(HistoryFilePath, line + Environment.NewLine, Encoding.UTF8);
        }

        private void btnStartStop_Click(object? sender, EventArgs e)
        {
            if (_running)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
        }

        private void StartServer()
        {
            try
            {
                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpSocket.Bind(new IPEndPoint(IPAddress.Any, UdpDiscoveryPort));

                _running = true;
                _udpThread = new Thread(UdpListenLoop) { IsBackground = true };
                _udpThread.Start();

                lblStatus.Text = $"Сервер запущен на UDP-порту {UdpDiscoveryPort}";
                btnStartStop.Text = "Остановить сервер";
                AppendLog("Сервер запущен");
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка запуска сервера: " + ex.Message);
            }
        }

        private void StopServer()
        {
            _running = false;

            lock (_syncRoot)
            {
                foreach (var pair in _clients.ToList())
                {
                    try
                    {
                        SendControlMessage(pair.Key, MessageType.ServerShutdown, "Сервер завершает работу");
                    }
                    catch
                    {
                        // ignore
                    }

                    try
                    {
                        pair.Key.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                        // ignore
                    }

                    pair.Key.Close();
                }

                _clients.Clear();
            }

            try
            {
                _udpSocket?.Close();
            }
            catch
            {
                // ignore
            }

            lblStatus.Text = "Сервер остановлен";
            btnStartStop.Text = "Запустить сервер";
            AppendLog("Сервер остановлен");
        }

        private void UdpListenLoop()
        {
            var buffer = new byte[1024];

            while (_running && _udpSocket != null)
            {
                try
                {
                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    int received = _udpSocket.ReceiveFrom(buffer, ref remote);
                    if (received <= 0)
                    {
                        continue;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, received);
                    // Формат: DISCOVER|ClientName|TcpPort
                    string[] parts = message.Split('|');
                    if (parts.Length == 3 && parts[0] == "DISCOVER")
                    {
                        string clientName = parts[1];
                        if (!int.TryParse(parts[2], out int clientTcpPort))
                        {
                            continue;
                        }

                        if (remote is IPEndPoint ep)
                        {
                            HandleNewClient(ep.Address, clientTcpPort, clientName);
                        }
                    }
                }
                catch (SocketException)
                {
                    if (!_running)
                    {
                        return;
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    AppendLog("Ошибка при обработке UDP: " + ex.Message);
                }
            }
        }

        private void HandleNewClient(IPAddress address, int tcpPort, string clientName)
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(address, tcpPort));

                var info = new ClientInfo
                {
                    Name = clientName,
                    EndPoint = (IPEndPoint)socket.RemoteEndPoint!
                };

                lock (_syncRoot)
                {
                    _clients[socket] = info;
                }

                AppendLog($"Подключен клиент {clientName} ({info.EndPoint})");

                // Отправляем клиенту подтверждение
                SendControlMessage(socket, MessageType.Join, "Подключение к серверу установлено");

                // Уведомляем остальных клиентов
                BroadcastControl(MessageType.Join, $"{clientName} присоединился к чату", exclude: socket);

                var thread = new Thread(() => ClientReceiveLoop(socket)) { IsBackground = true };
                thread.Start();
            }
            catch (Exception ex)
            {
                AppendLog($"Не удалось подключиться к клиенту {clientName} ({address}:{tcpPort}): {ex.Message}");
            }
        }

        private void ClientReceiveLoop(Socket socket)
        {
            var buffer = new byte[4096];
            var sb = new StringBuilder();

            while (_running)
            {
                try
                {
                    int received = socket.Receive(buffer);
                    if (received <= 0)
                    {
                        break;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, received));

                    while (true)
                    {
                        string text = sb.ToString();
                        int idx = text.IndexOf('\n');
                        if (idx < 0)
                        {
                            break;
                        }

                        string line = text[..idx].TrimEnd('\r');
                        sb.Remove(0, idx + 1);

                        if (line.Length == 0)
                        {
                            continue;
                        }

                        HandleClientMessage(socket, line);
                    }
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AppendLog("Ошибка приема от клиента: " + ex.Message);
                    break;
                }
            }

            HandleClientDisconnect(socket);
        }

        private void HandleClientMessage(Socket socket, string line)
        {
            // Формат: Type|SenderName|TimestampIso|SenderIp|Text
            string[] parts = line.Split('|');
            if (parts.Length < 5)
            {
                return;
            }

            if (!Enum.TryParse(parts[0], out MessageType type))
            {
                return;
            }

            string senderName = parts[1];
            string timestamp = parts[2];
            string senderIp = parts[3];
            string text = parts[4];

            switch (type)
            {
                case MessageType.Chat:
                    AppendLog($"[{timestamp}] {senderName} ({senderIp}): {text}");
                    BroadcastRaw(line, exclude: socket);
                    break;
                case MessageType.Leave:
                    AppendLog($"Клиент {senderName} ({senderIp}) отключился");
                    BroadcastRaw(line, exclude: socket);
                    HandleClientDisconnect(socket);
                    break;
            }
        }

        private void HandleClientDisconnect(Socket socket)
        {
            ClientInfo? info = null;
            lock (_syncRoot)
            {
                if (_clients.TryGetValue(socket, out info))
                {
                    _clients.Remove(socket);
                }
            }

            if (info != null)
            {
                AppendLog($"Клиент {info.Name} ({info.EndPoint}) отключен");
                BroadcastControl(MessageType.Leave, $"{info.Name} покинул чат", exclude: socket);
            }

            try
            {
                socket.Close();
            }
            catch
            {
                // ignore
            }
        }

        private void BroadcastRaw(string line, Socket? exclude = null)
        {
            byte[] data = Encoding.UTF8.GetBytes(line + "\n");

            lock (_syncRoot)
            {
                foreach (var socket in _clients.Keys.ToList())
                {
                    if (socket == exclude)
                    {
                        continue;
                    }

                    try
                    {
                        socket.Send(data);
                    }
                    catch
                    {
                        // ignore send errors
                    }
                }
            }
        }

        private void BroadcastControl(MessageType type, string text, Socket? exclude = null)
        {
            var line = BuildMessage(type, ServerName, text, IPAddress.Loopback.ToString());
            BroadcastRaw(line, exclude);
        }

        private void SendControlMessage(Socket socket, MessageType type, string text)
        {
            var line = BuildMessage(type, ServerName, text, IPAddress.Loopback.ToString());
            byte[] data = Encoding.UTF8.GetBytes(line + "\n");
            socket.Send(data);
        }

        private static string BuildMessage(MessageType type, string senderName, string text, string senderIp)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return $"{type}|{senderName}|{timestamp}|{senderIp}|{text}";
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_running)
            {
                StopServer();
            }
        }

        private class ClientInfo
        {
            public string Name { get; set; } = string.Empty;
            public IPEndPoint EndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
        }

        private enum MessageType
        {
            Chat,
            Join,
            Leave,
            ServerShutdown
        }
    }
}
