using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IMAP_Client
{
    public class Client
    {
        private TcpClient client { get; }
        private IPEndPoint ServerEndPoint { get; }
        private NetworkStream Stream { get; set; }
        private string oldMessagePart = "";
  

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

                foreach (var res in connectionResponse)
                {
                    if (res.Split(' ')[1] == Status.OK.Value)
                    {
                        Console.WriteLine("Соединение установлено");
                        return true;
                    }
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
                commandNumber++;
                var command = $"A{commandNum} LOGIN {name} {password}";

                await SendMessageAsync(command);

                var response = await ReadMessageAsync(commandNum);

                foreach (var res in response)
                {
                    if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "OK")
                    {
                        Console.WriteLine("Выполнен вход");
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Что-то пошло не так: {e.Message}");
            }

            Console.WriteLine("Неверный логин/пароль");
            return false;
        }

        public async Task<bool> Logout()
        {
            try
            {
                var commandNum = commandNumber;
                commandNumber++;
                var command = $"A{commandNum} LOGOUT";

                await SendMessageAsync(command);

                var response = await ReadMessageAsync(commandNum);

                foreach (var res in response)
                {
                    if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "OK")
                    {
                        Console.WriteLine("Выполнен выход");
                        CLoseConnection();
                        return true;
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
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
        }

        public async Task SendMessageAsync(string message)
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

        public async Task<List<string>> ReadMessageAsync()
        {
            List<string> result = new List<string>();

            try
            {
                var data = new byte[1024];
                int bytes;
                string response = null;
                await using var stream = new NetworkStream(client.Client);
                do
                {
                    bytes = await stream.ReadAsync(data);
                    response += Encoding.UTF8.GetString(data, 0, bytes);
                } while (data[^1] != 0);

                result = response.Split("\r\n").ToList();

                if (oldMessagePart.Length > 0)
                {
                    result[0] = oldMessagePart + response.Split("\r\n")[0];
                    oldMessagePart = "";
                }


                if (result.Last().SkipLast(Math.Max(0, result.Last().Length - 2)) != "\r\n")
                {
                    oldMessagePart += result.Last();
                    result.Remove(result.Last());
                }


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return result;
        }

        public async Task Close()
        {
            try
            {
                var commandNum = commandNumber;
                commandNumber++;
                var command = $"A{commandNum} CLOSE";

                await SendMessageAsync(command);

                var response = await ReadMessageAsync(commandNum);

                foreach (var res in response)
                {
                    if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "OK")
                    {
                        Console.WriteLine("Папка закрыта");
                        return;
                    }
                    if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "NO")
                    {
                        Console.WriteLine("Не удалось закрыть папку");
                        return;
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Не удалось закрыть папку");
                Console.WriteLine($"Что-то пошло не так: {e.Message}");
            }
        }

        public async Task<List<string>> ReadMessageAsync(int commandNum)
        {
            List<string> result = new List<string>();

            try
            {
                var data = new byte[1024];
                int bytes;
                string response = null;
                await using var stream = new NetworkStream(client.Client);
                do
                {
                    bytes = await stream.ReadAsync(data);
                    response += Encoding.UTF8.GetString(data, 0, bytes);
                } while (!response.Contains($"A{commandNum}"));

                result = response.Split("\r\n").ToList();

                if (oldMessagePart.Length > 0)
                {
                    result[0] = oldMessagePart + response.Split("\r\n")[0];
                    oldMessagePart = "";
                }


                if (result.Last().SkipLast(Math.Max(0, result.Last().Length - 2)) != "\r\n")
                {
                    oldMessagePart += result.Last();
                    result.Remove(result.Last());
                }


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return result;
        }

        public void CLoseConnection()
        {
            //Сделать правильное отключение
            client.Close();
        }

        public async Task<HashSet<string>> List(string box)
        {
            try
            {
                var boxList = new HashSet<string>();

                Console.WriteLine($"LIST {box}");
                var commandNum = commandNumber;
                var command = $"A{commandNum} LIST {box}";
                commandNumber++;
                await SendMessageAsync(command);


                var response = await ReadMessageAsync(commandNum);


                foreach (var res in response)
                {
                    if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "OK")
                    {
                        Console.WriteLine("Папки получены");
                        return boxList;
                    }

                    if (res.Split(' ')[4].Contains('.'))
                    {
                        foreach (var re in res.Split(' ')[4].Split('.'))
                        {
                            boxList.Add(re.Trim('\"'));
                        }
                    }
                    else
                    {
                        boxList.Add(res.Split(' ')[4].Trim('\"'));
                    }

                   
                }

                Console.WriteLine("Не удалось получить папки");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Что-то пошло не так: {e.Message}");
                return null;
            }
        }

        public async Task Select(string box)
        {
            try
            {
                var commandNum = commandNumber;
                Console.WriteLine($"SELECT {box}");
                var command = $"A{commandNum} SELECT {box}";
                commandNumber++;
                await SendMessageAsync(command);

                var response = await ReadMessageAsync(commandNum);

                foreach (var res in response)
                {
                    if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "OK")
                    {
                        Console.WriteLine($"{res.Split(' ')[1]}");
                        return;
                    }

                    Console.WriteLine($"{res.Split(' ', 2)[1]}");
                }

                Console.WriteLine("Не удалось выбрать папку");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Что-то пошло не так: {e.Message}");
            }
        }


        public async Task<Dictionary<string, string>> Fetch(string UID, string whatFetch)
        {
            try
            {
                var commandNum = commandNumber;
                Console.WriteLine($"FETCH {UID} {whatFetch}");
                var command = $"A{commandNum} FETCH {UID} {whatFetch}";
                commandNumber++;
                await SendMessageAsync(command);

                var mailDict = new Dictionary<string, string>();

                var response = await ReadMessageAsync(commandNum);

                if (whatFetch == "ALL")
                {
                    foreach (var res in response)
                    {
                        Console.WriteLine($"{res}");
                    }
                }

                if(whatFetch == "BODY[]")
                {

                    foreach (var res in response)
                    {
                        if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "OK")
                        {
                            Console.WriteLine(res);
                            return mailDict;
                        }

                        mailDict[res.Split(':')[0]] = res.Split(':')[1];
                    }
                }
                
                return mailDict;
            }
            catch (Exception e)
            {
                Console.WriteLine("Не удалось получить письмо");
                Console.WriteLine(e);

            }

            return null;
        }

        public async Task<List<uint>> SearchAll()
        {
            var uidList = new List<uint>();
            try
            {
                var commandNum = commandNumber;
               
                Console.WriteLine("FETCH ALL");
                var command = $"A{commandNum} UID SEARCH ALL";
                commandNumber++;
                await SendMessageAsync(command);

                var response = await ReadMessageAsync(commandNum);

                foreach (var res in response)
                {
                    if (res.Split(' ')[0] == $"A{commandNum}" && res.Split(' ')[1] == "OK")
                    {
                        Console.WriteLine($"{res.Split(' ')[1]}");
                        return uidList;
                    }

                    uint uid;
                    if (uint.TryParse(res.Split(' ')[1], out uid))
                    {
                        uidList.Add(uid);
                    }
                   
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return uidList;
        }



    }
}
