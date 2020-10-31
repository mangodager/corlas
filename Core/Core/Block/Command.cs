using ILRuntime.Runtime.Debugger.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ETModel
{
    public class Command
    {
        public static async System.Threading.Tasks.Task OrderAsync(HttpMessage httpMessage)
        {
            Wallet.Inst = null;
            if (httpMessage.map["0"] == "importkey")
            {
                if (!GetParam(httpMessage, "1", "1", out string privatekey))
                {
                    httpMessage.result = "command error! \nexample: import key privatekey";
                    return;
                }
                string address = AddKeyBag(privatekey, Program.keyBag);
                httpMessage.result = $" {address}";
            }
            else if (httpMessage.map["0"] == "showip")
            {
                httpMessage.result = JsonHelper.ToJson(Program.ruleIP);
            }
            else if (httpMessage.map["0"] == "openwallet")
            {
                Wallet wallet = new Wallet();
                if (httpMessage.map.Count == 2)
                {
                    httpMessage.map.Add("1", "./smartx-wallet.json");
                }
                if (Program.wallet.keys.Count > 0)
                {
                    httpMessage.result = "please close wallet first!";
                    return;
                }
                try
                {
                    string path = httpMessage.map["1"];
                    Wallet wallet1 = Wallet.GetWallet(path);
                    wallet = wallet1;
                }
                catch (Exception e)
                {
                    httpMessage.result = "Please check your path";
                    return;
                }
                

                if (wallet == null)
                {
                    httpMessage.result = "Password error";
                    return;
                }
                if (Program.keyBag.Count > 0  )
                {
                    if (!Program.keyBag.ContainsKey(wallet.keys[0].ToAddress()))
                    {
                        Program.keyBag.Add(Wallet.ToAddress(wallet.keys[0].publickey), wallet.keys[0]);
                    }
                }
                else 
                {
                    Program.keyBag.Add(Wallet.ToAddress(wallet.keys[0].publickey), wallet.keys[0]);
                }
                Program.wallet = wallet;
                //string random = Program.wallet.keys[0].random.ToHex();
                //string nonce="0";
                string nonce = await GetNonceAsync(Wallet.ToAddress(Program.wallet.keys[0].publickey), httpMessage, Program.ruleIP);//0
                if (nonce == "INVALID URL, set the correct url" || nonce == "false")
                {
                    httpMessage.result = nonce;
                    return;
                }
                Program.nonce = (int.Parse(nonce) + 1).ToString();
                //Console.WriteLine(Program.nonce);
                httpMessage.result = Wallet.ToAddress(Program.wallet.keys[0].publickey) + " open";
            }
            else if (httpMessage.map["0"] == "setwallet")
            {
                if (!GetParam(httpMessage, "1", "address", out string address))
                {
                    httpMessage.result = "command error! \nexample: setwallet address";
                    return;
                }
                WalletKey key = new WalletKey();
                if (Program.keyBag.Count > 0)
                {
                    if (Program.keyBag.ContainsKey(address))
                    {
                        key = Program.keyBag[address];
                    }
                    else
                    {
                        httpMessage.result = "The address isn't in the key bag";
                        return;
                    }
                }
                else
                {
                    httpMessage.result = "The address isn't in the key bag";
                    return;
                }

                if (Program.wallet.keys.Count != 0)
                {
                    httpMessage.result = "please close wallet first!";
                    return;
                }
                Program.wallet.keys.Add(key);
                string nonce = await GetNonceAsync(address, httpMessage, Program.ruleIP);//0
                if (nonce == "INVALID URL, set the correct url" || nonce == "false")
                {
                    httpMessage.result = nonce;
                    return;
                }
                Program.nonce = (int.Parse(nonce) + 1).ToString();
                httpMessage.result = $"{address} set";
            }
            else if (httpMessage.map["0"] == "list")
            {
                List<string> list = new List<string>();
                foreach (KeyValuePair<string, WalletKey> k in Program.keyBag)
                {
                    list.Add(k.Key);
                }
                httpMessage.result = JsonHelper.ToJson(list);
            }
            else if (httpMessage.map["0"] == "closewallet")
            {
                Wallet.Inst = null;
                Program.wallet = new Wallet();
                Program.nonce = "";
                httpMessage.result = "wallet closed";
            }
            else if (httpMessage.map["0"] == "getprivatekey")
            {
                if (Program.wallet.keys.Count != 0)
                {
                    httpMessage.result = JsonHelper.ToJson(Program.wallet.keys[0].random.ToHexString());
                }
                else
                {
                    httpMessage.result = "please set wallet";
                }
            }
            else if (httpMessage.map["0"] == "exportkey")
            {
                if (Program.wallet.keys.Count == 0)
                {
                    httpMessage.result = "please set wallet";
                    return;
                }
                if (httpMessage.map.Count <= 2)
                {
                    File.WriteAllText("./private.json", Program.wallet.keys[0].random.ToHexString());
                    httpMessage.result = $"export key  successful";
                    return;
                }
                else if (Program.wallet.keys.Count > 0)
                {
                    try
                    {
                        File.WriteAllText(httpMessage.map["1"] + "/private.json", Program.wallet.keys[0].random.ToHexString());
                        httpMessage.result = $"export key  successful";
                    }
                    catch (Exception)
                    {
                        httpMessage.result = "Please check the path";
                        return;
                    }
                }
                else
                {
                    httpMessage.result = "Please set the wallet first";
                }
            }
            else if (httpMessage.map["0"] == "clear")
            {
                Console.Clear();
            }
            else if (httpMessage.map["0"] == "transfer")
            {
                if (httpMessage.map.Count <= 3 )
                {
                    Console.WriteLine("transfer error example transfer addressOut amount fee");
                    return;
                }
                if (Program.wallet.keys.Count == 0)
                {
                    httpMessage.result = "Please set the wallet first";
                    return;
                }
                if (!Wallet.CheckAddress(httpMessage.map["1"]))
                {
                    httpMessage.result = "Please check addressOut";
                    return;
                }
                TransferInfo(httpMessage, Program.ruleIP);
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                if (result.result.Contains("true"))
                {
                    Program.nonce = (int.Parse(Program.nonce) + 1).ToString();
                    httpMessage.result =  result.result + " " + httpMessage.map["hash"];
                }
                else
                {
                    httpMessage.result = "false";
                }
                
            }
            else if (httpMessage.map["0"] == "createkey")
            {
                Console.WriteLine("Please enter random word: ");
                string input = Console.ReadLine();
                WalletKey walletKey = new WalletKey();
                walletKey.random = CryptoHelper.Sha256(Seek().ToHexString() + "#" + input).HexToBytes();
                ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, walletKey.random);
                if (walletKey.random != null)
                {
                    Dictionary<string, string> walletinfo = new Dictionary<string, string>();
                    walletinfo.Add("address", walletKey.ToAddress());
                    walletinfo.Add("privatekey", walletKey.random.ToHexString());
                    httpMessage.result = JsonHelper.ToJson(walletinfo);
                    return;
                }
                httpMessage.result = "createkey error";


            }
            else if (httpMessage.map["0"] == "createwallet")
            {
                OnCreat(httpMessage);

            }
            else if (httpMessage.map["0"] == "mempool")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "getlastblock")
            {
                try
                {
                    if (!GetParam(httpMessage, "1", "index", out string index))
                    {
                        httpMessage.result = "command error! \nexample: getlastblock index";
                        return;
                    }
                    string height = await GetHeightAsync();
                    httpMessage.map["1"] = (long.Parse(height) - 19 * long.Parse(httpMessage.map["1"])) != 0 ? (long.Parse(height) - 19 * long.Parse(httpMessage.map["1"])).ToString() : "0";
                    HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                    httpMessage.result = result.result;
                } catch (Exception e)
                {
                    httpMessage.result = "command error! \nexample: getlastblock index";
                }
            }
            else if (httpMessage.map["0"] == "transfercount")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "search")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "miner")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "node")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "beruler")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "rules")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "stats")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "account")
            {
                HttpMessage result = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
                httpMessage.result = result.result;
            }
            else if (httpMessage.map["0"] == "help")
            {
                OnHelp(httpMessage);
            }
            else
            {
                httpMessage.result = "command error";
            }
        }

        private static async System.Threading.Tasks.Task<string> GetHeightAsync()
        {
            Dictionary<string, object> latestBlockHeight = new Dictionary<string, object>();
            latestBlockHeight.Add("success", true);
            latestBlockHeight.Add("message", "successful operation");
            latestBlockHeight.Add("height", "");
            HttpMessage temp = new HttpMessage();
            temp.map = new Dictionary<string, string>();
            temp.map.Add("cmd", "latest-block-height");
            temp = await ComponentNetworkHttp.QueryCommand(Program.ruleIP + $"/{temp.map["cmd"]}", temp);
            var temp1 = JsonHelper.FromJson<Dictionary<string, object>>(temp.result);
            return (string)temp1["height"];
        }

        private static string AddKeyBag(string randomseed, Dictionary<string, WalletKey> keyBag)
        {
            WalletKey walletKey = new WalletKey();
            ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, randomseed.HexToBytes());
            string address = Wallet.ToAddress(walletKey.publickey);
            walletKey.random = randomseed.HexToBytes();
            if (Program.keyBag.Count > 0)
            {
                if (keyBag.ContainsKey(address))
                {
                    return "The address already exists!";
                }
            }
            else
            {
                if (!Program.keyBag.ContainsKey(address)) keyBag.Add(address, walletKey);
            }
            
           
            return address;
        }

        private static void TransferInfo(HttpMessage httpMessage, string ruleIP)
        {
            string timestamp = (TimeHelper.Now()).ToString();
            string cmd = "transfer";//1
            string type = cmd;//2
            string nonce = Program.nonce;
            string addressIn = Wallet.ToAddress(Program.wallet.keys[0].publickey);//3
            string addressOut = httpMessage.map["1"];//4
            string amount = (float.Parse(httpMessage.map["2"]) * 10000).ToString();//5
            string data = "";//6
            string fee = (httpMessage.map.Count == 5 ? httpMessage.map["3"] : "0");
            string depend = "";//7
            string hashdata = "transfer#" + nonce + "#" + addressIn + "#" + addressOut + "#" + amount + "#" + data + "#" + depend + "#" + timestamp;
            string hash = CryptoHelper.Sha256(hashdata);//8
            string sign = GetSign(hash).ToLower();//9
            httpMessage.map.Clear();
            httpMessage.map.Add("cmd", cmd);
            httpMessage.map.Add("type", type);
            httpMessage.map.Add("hash", hash);
            httpMessage.map.Add("nonce", nonce);
            httpMessage.map.Add("addressIn", addressIn);
            httpMessage.map.Add("addressOut", addressOut);
            httpMessage.map.Add("amount", amount);
            httpMessage.map.Add("data", data);
            httpMessage.map.Add("depend", depend);
            httpMessage.map.Add("sign", sign);
            httpMessage.map.Add("fee", fee);
            httpMessage.map.Add("timestamp", timestamp);
        }

        private static string GetSign(string hash)
        {
            string address = Wallet.ToAddress(Program.wallet.keys[0].publickey);
            string sign = Wallet.Sign(hash.HexToBytes(), Program.wallet.keys[0]).ToHex();
            return sign;
        }

        private static async System.Threading.Tasks.Task<string> GetNonceAsync(string address, HttpMessage httpMessage, string ruleIP)
        {
            httpMessage.map["cmd"] = "getnonce";
            httpMessage.map["address"] = address;
            HttpMessage result = await ComponentNetworkHttp.QueryCommand(ruleIP + $"/{httpMessage.map["cmd"]}", httpMessage);
            if (result.result == "INVALID URL, set the correct url" || result.result == "false") return result.result;
            Dictionary<string, string> nonce = JsonHelper.FromJson<Dictionary<string, string>>(result.result);
            string nonc = nonce["nonce"];
            return nonc;
        }

        private static bool GetParam(HttpMessage httpMessage, string key1, string key2, out string value)
        {
            if (!httpMessage.map.TryGetValue(key1, out value))
            {
                if (!httpMessage.map.TryGetValue(key2, out value))
                    return false;
            }
            return true;
        }
        static public byte[] Seek()
        {
            byte[] seed = new byte[32];
            ed25519.ed25519_create_seed(seed);
            return seed;
        }
        private static void OnCreat(HttpMessage httpMessage)
        {
            try
            {
                Console.Write("please input your password:");
                string passwd = Console.ReadLine();
                Wallet wallet = new Wallet();
                wallet = wallet.NewWallet(passwd);
                if (httpMessage.map.Count == 3)
                {
                    wallet.walletFile = httpMessage.map["1"] + "/wallet.json";
                }
                else
                {
                    wallet.walletFile = "./wallet.json";
                }
                wallet.SaveWallet();
                if (wallet.keys.Count > 0)
                {
                    httpMessage.result = JsonHelper.ToJson(wallet.keys[0].ToAddress());
                    return;
                }
                httpMessage.result = "create account error";
            }
            catch (Exception e)
            {
                httpMessage.result = "path error";

            }
            
        }
        private static void OnHelp(HttpMessage httpMessage)
        {
            Dictionary<string, string> c = new Dictionary<string, string>();
            c.Add("importkey privatekey", "Users can import their private keys into nodeshell to recover their wallets");
            c.Add("createwallet path", "The path parameter is the wallet path, which can be used to create a wallet file");
            c.Add("createkey", "Public and private key pairs can be created");
            c.Add("transfercount address", "Returns all transactions at an address");
            c.Add("getlastblock index", "Index: the page number of the most recently generated block, and returns the most recently generated main block");
            c.Add("mempool", "Returns unprocessed transactions in the memory pool");
            c.Add("search hash / address", "Search main block information, transaction information or address transaction information");
            c.Add("miner", "Information about miners mining");
            c.Add("node", "Return the valid node information that is connecting in smartx P2P network");
            c.Add("beruler", "With a certain number of SAT coins, you can send a transaction through this command to become a POS verification node");
            c.Add("rules", "Returns the current POS verification node information of smartx network");
            c.Add("stats", "Current network statistics");
            c.Add("account address", "Return the current address, including balance information");
            c.Add("transfer addressOut amount fee", "Addressout: transfer out address ,amount: transfer out amount ,fee: service charge and return the transfer result");
            c.Add("exportkey path", "Export default wallet to path");
            c.Add("getprivatekey", "Get the current default wallet private key");
            c.Add("closewallet", "Cancel current Wallet");
            c.Add("list", "List all wallets in the system");
            c.Add("setwallet address", "Set current Wallet");
            c.Add("openwallet path", "Open and set the current Wallet");
            c.Add("help", "Display executable commands");
            c.Add("clear", "Clear current  screen");
            c.Add("showip", "Displays the IP address of the current computer connection");
            httpMessage.result = JsonHelper.ToJson(c);
        }

        



    }
}
