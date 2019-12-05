//https://docs.microsoft.com/pt-br/dotnet/framework/network-programming/using-udp-services
//https://stackoverflow.com/questions/10832770/sending-udp-broadcast-receiving-multiple-messages

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Net; 
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Net.Http;
namespace ChatUDP
{
    class Program
    {
        public static UDPConn conexaoEnviar = new UDPConn(61000, 61000);
        public static UDPConn conexaoReceber = new UDPConn(60000, 60000);
        public static int portaSuprema = 60000;
        public static string lider = null;
        public static string my_ip = "172.18.0.100";
        public static Dictionary<string, int> ips_map = new Dictionary<string, int>() { { "172.18.0.100", 1 }, { "172.18.0.102", 2 }, { "172.18.2.244", 3 } };
        public static DateTime lastRespond;
        public static Thread t3 = null;
        public static Dictionary<string, string> valueDic;
        public static bool processing = false;
        public static int actual_time = 1;
        public static int step_time = 20000000;
        public static string dados = "";





        static void Main(string[] args)
        {

           

            Thread t1 = new Thread(EnviarParalista);
            t1.Start();

            Thread t2 = new Thread(ReceberValores);
            t2.Start();


            


        }

        public static void BuscarValor()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mineracao-facens.000webhostapp.com/request.php");
            WebResponse response = request.GetResponse();
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                object objResponse = reader.ReadToEnd();
                valueDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(objResponse.ToString());
                if (valueDic.Count == 0)
                {
                    valueDic = new Dictionary<string, string>();
                    valueDic["timestemp"] = DateTime.Now.ToString("yyyyMMddHHmmss");
  
                }
                else
                {
                    valueDic["timestemp"] = DateTime.Now.ToString("yyyyMMddHHmmss");
                }
            }
            
        }

        public static void ProcurarValor()
        {

        }

        public static void EnviarParalista()
        {
            List<string> ips = new List<string>(ips_map.Keys);
         
            while(true)
            {
                conexaoEnviar.SendTextToList(ips, portaSuprema, "Heartbeat Request");
                Thread.Sleep(3000);
                if (lider != null)
                {
                    if (DateTime.Now > lastRespond.AddSeconds(5))
                    {
                        lider = null;
                        Console.WriteLine("Lider Parou de responder");
                    }
                }
                if (lider != null && lider != my_ip && !processing) 
                {
                    List<string> ipsAux = new List<string>() { lider };
                    conexaoEnviar.SendTextToList(ips, portaSuprema, "ProcessRequest");
                }
                
            }
            
        }

        public static string GeraHash(string hash,string nonce,string timestamp)
        {
            var aux = hash + nonce + timestamp;
            using(SHA256 crypto = SHA256.Create())
            {
                byte[] bytes = crypto.ComputeHash(Encoding.UTF8.GetBytes(aux));

                StringBuilder builder = new StringBuilder();
                for(int i=0;i<bytes.Length;i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                //Console.WriteLine("linha:" + builder.ToString());
                return builder.ToString();
            }
 

        }

        public static void Processar()
        {
            List<string> data = dados.Split(';').ToList();
            string zero = "";
            bool find = false;
            for(int a = 0;a<Int32.Parse(data[5]);a++)
            {
                zero += '0';
            }
            for(int a= Int32.Parse(data[1]);a<Int32.Parse(data[2]);a++)
            {
                var resp = GeraHash(data[4], a.ToString(), data[3]);

                Console.WriteLine("resp"+resp);
                if(resp.StartsWith(zero))
                {
                    Console.WriteLine("Achou");
                    find = true;
                    conexaoEnviar.SendTextToList(new List<string>() {lider}, portaSuprema, "ProcessAnswerYes;"+a.ToString());
                    break;
                }

            }
            if(!find)
            {
                conexaoEnviar.SendTextToList(new List<string>() { lider }, portaSuprema, "ProcessAnswerNo");
                Console.WriteLine("Não achou");
            }
            processing = false;
        }
        
        private static async void SendHallOfFameAsync(string nonce, string PoolName)
        {

            var url = @"https://mineracao-facens.000webhostapp.com/submit.php?timestamp=" +  valueDic["timestemp"] + "&nonce="
               + nonce + "&poolname=" + PoolName;

                using (var client = new HttpClient())
                {
                    var response = client.PostAsync(url, null).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("HALL OF FAME: " + responseString);
                    }
                }
        }
        

        public static void ReceberValores()
        {
            while (true)
            {
                string texto = conexaoReceber.DataReceive();
                string ip = conexaoReceber.SenderIP();
                string porta = conexaoReceber.SenderDoor();

                
                if (ips_map.ContainsKey(ip))
                {
                    if (texto.ToUpper().Contains("HEARTBEAT REQUEST"))
                    {

                        List<string> ips = new List<string>() { ip };
                        conexaoEnviar.SendTextToList(ips, portaSuprema, "Heartbeat Reply");

                    }
                    else if (texto.ToUpper().Contains("HEARTBEAT REPLY"))
                    {
                        if (lider == null)
                        {
                            if (ips_map.ContainsKey(ip))
                            {
                                lider = ip;
                                lastRespond = DateTime.Now;
                                Console.WriteLine("NovoLider: " + lider);
                                if (lider == my_ip)
                                {
                                    BuscarValor();
                                    processing = false;
                                    actual_time = 0;
                                }
                            }
                        }
                        else if (lider.Contains(ip))
                        {
                            if (ips_map.ContainsKey(ip))
                            {
                                lastRespond = DateTime.Now;
                            }
                        }
                        if (lider != null && ips_map[ip] < ips_map[lider])
                        {
                            if (ips_map.ContainsKey(ip))
                            {
                                lider = ip;
                                lastRespond = DateTime.Now;
                                Console.WriteLine("NovoLider: " + lider);
                                if (lider == my_ip)
                                {
                                    BuscarValor();
                                    processing = false;
                                    actual_time = 0;
                                }
                            }
                        }

                    }
                    else if (texto.Contains("ProcessRequest") && lider !=null && lider.Contains(my_ip)) 
                    {
                        if (lider == null)
                        {
                            if (ips_map.ContainsKey(ip))
                            {
                                lider = ip;
                                lastRespond = DateTime.Now;
                                Console.WriteLine("NovoLider: " + lider);
                                if (lider == my_ip)
                                {
                                    BuscarValor();
                                    processing = false;
                                    actual_time = 0;
                                }
                            }
                        }
                        int auxInt = actual_time;
                        int auxStepFinal = actual_time + step_time;
                        if (auxStepFinal >= 2000000000)
                        {
                            auxStepFinal = 2000000000;
                            actual_time = 1;
                            valueDic["timestemp"] = DateTime.Now.ToString("yyyyMMddHHmmss");
                        }
                        else
                        {
                            actual_time = actual_time + step_time + 1;
                        }
                        List<string> ips = new List<string>() { ip };
                        conexaoEnviar.SendTextToList(ips, portaSuprema, "Process;" + auxInt + ";" + auxStepFinal + ";" + valueDic["timestemp"] + ";" + valueDic["hash"] + ";" + valueDic["zeros"]);
                    }
                    else if (texto.Contains("ProcessAnswerYes") && lider != null && lider.Contains(my_ip))
                    {
                        List<string> res = texto.Split(';').ToList();
                        SendHallOfFameAsync(res[1], "Dutra");
                         List<string> ips = new List<string>() { ip };
                         conexaoEnviar.SendTextToList(ips, portaSuprema, "ProcessInterrupt");
                    }
                    else if ((lider != my_ip && texto.Split(';')[0] == "Process" && !processing) || texto.Contains("ProcessInterrupt"))
                    {
                        
                        dados = texto;
                        Thread t3 = new Thread(Processar);
                        if (texto.Contains("ProcessInterrupt"))
                        {
                            t3.Abort();
                        }
                        else
                        {
                            t3.Start();
                        }
                        processing = true;
                    }
                    else
                    {
                        Console.WriteLine(ip + ":" + texto);
                    }
                }
            }
         }
        }

    
}
