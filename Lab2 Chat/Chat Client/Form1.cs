using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chat_Client
{
    public partial class Form1 : Form
    {
        private const int UdpDiscoveryPort = 50000;

        private Socket? _udpSocket;
        private Socket? _listenerSocket;
        private Socket? _serverSocket;
        private Thread? _acceptThread;
        private Thread? _receiveThread;
        private bool _connected;

        private string HistoryFilePath =>
            Path.Combine(Application.StartupPath, "client_history.txt");

        public Form1()
        {
            InitializeComponent();
            LoadHistory();
        }

        private void LoadHistory()
        {
            if (File.Exists(HistoryFilePath))
            {
                txtHistory.Text = File.ReadAllText(HistoryFilePath, Encoding.UTF8);
            }
        }

        private void AppendHistory(string text)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {text}";
            if (txtHistory.InvokeRequired)
            {
                txtHistory.Invoke(new Action(() => AppendHistory(text)));
                return;
            }

            txtHistory.AppendText(line + Environment.NewLine);
            File.AppendAllText(HistoryFilePath, line + Environment.NewLine, Encoding.UTF8);
        }

        private void btnConnect_Click(object? sender, EventArgs e)
        {
            if (_connected)
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        }

        private void Connect()
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Укажите имя клиента.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtTcpPort.Text.Trim(), out int tcpPort) || tcpPort <= 0 || tcpPort > 65535)
            {
                MessageBox.Show("Некорректный TCP-порт.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // TCP слушатель для входящего соединения от сервера
                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, tcpPort));
                _listenerSocket.Listen(1);

                _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
                _acceptThread.Start();

                // Отправляем широковещательный UDP пакет
                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.EnableBroadcast = true;

                string discoverMessage = $"DISCOVER|{name}|{tcpPort}";
                byte[] data = Encoding.UTF8.GetBytes(discoverMessage);
                var endPoint = new IPEndPoint(IPAddress.Broadcast, UdpDiscoveryPort);
                _udpSocket.SendTo(data, endPoint);

                lblStatus.Text = "Ожидание подключения сервера...";
                AppendHistory("Отправлен широковещательный запрос на поиск сервера.");
            }
            catch (Exception ex)
            {
                AppendHistory("Ошибка подключения: " + ex.Message);
                CleanupSockets();
            }
        }

        private void AcceptLoop()
        {
            try
            {
                if (_listenerSocket == null)
                {
                    return;
                }

                var serverSocket = _listenerSocket.Accept();
                _serverSocket = serverSocket;
                _connected = true;

                BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = "Подключено к серверу.";
                    btnConnect.Text = "Отключиться";
                }));

                AppendHistory($"Установлено TCP-соединение с сервером {serverSocket.RemoteEndPoint}.");

                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                _receiveThread.Start();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                AppendHistory("Ошибка при ожидании сервера: " + ex.Message);
            }
        }

        private void ReceiveLoop()
        {
            if (_serverSocket == null)
            {
                return;
            }

            var buffer = new byte[4096];
            var sb = new StringBuilder();

            while (_connected)
            {
                try
                {
                    int received = _serverSocket.Receive(buffer);
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

                        HandleServerMessage(line);
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
                    AppendHistory("Ошибка приема от сервера: " + ex.Message);
                    break;
                }
            }

            ServerDisconnected();
        }

        private void HandleServerMessage(string line)
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
                    AppendHistory($"[{timestamp}] {senderName} ({senderIp}): {text}");
                    break;
                case MessageType.Join:
                    AppendHistory($"[{timestamp}] {text}");
                    break;
                case MessageType.Leave:
                    AppendHistory($"[{timestamp}] {text}");
                    break;
                case MessageType.ServerShutdown:
                    AppendHistory($"[{timestamp}] Сервер завершил работу: {text}");
                    ServerDisconnected();
                    break;
            }
        }

        private void btnSend_Click(object? sender, EventArgs e)
        {
            SendCurrentMessage();
        }

        private void txtMessage_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendCurrentMessage();
            }
        }

        private void SendCurrentMessage()
        {
            if (!_connected || _serverSocket == null)
            {
                MessageBox.Show("Нет подключения к серверу.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string text = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string name = txtName.Text.Trim();
            string ip = GetLocalIpAddress();

            string line = BuildMessage(MessageType.Chat, name, text, ip);

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(line + "\n");
                _serverSocket.Send(data);
                AppendHistory($"Вы: {text}");
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                AppendHistory("Ошибка отправки сообщения: " + ex.Message);
            }
        }

        private void Disconnect()
        {
            if (!_connected)
            {
                return;
            }

            try
            {
                if (_serverSocket != null)
                {
                    string name = txtName.Text.Trim();
                    string ip = GetLocalIpAddress();
                    string line = BuildMessage(MessageType.Leave, name, "Клиент отключился", ip);
                    byte[] data = Encoding.UTF8.GetBytes(line + "\n");
                    _serverSocket.Send(data);
                }
            }
            catch
            {
                // ignore
            }

            CleanupSockets();
            _connected = false;
            lblStatus.Text = "Не подключено к чату";
            btnConnect.Text = "Подключиться";
            AppendHistory("Клиент отключился от сервера.");
        }

        private void ServerDisconnected()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ServerDisconnected));
                return;
            }

            CleanupSockets();
            _connected = false;
            lblStatus.Text = "Соединение с сервером потеряно";
            btnConnect.Text = "Подключиться";
            AppendHistory("Соединение с сервером потеряно.");
        }

        private void CleanupSockets()
        {
            try
            {
                _serverSocket?.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // ignore
            }

            try
            {
                _serverSocket?.Close();
                _listenerSocket?.Close();
                _udpSocket?.Close();
            }
            catch
            {
                // ignore
            }

            _serverSocket = null;
            _listenerSocket = null;
            _udpSocket = null;
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                return ip?.ToString() ?? "0.0.0.0";
            }
            catch
            {
                return "0.0.0.0";
            }
        }

        private static string BuildMessage(MessageType type, string senderName, string text, string senderIp)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return $"{type}|{senderName}|{timestamp}|{senderIp}|{text}";
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_connected)
            {
                Disconnect();
            }
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
