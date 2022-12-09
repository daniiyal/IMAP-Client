using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace IMAP_Client
{
    public class Client
    {
        private TcpClient client { get; }
        private IPEndPoint ServerEndPoint { get; }
        private NetworkStream Stream { get; set; }

        private Dictionary<string, string> responses;

        private static int commandNumber = 0;

        public Client(string ip, int port)
        {
            ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            client = new TcpClient();
        }

        public async Task<bool> ConnectServerAsync()
        {
            try
            {
                await client.ConnectAsync(ServerEndPoint);
                Stream = client.GetStream();
                var connectionResponse = await ReadMessageAsync();

                if (connectionResponse.Split(' ')[1] == Status.OK.Value)
                {
                    Console.WriteLine("Соединение установлено");
                    return true;
                }

            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Не удалось установить соединение");

            return false;
        }


        public async Task<bool> Login(string name, string password)
        {
            try
            {
                var commandNum = commandNumber;
                var command = $"A{commandNum} LOGIN {name} {CreateMD5(password)}";

                SendMessageAsync(command);

                await ReadMessageAsync();
      
                foreach (var res in responses)
                {
                    if (res.Split(' ')[0] == $"A{commandNum}")
                    {

                    }
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine($"Что-то пошло не так: {e.Message}");
            }

            return false;
        }


        private static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
        }

        public async void SendMessageAsync(string message)
        {
            try
            {
                var request = Encoding.UTF8.GetBytes(message + "\r\n");

                await Stream.WriteAsync(request);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        public async Task ReadMessageAsync()
        {
            string? response = null;

            try
            {
                var data = new byte[1024];

                await using var stream = new NetworkStream(client.Client);
                do
                {
                    int bytes = await stream.ReadAsync(data);
                    response += Encoding.UTF8.GetString(data, 0, bytes);
                }
                while (data[^1] != 0);

                foreach (var res in response.Split("\r\n"))
                {
                    responses.Add(res.Split(' ')[0], res.Split(' '));
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return response;
        }

        public void CLoseConnection()
        {
            //Сделать правильное отключение
            client.Close();
        }
    }
}
