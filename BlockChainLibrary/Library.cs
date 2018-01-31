using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainLibrary
{
    [SerializableAttribute]
    public class Block
    {
        public int Id;
        public string Time;
        public string prevHash;
        public Ledger BlockLedger = new Ledger();
        public string Hash;
        public int MinedTime = 0;

        [SerializableAttribute]
        public class Ledger
        {
            public int TransactionId = 0;
            

            private List<Tuple<int, string, string, double,string>> Lines = new List<Tuple<int, string, string, double,string>>();

            public void AddLine(string Sender, string Receiver, double Amount,string Signature)
            {
                Lines.Add(new Tuple<int, string, string, double,string>(TransactionId, Sender, Receiver, Amount,Signature));
                TransactionId++;
            }
            public Double GetTotalAmount()
            {
                Double i = 0;
                foreach (var Line in Lines)
                {
                    i += Line.Item4;
                }
                return i;
            }
        }

        public Block(int _Id, string _Time, Ledger _Ledger, string _prevHash)
        {
            Id = _Id;
            Time = _Time;
            BlockLedger = _Ledger;
            prevHash = _prevHash;
            Hash = CalculateHash();
        }

        public string CalculateHash()
        {
            return SHA256Hash(Id + Time + BlockLedger + prevHash + Hash + MinedTime);
        }

        public void MineBlock(int difficulty)
        {
            while (Hash.Substring(0, difficulty).Length != Hash.ToCharArray().Count(c => c == '0'))
            {
                MinedTime++;
                Hash = CalculateHash();
            }
        }

        public string SHA256Hash(string value)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                return String.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(value)));
            }
        }
    }
    [SerializableAttribute]
    public class BlockChain
    {
        public List<Block> Chain = new List<Block>();
        public int difficulty = 10;

        public void CreateGenesis()
        {
            Chain.Add(new Block(0, "24/01/18", new Block.Ledger(), "0"));

        }
        public Block GetLatest()
        {
            return Chain.Last();
        }

        public void AddBlock(Block block)
        {
            if (block.Id == Chain.Count)
            {
                //block.MineBlock(difficulty);
                Chain.Add(block);
            }

        }



        public bool IsGoodChain()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                if (Chain[i].prevHash != Chain[i - 1].Hash)
                {
                    return false;
                }
                if (Chain[i].Hash != Chain[i].CalculateHash())
                {
                    return false;
                }
            }
            return true;
        }

    }
    [SerializableAttribute]
    public class MessageUDP
    {
        public byte[] Data { get; set; }
    }
    [SerializableAttribute]
    public class Command
    {
        public int CommandId;
        public Command(int _CommandId)
        {
            CommandId = _CommandId;
        }

    }
    public static class Function
    {

        public static MessageUDP Serialize(object anySerializableObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, anySerializableObject);
                return new MessageUDP { Data = memoryStream.ToArray() };
            }
        }

        public static object Deserialize(MessageUDP message)
        {
            using (var memoryStream = new MemoryStream(message.Data))
                return (new BinaryFormatter()).Deserialize(memoryStream);
        }

    }
}
