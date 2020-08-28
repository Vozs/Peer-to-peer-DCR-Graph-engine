using Node.Interfaces;
using System;

namespace Node
{
    class Program
    {
        static string configFile = "Node.ini";
        static void Main(string[] args)
        {
            IConfig config = new Config(configFile);
            Node node = new Node(config);

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                string write = node.HandleInput(input);
                Console.WriteLine(write);
            }
        }

    }
}
