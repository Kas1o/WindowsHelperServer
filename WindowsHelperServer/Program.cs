using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MessageServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RunServer();
        }

        static async Task RunServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:10829/");
            listener.Start();

            Console.WriteLine("Server started, listening on http://localhost:10829/");

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HandleRequest(context).ConfigureAwait(false);
            }
        }
        static async Task HandleRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "OPTIONS")
            {
                // 设置响应头,允许跨域请求
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                context.Response.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
                context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                return;
            }

            if (context.Request.HttpMethod == "POST")
            {
                using (StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    string requestBody = await reader.ReadToEndAsync();
                    //Console.WriteLine($"\nReceived message: {requestBody}");
                    ProcessCommand(requestBody);
                }

                string responseMessage = "Message received";
                byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                context.Response.ContentLength64 = responseBytes.Length;
                await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            else
            {
                Console.WriteLine($"Received {context.Request.HttpMethod} request at {context.Request.RawUrl}");
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }

            context.Response.OutputStream.Close();
        }

        static void ProcessCommand(string input)
        {
            // 懒得改。
            var commands = ExtractData(input);

            foreach (var command in commands)
            {
                foreach (var item in command.ToArray())
                {
                    switch(item.Key)
                    {
                        case "wifi":
                            bool open = false;
                            if (bool.TryParse((string)item.Value,out open))
                                Console.WriteLine(open? "wifi已经开启" : "wifi已经关闭");
                                //我没wifi网卡
                            break;
                        case "bluetooth":
                            bool open2 = false;
                            if (bool.TryParse((string)item.Value, out open2))
                                Console.WriteLine(open2 ? "蓝牙已经开启" : "蓝牙已经关闭");
                                //我没蓝牙
                            break;
                        case "mute":
                            bool open3 = false;
                            if (bool.TryParse((string)item.Value, out open3))
                            {
                                Console.WriteLine(open3 ? "静音已经开启" : "静音已经关闭");
                                SystemVolume.SetMasterVolumeMute(open3);
                            }
                            break;
                        case "volume":
                            float target_volume = 0;
                            if(float.TryParse((string)item.Value,out target_volume))
                            {
                                Console.WriteLine($"音量已经设为{target_volume}");
                                SystemVolume.SetMasterVolume(target_volume);
                            }
                            break;
                    }
                }
            }
        }
        public static List<Dictionary<string, object>> ExtractData(string input)
        {
            var result = new List<Dictionary<string, object>>();
            var regex = new Regex(@"<(\w+):([^>]+)>");
            var matches = regex.Matches(input);

            foreach (Match match in matches)
            {
                var dict = result.Find(d => d.ContainsKey(match.Groups[1].Value));
                if (dict == null)
                {
                    dict = new Dictionary<string, object>();
                    result.Add(dict);
                }
                dict[match.Groups[1].Value] = match.Groups[2].Value;
            }

            return result;
        }
    }
}