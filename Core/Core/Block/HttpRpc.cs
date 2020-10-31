using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ETModel
{
    // 外网连接
    public class HttpRpc : Component
    {
        ComponentNetworkHttp networkHttp;

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);

        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"HttpRpc http://{networkHttp.ipEndPoint}/");

        }

        public void OnHttpMessage(Session session, int opcode, object msg)
        {
            HttpMessage httpMessage = msg as HttpMessage;
            if (httpMessage == null || httpMessage.request == null || httpMessage.request.LocalEndPoint.ToString() != networkHttp.ipEndPoint.ToString())
                return;

            string cmd = httpMessage.map["cmd"].ToLower();
            switch (cmd)
            {
                case "account":
                    OnAccount(httpMessage);
                    break;
                case "stats":
                    OnStats(httpMessage);
                    break;
                case "rules":
                    OnRules(httpMessage);
                    break;
                case "transfer":
                    OnTransfer(httpMessage);
                    break;
                case "transferstate":
                    OnTransferState(httpMessage);
                    break;
                case "beruler":
                    OnBeRuler(httpMessage);
                    break;
                case "getmnemonicword":
                    GetMnemonicWord(httpMessage);
                    break;
                case "command":
                    Command(httpMessage);
                    break;
                case "gettransfers":
                    GetTransfers(httpMessage);
                    break;
                case "getaccounts":
                    GetAccounts(httpMessage);
                    break;
                case "getnearblock":
                    GetNearBlock(httpMessage);
                    break;
                case "node":
                    OnNode(httpMessage);
                    break;
                case "getblock":
                    GetBlock(httpMessage);
                    break;
                default:
                    break;
            }
        }

        // console 支持
        public void Command(HttpMessage httpMessage)
        {
            httpMessage.result = "unknow command please check";

            string input = httpMessage.map["input"];
            input = input.Replace("%20", " ");
            input = input.Replace("  ", " ");
            input = input.Replace("   ", " ");
            input = input.Replace("  ", " ");

            string[] array = input.Split(' ');

            if (input.IndexOf("-") != -1)
            {
                for (int ii = 1; ii < array.Length; ii++)
                {
                    string[] arrayValue = array[ii].Split(':');
                    if (arrayValue != null && arrayValue.Length >= 1)
                    {
                        httpMessage.map.Remove(arrayValue[0].Replace("-", ""));
                        httpMessage.map.Add(arrayValue[0].Replace("-", ""), arrayValue.Length >= 2 ? arrayValue[1] : "");
                        Console.WriteLine(httpMessage.map.TryGetValue(array[0],out string value));
                    }
                }
            }
            else
            {
                for (int ii = 1; ii < array.Length; ii++)
                {
                    string arrayValue = array[ii];
                    httpMessage.map.Remove("" + ii);
                    httpMessage.map.Add("" + ii, arrayValue);
                }
            }
            string cmd = array[0].ToLower();
            switch (cmd)
            {
                case "account":
                    AccountInfo(httpMessage);//获取地址和余额
                    break;
                case "balance":
                    balanceInfo(httpMessage);
                    break;
                case "stats":
                    statsinfo(httpMessage); //高度和计算能力和地址和余额和节点数
                    break;
                case "rules":
                    rulesInfo(httpMessage); //获取id地址种节点时间
                    break;
                case "rulers":
                    RulersInfo(httpMessage);//地址 起止
                    break;
                //case "transfer":
                //    OnTransfer(httpMessage);
                //    break;
                case "transferstate":    
                    TransferStateInfo(httpMessage);// 钱包出块数据
                    break;
                case "transferblock":
                    TransferBlockInfo(httpMessage);// 钱包中区块的数据 带hash
                    break;
                case "beruler":
                    BerulerInfo(httpMessage);//成为裁决节点 可以打包数据
                    break;
                case "getmnemonic":
                    GetMneonicInfo(httpMessage);  //获取助记词
                    break;
                case "node":
                    nodeInfo(httpMessage); //节点信息
                    break;
                case "getblock":
                    //获取区块信息
                    getBlockInfo(httpMessage); //获取区块信息 需要hash值
                    break;
               
                case "getnearblock":
                    getNearBlockInfo(httpMessage); //获取区块第一页面的信息
                    break;
                case "test":
                    Test(httpMessage);
                    break;
                case "delblock":
                    DelBlock(httpMessage);
                    break;
                case "hello":
                    {
                        httpMessage.result = "welcome join smartx";
                    }
                    break;
                case "help":
                    {
                        httpMessage.result = "account\nstats\nrules\nrulers\ntransferstate\ntransferblock\nberuler\ngetmnemonic\nnode\ngetblock\ngetaddress\ngetnearblock\n";
                    }
                    break;
                //case "importwallet":
                //    {
                //        Console.Write("please input your password:");
                //        string passwd = Console.ReadLine();
                //        Program.wallet = new Wallet();
                //        Program.wallet.walletFile = "./wallet.dat";
                //        if (1 != Program.wallet.OpenWallet(passwd))
                //        {
                //            httpMessage.result =  "please check your password";
                //            return;
                //        }

                //        WalletKey key = Program.wallet.GetCurWallet();
                //        string address = Wallet.ToAddress(key.publickey);
                //        httpMessage.result = $"publickey   {key.publickey.ToHexString()}\nprivatekey {key.privatekey.ToHexString()}\nAddress    {address}";
                //    }
                //    break;
                case "create":
                    Oncreat(httpMessage);
                    break;
                case "exit":
                    {
                        Environment.Exit(0);
                    }
                    break;
                default:
                    break;

            }

            //httpMessage.result = "ok";
        }

        private void Oncreat(HttpMessage httpMessage)
        {
            Console.Write("please input your password:");
            string passwd = Console.ReadLine();
            Wallet wallet = new Wallet();
            wallet = wallet.NewWallet(passwd);
            wallet.walletFile = "./wallet.dat";
            wallet.SaveWallet();
            if (wallet.keys.Count > 0)
            {
                httpMessage.result = wallet.keys[0].ToAddress();
                return;
            }
            httpMessage.result = "create account error";
        }
        private void balanceInfo(HttpMessage httpMessage)
        {
            string url, ip, port, address;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            httpMessage.map.TryGetValue("1", out address);
            if (address == null) httpMessage.result = "Command error example balance address";
            else
            {
                url = "http://" + ip + ":" + port + "/Command?input=account%20" + address;
                httpMessage.result = HttpGet(url);
                string[] arrayValue = httpMessage.result.Split(",");
                httpMessage.result = arrayValue[1];
            }
        }

        private string correctData(string str)
        {

            if(!str.Contains("["))
            {
                str = str.Replace("{", "[{");
                str = str.Replace("}", "}]");
            }
            var list = JsonHelper.FromJson<List<Transfer>>(str);
            for(int i=0;i<list.Count;i++)
            {
                list[i].amount = (int.Parse(list[i].amount) / 10000).ToString();
            }
            str = JsonHelper.ToJson(list);
            return str;
        }

        private string ConvertJsonString(string str)
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

        private void rulesInfo(HttpMessage httpMessage)
        {
            string url, ip, port;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            url = "http://" + ip + ":" + port + "/Command?input=rules";
            httpMessage.result = HttpGet(url);
            
        }

        private void TransferBlockInfo(HttpMessage httpMessage)
        {
            string url, ip, port, hash;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            httpMessage.map.TryGetValue("1", out hash);
            if (hash == null) httpMessage.result = "Command error example transferblock hash";
            else
            {
                url = "http://" + ip + ":" + port + "/TransferState?hash=" + hash;
                httpMessage.result = HttpGet(url);
                
                httpMessage.result = correctData(httpMessage.result);
                httpMessage.result = ConvertJsonString(httpMessage.result);
            }
        }

        private void getBlockInfo(HttpMessage httpMessage)
        {
            string url, ip, port, hash;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            httpMessage.map.TryGetValue("1", out hash);
            if (hash == null) httpMessage.result = "Command error example getblock hash";
            else
            {
                url = "http://" + ip + ":" + port + "/GetBlock?hash=" + hash;
                httpMessage.result = HttpGet(url);
                httpMessage.result = ConvertJsonString(httpMessage.result);
            }
        }

        private void TransferStateInfo(HttpMessage httpMessage)
        {
            string url, ip, port, num,address;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            httpMessage.map.TryGetValue("1", out address);
            httpMessage.map.TryGetValue("2", out num);
            if (num == null || address==null) httpMessage.result = "Command error example transfer address index";
            else
            {
                url = "http://" + ip + ":" + port + "/GetTransfers?address=" + address + "&index=" + (20 * int.Parse(num)).ToString();
                httpMessage.result = HttpGet(url);
                httpMessage.result = correctData(httpMessage.result);
                httpMessage.result = ConvertJsonString(httpMessage.result);
            }
        }

        private string getPoolHeight(HttpMessage httpMessage)
        {
            statsinfo(httpMessage);
            string[] array = httpMessage.result.Split('\n');
            string[] array1 = array[1].Split(':');
            string poolHeight = array1[1];
            httpMessage.result = poolHeight;
            return poolHeight;
        }

        private void getNearBlockInfo(HttpMessage httpMessage)
        {
            string url, ip, port,num;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            httpMessage.map.TryGetValue("1", out num);
            if (num == null) httpMessage.result = "Command error example getnearblock num";
            else
            {
                num = (int.Parse(getPoolHeight(httpMessage)) - int.Parse(num) * 21).ToString();
                url = "http://" + ip + ":" + port + "/GetNearBlock?browserIndex=" + num;
                httpMessage.result = HttpGet(url);
                httpMessage.result = ConvertJsonString(httpMessage.result);
            }
        }

        private void AccountInfo(HttpMessage httpMessage)
        {
            string url, ip, port, address;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            httpMessage.map.TryGetValue("1", out address);
            if (address == null) httpMessage.result = "Command error example account address";
            else
            {
                url = "http://" + ip + ":" + port + "/Command?input=account%20" + address;
                httpMessage.result = HttpGet(url);
            }
        }

        private void BerulerInfo(HttpMessage httpMessage)
        {
            string url, ip, port;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            url = "http://" + ip + ":" + port + "/Command?input=beruler";
            httpMessage.result = HttpGet(url);
        }

        private void RulersInfo(HttpMessage httpMessage)
        {
            string url, ip, port;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            url = "http://" + ip + ":" + port + "/Command?input=rules";
            httpMessage.result = HttpGet(url);
        }

        private string GetMneonicInfo(HttpMessage httpMessage)
        {
            string url, ip, port, key;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            httpMessage.map.TryGetValue("1", out key);
            if (key == null || key == "") return httpMessage.result = "Command Error example getmneonic password";
            url = "http://" + ip + ":" + port + "/Command?input=getmnemonic%20" + key;
            httpMessage.result = HttpGet(url);
            return httpMessage.result;
        }

        private string statsinfo(HttpMessage httpMessage)
        {
            string url, ip, port;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            url = "http://" + ip + ":" + port + "/Command?input=stats";
            httpMessage.result = HttpGet(url).Replace("Alppy","Apply");
            return httpMessage.result;
        }

        public string nodeInfo(HttpMessage httpMessage)
        {
            string url, ip, port;
            httpMessage.map.TryGetValue("ip", out ip);
            httpMessage.map.TryGetValue("port", out port);
            url = "http://" + ip + ":" + port + "/Node?get=all";
            //Console.WriteLine(url);
            httpMessage.result = HttpGet(url);
            return httpMessage.result;
            //Console.WriteLine(result);
            //resultDeal(result);
        }

        private string HttpGet(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
            string result = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            return result;
        }


        public void OnStats(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "style", out string style))
            {
                style = "";
            }

            string address = Wallet.GetWallet().GetCurWallet().ToAddress();
            Account account = null;
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                account = dbSnapshot.Accounts.Get(address);
            }

            var amount = account != null ? BigInt.Div(account.amount , "10000").ToString() : "0";
            long.TryParse(Entity.Root.GetComponent<LevelDBStore>().Get("UndoHeight"), out long UndoHeight);
            long PoolHeight = Entity.Root.GetComponent<Rule>().height;
            string power1 = Entity.Root.GetComponent<Rule>().calculatePower.GetPower();
            string power2 = Entity.Root.GetComponent<Consensus>().calculatePower.GetPower();

            int NodeCount = Entity.Root.GetComponent<NodeManager>().GetNodeCount();

            if (style == "")
            {
                httpMessage.result = $"      AlppyHeight: {UndoHeight}\n" +
                                        $"       PoolHeight: {PoolHeight}\n" +
                                        $"  Calculate Power: {power1} of {power2}\n" +
                                        $"          Account: {address}, {amount}\n" +
                                        $"             Node: {NodeCount}";
            }
            else
            {
                httpMessage.result = $"H:{UndoHeight} P:{power2}";
            }
        }

        public void OnRules(HttpMessage httpMessage)
        {
            Dictionary<string, RuleInfo> ruleInfos = Entity.Root.GetComponent<Consensus>().ruleInfos;
            httpMessage.result = JsonHelper.ToJson(ruleInfos);
        }


        public void OnNode(HttpMessage httpMessage)
        {
            var nodes = Entity.Root.GetComponent<NodeManager>().GetNodeList();
            nodes.Sort((a, b) => b.kIndex - a.kIndex);
            httpMessage.result = JsonHelper.ToJson(nodes);
        }

        public void GetAccounts(HttpMessage httpMessage)
        {
            var buffer = Base58.Decode(httpMessage.map["List"]).ToStr();
            var list = JsonHelper.FromJson<List<string>>(buffer);

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var accounts = new Dictionary<string, Account>();
                for (int i = 0; i < list.Count; i++)
                {
                    Account account = dbSnapshot.Accounts.Get(list[i]);
                    if (account == null)
                    {
                        account = new Account() { address = list[i], amount = "0", index = 0, nonce = 0 };
                    }
                    accounts.Remove(account.address);
                    accounts.Add(account.address, account);
                }
                httpMessage.result = JsonHelper.ToJson(accounts);
            }
        }

        public void OnAccount(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "Address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(address);
                if (account != null)
                    httpMessage.result = $"          Account: {account.address}, amount:{BigInt.Div(account.amount , "10000")} , index:{account.index}";
                else
                    httpMessage.result = $"          Account: {address}, amount:0 , index:0";
            }
        }

        // web发送交易接收处理
        public void OnTransfer(HttpMessage httpMessage)
        {
            Transfer transfer = new Transfer();
            transfer.hash = httpMessage.map["hash"];
            transfer.type = httpMessage.map["type"];
            transfer.nonce = long.Parse(httpMessage.map["nonce"]);
            transfer.addressIn = httpMessage.map["addressIn"];
            transfer.addressOut = httpMessage.map["addressOut"];
            transfer.amount = httpMessage.map["amount"];
            transfer.data = httpMessage.map["data"];
            transfer.sign = httpMessage.map["sign"].HexToBytes();
            transfer.depend = httpMessage.map["depend"];

            //string hash = transfer.ToHash();
            //string sign = transfer.ToSign(Wallet.GetWallet().GetCurWallet()).ToHexString();

            var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
            if (rel==-1)
            {
                // 这里没有加await 意味是这个协程 by sxh
                OnTransferAsync(transfer);
            }

            httpMessage.result = "accepted";
        }

        public async void OnTransferAsync(Transfer transfer)
        {
            Consensus consensus = Entity.Root.GetComponent<Consensus>();

            Q2P_Transfer q2p_Transfer = new Q2P_Transfer();
            q2p_Transfer.transfer = JsonHelper.ToJson(transfer);

            var networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
            var nodeList = Entity.Root.GetComponent<NodeManager>().GetNodeList();

            // 遍历node提交交易，直到找个一个可以出块的节点
            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = nodeList[i];
                Session session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
                if (session != null && session.IsConnect())
                {
                    var r2p_Transfer = (R2P_Transfer)await session.Query(q2p_Transfer, 5);
                    if (r2p_Transfer != null && r2p_Transfer.rel != "-1")
                    {
                        break;
                    }
                }
            }
        }

        public bool GetParam(HttpMessage httpMessage,string key1,string key2,out string value)
        {
            if (!httpMessage.map.TryGetValue(key1, out value))
            {
                if (!httpMessage.map.TryGetValue(key2, out value))
                    return false;
            }
            return true;
        }

        public void GetTransfers(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "address", out string address)) {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }
            if (!GetParam(httpMessage, "2", "index", out string indexStr)) {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }

            long.TryParse(indexStr, out long getIndex);

            var transfers = new List<Transfer>();
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(address);

                if (account != null)
                {
                    getIndex = account.index - getIndex;
                    for (long ii = getIndex; ii > getIndex - 20 && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.TFA.Get($"{address}_{ii}");
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                transfers.Add(transfer);
                            }
                        }
                    }
                }

                httpMessage.result = JsonHelper.ToJson(transfers);
            }
        }

        public void OnBeRuler(HttpMessage httpMessage)
        {
            Consensus consensus = Entity.Root.GetComponent<Consensus>();
            WalletKey key = Wallet.GetWallet().GetCurWallet();

            var  address = key.ToAddress();
            long nonce  = 1;
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(address);
                if (account != null)
                {
                    nonce = account.nonce + 1;
                }
            }

            Transfer transfer = new Transfer();
            transfer.addressIn = address;
            transfer.addressOut = consensus.consAddress;
            transfer.amount = "0";
            transfer.nonce = nonce;
            transfer.type = "contract";

            LuaVMCall luaVMCall = new LuaVMCall();
            luaVMCall.fnName = "Add";
            luaVMCall.args = new FieldParam[0];
            //luaVMCall.args[0] = new FieldParam();
            //luaVMCall.args[0].type = "Int64";
            //luaVMCall.args[0].value = "999";
            //long aaa = (long)luaVMCall.args[0].GetValue();
            transfer.data = luaVMCall.Encode();

            transfer.hash = transfer.ToHash();
            transfer.sign = transfer.ToSign(key);

            var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
            if (rel == -1)
            {
                OnTransferAsync(transfer);
            }
            httpMessage.result = $"accepted transfer:{transfer.hash}";
        }

        public void OnTransferState(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "command error! \nexample: transferstate hash";
                return;
            }

            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var transfer = dbSnapshot.Transfers.Get(hash);
                if (transfer != null)
                    httpMessage.result = JsonHelper.ToJson(transfer);
                else
                    httpMessage.result = "";
            }
        }


        public void GetMnemonicWord(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "passwords", out string passwords))
            {
                httpMessage.result = "command error! \nexample: getmnemonic password";
                return;
            }

            var walletKey = Wallet.GetWallet().GetCurWallet();
            string randomSeed = CryptoHelper.Sha256(walletKey.random.ToHexString() + "#" + passwords);
            httpMessage.result = randomSeed;
        }

        public void GetNearBlock(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "browserIndex", out string browserIndexStr))
            {
                httpMessage.result = "command error! \nexample: GetNearBlock browserIndex";
                return;
            }
            long.TryParse(browserIndexStr, out long browserIndex);

            List<Block> list = new List<Block>();
            // 高度差太大的块直接忽略
            var consensus = Entity.Root.GetComponent<Consensus>();
            var blockMgr  = Entity.Root.GetComponent<BlockMgr>();
            var rule      = Entity.Root.GetComponent<Rule>();
            var showCount = 19;

            var height = browserIndex != 0 ? browserIndex : consensus.transferHeight;
            for (long ii = height; ii > height - showCount && ii > 0; ii--)
            {
                Block myblk = BlockChainHelper.GetMcBlock(ii);
                if(myblk!=null)
                    list.Add(myblk);
            }

            if (consensus.transferHeight <= height&& rule.state!=0)
            {
                var last = rule.GetLastMcBlock();
                if (last != null)
                {
                    var pre = blockMgr.GetBlock(last.prehash);
                    if (pre.height != consensus.transferHeight)
                    {
                        list.Insert(0, pre);
                        if(list.Count> showCount)
                            list.RemoveAt( list.Count-1 );
                    }
                    list.Insert(0, last);
                    if (list.Count> showCount)
                        list.RemoveAt(list.Count - 1);
                }
            }

            List<Block> list2 = new List<Block>();
            for (int ii = 0; ii < list.Count ; ii++)
            {
                Block blk = list[ii].GetHeader();
                list2.Add(blk);
            }

            httpMessage.result = JsonHelper.ToJson(list2);
        }

        public void GetBlock(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "command error! \nexample: GetBlock hash";
                return;
            }
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var blk = blockMgr.GetBlock(hash);
            if(blk!=null)
                httpMessage.result = JsonHelper.ToJson(blk);
            else
                httpMessage.result = "";
        }

        public void Test(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "style", out string style))
            {
                httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                return;
            }

            httpMessage.result = "";
            if (style == "1")
            {
                if (!GetParam(httpMessage, "2", "Address", out string Address))
                {
                    httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                    return;
                }
                if (!GetParam(httpMessage, "3", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                    return;
                }

                LevelDBStore.Export2CSV_Transfer($"{file}", Address);

            }
            else
            if (style == "2")
            {
                OneThreadSynchronizationContext.Instance.Post(this.Test2Async, null);

            }
            else
            if (style == "3")
            {
                OneThreadSynchronizationContext.Instance.Post(this.Test3Async, null);

            }
            else
            if (style == "4")
            {
                LevelDBStore.Export2CSV_Accounts($"C:\\Accounts_test4.csv");
            }
            else
            if (style == "5")
            {
                if (!GetParam(httpMessage, "2", "Address", out string Address))
                {
                    httpMessage.result = "command error! \nexample: test 5 Address C:\\Address.csv";
                    return;
                }
                if (!GetParam(httpMessage, "3", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test 5 Address C:\\Address.csv";
                    return;
                }
                LevelDBStore.Export2CSV_Account($"{file}", Address);
            }
            else
            if (style == "rule")
            {
                TestBeRule(httpMessage);
            }

        }

        public long DelBlock_min;
        public long DelBlock_max;
        public void DelBlock(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!GetParam(httpMessage, "1", "from", out string from))
            {
                httpMessage.result = "command error! \nexample: DelBlock 100 1";
                return;
            }
            if (!GetParam(httpMessage, "2", "to", out string to))
            {
                httpMessage.result = "command error! \nexample: DelBlock 1 100 ";
                return;
            }

            DelBlock_max = Math.Max(long.Parse(from), long.Parse(to));
            DelBlock_min = Math.Min(long.Parse(from), long.Parse(to));

            Entity.Root.GetComponent<Consensus>().AddRunAction(DelBlockAsync);
        }

        public void DelBlockAsync()
        {
            long max = DelBlock_max;
            long min = DelBlock_min;

            Log.Info($"DelBlock {min} {max}");

            Entity.Root.GetComponent<Consensus>().transferHeight = min;
            Entity.Root.GetComponent<LevelDBStore>().UndoTransfers(min);
            for (long ii = max; ii > min; ii--)
            {
                Entity.Root.GetComponent<BlockMgr>().DelBlock(ii);
            }

            Log.Info("DelBlock finish");
        }

        Dictionary<string, long> AccountNotice = new Dictionary<string, long>();
        public long GetAccountNotice(string address,bool reset=true)
        {
            long nonce = 0;
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(address);
                if (!AccountNotice.TryGetValue(address, out nonce))
                {
                    if (account != null)
                        nonce = account.nonce;
                }

                // 中间有交易被丢弃了
                if (reset && account != null && account.nonce < nonce - 20)
                {
                    nonce = account.nonce;
                }

                nonce += 1;

                AccountNotice.Remove(address);
                AccountNotice.Add(address, nonce);
            }
            return nonce;
        }

        public async ETTask<Session> OnTransferAsync2(Transfer transfer ,Session session2)
        {
            Consensus consensus = Entity.Root.GetComponent<Consensus>();

            Q2P_Transfer q2p_Transfer = new Q2P_Transfer();
            q2p_Transfer.transfer = JsonHelper.ToJson(transfer);

            var networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
            var nodeList = Entity.Root.GetComponent<NodeManager>().GetNodeList();

            // 遍历node提交交易，直到找个一个可以出块的节点
            int start = RandomHelper.Random();
            for (int i = start; i < start + nodeList.Count; i++)
            {
                var node = nodeList[ i % nodeList.Count ];
                Session session = session2 ?? await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
                if (session != null && session.IsConnect())
                {
                    var r2p_Transfer = (R2P_Transfer)await session.Query(q2p_Transfer, 5);
                    if (r2p_Transfer != null && r2p_Transfer.rel != "-1")
                    {
                        return session;
                    }
                }
            }
            await Task.Delay(10);
            return null;
        }


        public async void Test2Async(object o)
        {
            NodeManager nodeManager = Entity.Root.GetComponent<NodeManager>();

            for (int ii = Wallet.GetWallet().keys.Count; ii < 1000; ii++)
            {
                Wallet.GetWallet().Create();
            }
            Wallet.GetWallet().SaveWallet();

            Log.Info("Test2Async start1");

            Session session2 = null;
            for (int ii = 1; ii < 1000; ii++)
            {
                int random1 = 0;
                int random2 = ii;
                int random3 = 1000 * 10000;

                Transfer transfer = new Transfer();
                transfer.type = "tranfer";
                transfer.addressIn = Wallet.GetWallet().keys[random1].ToAddress();
                transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                transfer.amount = random3.ToString();
                transfer.data = "";
                transfer.depend = "";
                transfer.nonce = GetAccountNotice(transfer.addressIn, false);
                transfer.hash = transfer.ToHash();
                transfer.sign = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                session2 = await OnTransferAsync2(transfer, session2);
                while (session2 == null) {
                    session2 = await OnTransferAsync2(transfer, session2);
                };
            }

        }

        public async void Test3Async(object o)
        {
            Session session2 = null;

            var accountCount = Wallet.GetWallet().keys.Count;
            while (true)
            {
                Log.Info("Test2Async 200");
                session2 = null;
                for (int ii = 0; ii < 200; ii++)
                {
                    int random1 = RandomHelper.Range(0, accountCount);
                    int random2 = RandomHelper.Range(0, accountCount);
                    while(random1== random2)
                        random2 = RandomHelper.Range(0, accountCount);
                    int random3 = RandomHelper.Range(1, 100) * 10000;

                    Transfer transfer   = new Transfer();
                    transfer.type       = "tranfer";
                    transfer.addressIn  = Wallet.GetWallet().keys[random1].ToAddress();
                    transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                    transfer.amount = random3.ToString();
                    transfer.data   = "";
                    transfer.depend = "";
                    transfer.nonce = GetAccountNotice(transfer.addressIn);
                    transfer.hash   = transfer.ToHash();
                    transfer.sign   = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                    session2 = await OnTransferAsync2(transfer, session2);
                    while (session2 == null)
                    {
                        session2 = await OnTransferAsync2(transfer, session2);
                    };
                }
                await Task.Delay(1000);
            }
        }

        public async void TestBeRule(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            await Task.Delay(10);

        }

    }


}