using laba3_vote.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace laba3_vote.Models
{
    public class CVK
    {
        UnicodeEncoding byteConverter = new UnicodeEncoding();
        private Tuple<string, string> keys = null;
        public List<Person> People { get; set; } = new List<Person>();
        public List<Token> Tokens { get; set; } = new List<Token>();
        private static RSACryptoServiceProvider rsa;

        public List<Vote> Votes { get; set; } = new List<Vote>();

        private string personsPath;
        private string votesPath; 
        private string tokensPath;
        private string bulletinDividersPath;
        private string bulletinApplicantsPath;

        public CVK()
        {
            personsPath = @"Persons.json";
            votesPath = @"Votes.json";
            tokensPath = @"Tokens.json";
            keys = GenerateElgamalKeys();
        }


        public void Load()
        {
            FileInfo fi = new FileInfo(personsPath);
            FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);

            using (StreamReader r = new StreamReader(fs))
            {
                string json = r.ReadToEnd();
                if (json != "")
                {
                    People = JsonSerializer.Deserialize<List<Person>>(json);
                }
            }

            fi = new FileInfo(votesPath);
            fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);

            using (StreamReader r = new StreamReader(fs))
            {
                string json = r.ReadToEnd();
                if (json != "")
                {
                    Votes = JsonSerializer.Deserialize<List<Vote>>(json);
                }
            }

            fi = new FileInfo(tokensPath);
            fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);

            using (StreamReader r = new StreamReader(fs))
            {
                string json = r.ReadToEnd();
                if (json != "")
                {
                    Tokens = JsonSerializer.Deserialize<List<Token>>(json);
                }
            }

        }

        public void Save()
        {
            string jsonPeople = JsonSerializer.Serialize(People);
            File.WriteAllText(personsPath, jsonPeople);

            string jsonVotes = JsonSerializer.Serialize(Votes);
            File.WriteAllText(votesPath, jsonVotes);

            string jsonTokens = JsonSerializer.Serialize(Tokens);
            File.WriteAllText(tokensPath, jsonTokens);

        }

        public byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public bool SendBulletin(byte[] eGEncryptedBulletin, byte[] eGEncryptedSeed, byte[] eGEncryptedId)
        {
            byte[] eGDecryptedBulletin = Decrypt(keys.Item1, eGEncryptedBulletin);
            byte[] eGDecryptedSeed = Decrypt(keys.Item1, eGEncryptedSeed);
            byte[] eGDecryptedId = Decrypt(keys.Item1, eGEncryptedId);

            int seed = BitConverter.ToInt32(eGDecryptedSeed);
            int voterId = BitConverter.ToInt32(eGDecryptedId);

            if (Votes.FirstOrDefault(x=>x.Who == voterId.ToString()) != null)
            {
                Console.WriteLine("You already voted!");
                return false;
            }

            byte[] bulletinData = XORWithBBSBySeed(eGDecryptedBulletin, seed);
            string bulletinString = Encoding.UTF8.GetString(bulletinData);
            int candidateId = Convert.ToInt32(bulletinString);

            Vote vote = new Vote(voterId.ToString(), candidateId.ToString());
            
            Votes.Add(vote);
            Save();
            return true;
        }

        public string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }


        private static readonly BigInteger p = 3263849;
        private static readonly BigInteger q = 1302498943;
        private static readonly BigInteger m = p * q;
        private static int RandomExponentMax = 100;

        public static BigInteger next(BigInteger prev)
        {
            return (prev * prev) % m;
        }

        public string GetElgamalPublicKey()
        {
            return keys.Item2;
        }

        public Tuple<string, string> GenerateElgamalKeys()
        {
            var lines = new string[2]{ "1665997633093155705263923663680487185948531888850484859473375695734301776192932338784530163","170057347237941209366519667629336535698946063913573988287540019819022183488419112350737049" };

            var prime = BigInteger.Parse(lines[0]);
            var generator = BigInteger.Parse(lines[1]);

            var random = new Random();
            var aliceK = random.Next(1, RandomExponentMax);
            var alicePublicKey = BigInteger.ModPow(generator, aliceK, prime);

            var output = prime + " " + generator + " ";
            var privateKeyText = output + aliceK + " ";
            var publicKeyText = output + alicePublicKey + " ";

            return Tuple.Create(privateKeyText, publicKeyText);
        }

        private byte[] XOR(byte[] word, byte[] key)
        {
            if (word.Length == key.Length)
            {
                byte[] result = new byte[word.Length];
                for (int i = 0; i < word.Length; i++)
                {
                    result[i] = (byte)(word[i] ^ key[i]);
                }
                return result;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public byte[] XORWithBBSBySeed(byte[] dataToProcess, BigInteger seed)
        {
            byte[] key = new byte[dataToProcess.Length];

            BigInteger xprev = seed;

            for (int i = 0; i < key.Length; i++)
            {
                BigInteger xnext = next(xprev);
                key[i] = (byte)(xnext % 256);
                xprev = xnext;
            }

            byte[] result = XOR(dataToProcess, key);

            return result;
        }

        public byte[] Encrypt(string publicKey, byte[] message)
        {
            var publicKeyLines = publicKey.Split(' ');
            var messageLines = message;

            var messege = BigInteger.Parse(publicKeyLines[0]);
            var prime = BigInteger.Parse(publicKeyLines[1]);

            var generator = BigInteger.Parse(publicKeyLines[1]);
            var alicePublicKey = BigInteger.Parse(publicKeyLines[2]);

            var random = new Random();
            var bobK = random.Next(1, RandomExponentMax);

            var bobPublicKey = BigInteger.ModPow(generator, bobK, prime);

            var encryptionKey = BigInteger.ModPow(alicePublicKey, bobK, prime);

            var encryptedMessage = (messege * encryptionKey) % prime;
            var output = bobPublicKey + " " + encryptedMessage + " ";
            return message;
        }

        public byte[] Decrypt(string privateKey, byte[] encryptedMessage)
        {
            var privateKeyLines = privateKey.Split(' ');
            var encryptedMessageLines = privateKeyLines;


            var prime = BigInteger.Parse(privateKeyLines[0]);
            var generator = BigInteger.Parse(privateKeyLines[1]);

            var bobPublicKey = BigInteger.Parse(encryptedMessageLines[0]);
            var bobK = 1;
            while (true)
            {
                if (BigInteger.ModPow(generator, bobK, prime) == bobPublicKey)
                    break;

                bobK++;
                break;
            }

            var aliceK = BigInteger.Parse(privateKeyLines[2]);
            var alicePublicKey = BigInteger.ModPow(generator, aliceK, prime);

            var encryptionKey = BigInteger.ModPow(alicePublicKey, bobK, prime);

            var encryptedMesege = BigInteger.Parse(encryptedMessageLines[1]);
            var decryptedMessege = encryptedMessage;
            var encryptionKeyInverse = BigInteger.ModPow(encryptionKey, prime - 2, prime);
            var decryptedMessage = (encryptedMesege * encryptionKeyInverse) % prime;

            var output = decryptedMessege;
            return output;
        }

    }
}
