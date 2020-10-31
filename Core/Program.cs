using System;
using System.Threading;
//using Base;
//using NLog;
using JsonFx;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;
using XLua;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using Newtonsoft.Json;
//using Core;

namespace ETModel
{
	internal static class Program
    {
        public static Wallet wallet = new Wallet();
        public static Dictionary<string, WalletKey> keyBag = new Dictionary<string, WalletKey>();
        
        public static string ruleIP = "";
        public static string nonce ="";
        public static  string input="";

        public static JToken jdNode;
        private static async System.Threading.Tasks.Task Main(string[] args)
        {
            Wallet wallet = new Wallet();
            try
            {
                Dictionary<string, string> setip = new Dictionary<string, string>();
                string wordTemplateName = "./ip.json";
                StreamReader sr = File.OpenText(wordTemplateName);
                string jsonWord = sr.ReadToEnd();
                setip = JsonHelper.FromJson<Dictionary<string, string>>(jsonWord);
                string ips = setip["ip"].Split(":")[0];
                if (System.Net.IPAddress.TryParse(ips, out IPAddress ip))
                {
                    ruleIP = "http://" + ip.ToString() + ":" + setip["ip"].Split(":")[1];
                }
                else
                {
                    IPHostEntry host = Dns.GetHostEntry(ips);
                    ip = host.AddressList[0];
                    ruleIP = "http://" + ip.ToString() + ":" + setip["ip"].Split(":")[1];
                }
            }
            catch (Exception e)
            {
                try
                {
                    if (System.Net.IPAddress.TryParse(args[0], out IPAddress ip))
                    {
                        ruleIP = "http://" + ip.ToString() + ":" + args[1];
                    }
                    else
                    {
                        IPHostEntry host = Dns.GetHostEntry(args[0]);
                        ip = host.AddressList[0];
                        ruleIP = "http://" + ip.ToString() + ":" + args[1];
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Please check the IP file and input IP port");
                    while (true) ;
                }
            }

            //string web = "seednode.smartx.one";
            //IPHostEntry host = Dns.GetHostEntry(web);
            //IPAddress ip = host.AddressList[0];
            //ruleIP = "http://" + ip.ToString();
            //ruleIP += ":5000";
            ////ruleIP = "http://192.168.1.101:5000";

            while (true)
            {
                Console.Write("smartx>");

                HttpMessage httpMessage = new HttpMessage();
                httpMessage.map = new Dictionary<string, string>();

                string input = Console.ReadLine();
                Program.input = input;
                if (input == "") continue;


                input = input.Replace("%20", " ");
                input = input.Replace("  ", " ");
                input = input.Replace("   ", " ");
                input = input.Replace("  ", " ");

                string[] array = input.Split(' ');

                for (int ii = 0; ii < array.Length; ii++)
                {
                    string arrayValue = array[ii];
                    httpMessage.map.Remove("" + ii);
                    httpMessage.map.Add("" + ii, arrayValue);
                }

                httpMessage.map.Add("cmd", array[0]);
                await Command.OrderAsync(httpMessage);
                if (httpMessage.result != null && httpMessage.map["cmd"]!="transfer")
                {
                    if (httpMessage.result.Contains("{")) httpMessage.result = ConvertJsonString(httpMessage.result);
                }
                Console.WriteLine(httpMessage.result);
            }
        }
        
        private static string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }  //将json化的字符串可视化显示
        
        static public void Update()
        {
            float lasttime = TimeHelper.time;
            while (true)
            {
                try
                {
                    TimeHelper.deltaTime = TimeHelper.time - lasttime;
                    lasttime = TimeHelper.time;

                    Thread.Sleep(1);
                    OneThreadSynchronizationContext.Instance.Update();
                    Entity.Root.Update();
                    CoroutineMgr.UpdateCoroutine();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}
