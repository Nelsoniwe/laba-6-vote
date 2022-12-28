using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using laba3_vote.Models;
using System.Runtime.ConstrainedExecution;

namespace laba3_vote
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CVK cvk = new CVK();
            cvk.Load();
            

            Person currentUser = null;
            bool exit = false;
            string currentToken = null;

            while (!exit)
            {
                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("1. Choose Person\n2. Create Person\n3. Delete Person\n4. Watch Results\n5. exit");
                    string action = Console.ReadLine();

                    if (action == "1")
                    {
                        Console.Clear();

                        if (cvk.People.FindAll(x => x.Role == Role.voter).Count > 0)
                        {

                            var people = cvk.People.FindAll(x => x.Role == Role.voter);
                            foreach (var item in people)
                            {
                                Console.WriteLine($"{item.Id} Role: {item.Role} Name: {item.Name} Surname: {item.Surname}");
                            }

                            Console.WriteLine("Choose person");
                            var id = Console.ReadLine();
                            var result = cvk.People.FirstOrDefault(x => x.Id == id);
                            if (result != null && result.Role == Role.voter)
                            {
                                currentUser = result;

                                Console.WriteLine("Write token:");
                                currentToken = Console.ReadLine();

                                break;
                            }
                            else
                            {
                                Console.WriteLine("Person don't exist");
                                Console.ReadLine();
                            }

                        }
                        else
                        {
                            Console.WriteLine("People don't exist");
                            Console.ReadLine();
                        }
                    }
                    if (action == "2")
                    {
                        Console.Clear();
                        Console.WriteLine("Write a name:");
                        var name = Console.ReadLine();
                        Console.WriteLine("Write a surname:");
                        var surname = Console.ReadLine();

                        Console.WriteLine($"Write a role: ({Role.applicant}, {Role.voter})");
                        var role = Console.ReadLine();

                        if (name != "" && surname != "" && Enum.IsDefined(typeof(Role), role))
                        {
                            var random = new Random();
                            string id = random.Next(1000000).ToString();

                            while (cvk.People.FirstOrDefault(x => x.Id == id) != null)
                            {
                                id = random.Next(1000000).ToString();
                            }

                            cvk.People.Add(new Person(id, name, surname, (Role)Enum.Parse(typeof(Role), role)));
                            var token = random.Next(1000000).ToString();
                            cvk.Tokens.Add(new Token(){Id = id ,Key = token });

                            cvk.Save();

                            Console.WriteLine($"Your token: {token}");
                            Console.ReadLine();
                            continue;
                        }
                    }
                    if (action == "3")
                    {
                        Console.Clear();
                        var people = cvk.People;

                        if (people.Count == 0)
                        {
                            Console.WriteLine("People don't exist");
                            Console.ReadLine();
                            continue;
                        }

                        foreach (var item in people)
                        {
                            Console.WriteLine($"{item.Id} Role: {item.Role} Name: {item.Name} Surname: {item.Surname}");
                        }

                        Console.WriteLine("Choose person");

                        var id = Console.ReadLine();
                        var result = cvk.People.FirstOrDefault(x => x.Id == id);
                        if (result != null)
                        {
                            cvk.People.Remove(result);
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Person doesn't exist");
                            Console.ReadLine();
                        }
                    }
                    if (action == "4")
                    {
                        foreach (var person in cvk.People.Where(x=>x.Role==Role.applicant))
                        {
                            Console.WriteLine($"id: {person.Id} name: {person.Name} {person.Surname} votes: {cvk.Votes.Count(x => x.ForWho == person.Id)}");
                        }

                        Console.ReadLine();
                        continue;
                    }
                    if (action == "5")
                    {
                        exit = true;
                        break;
                    }
                }

                while (true)
                {
                    Console.Clear();
                    if (currentUser == null)
                    {
                        Console.WriteLine("Current user doesn't exist");
                        break;
                    }

                    if (currentUser.Voted == true && currentUser.Permission != true)
                    {
                        Console.WriteLine("Current user can't vote");
                        break;
                    }
                     
                    if (cvk.Tokens.FirstOrDefault(x => x.Key == currentToken) == null)
                    {
                        Console.WriteLine("Token doesn't exist!");
                        Console.ReadLine();
                        break;
                    }

                    Console.WriteLine("Write id of the applicant you want to vote for");

                    var people = cvk.People.FindAll(x => x.Role == Role.applicant);

                    foreach (var item in people)
                    {
                        Console.WriteLine($"Id: {item.Id} Name: {item.Name} Surname: {item.Surname}");
                    }

                    var id = Console.ReadLine();
                    var choosenApplicant = cvk.People.FirstOrDefault(x => x.Id == id);

                    if (choosenApplicant == null)
                    {
                        Console.WriteLine("Applicant doesn't exist");
                        Console.ReadLine();
                        break;
                    }

                    byte[] bulletin = Encoding.UTF8.GetBytes(id);

                    byte[] xORedBulletin = cvk.XORWithBBSBySeed(bulletin, Convert.ToInt32(currentToken));

                    string publicEGkey = cvk.GetElgamalPublicKey();

                    byte[] eGEncryptedBulletin = cvk.Encrypt(publicEGkey, xORedBulletin);
                    byte[] eGEncryptedSeed = cvk.Encrypt(publicEGkey, BitConverter.GetBytes(Convert.ToInt32(currentToken)));
                    byte[] eGEncryptedId = cvk.Encrypt(publicEGkey, BitConverter.GetBytes(Convert.ToInt32(currentUser.Id)));

                    var result = cvk.SendBulletin(eGEncryptedBulletin, eGEncryptedSeed, eGEncryptedId);

                    if (result)
                        Console.WriteLine("Success");
                    else
                        Console.WriteLine("Something went wrong");

                    Console.ReadLine();
                    break;
                }

                cvk.Save();

            }
        }
    }
}
