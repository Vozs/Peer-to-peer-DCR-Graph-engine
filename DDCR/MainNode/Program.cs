using System;
using System.Threading;

namespace DDCR
{
    internal class Program
    {
        static string configFile = "MainNode.ini";
        static string graphFile = "..\\graph.xml";

        private static void Main()
        {
            Config config = new Config(configFile);
            Log log = new Log(config);
            Graph graph = new Graph(log, config);
            graph.Load(graphFile);
            Parser parser = new Parser(graph, config);
            Listener listener = new Listener(parser, config);
            Thread listen = new Thread(listener.Listen);

            //Below is debugging to make it easier for manual testing.
            Console.WriteLine("Known Main Nodes:");
            foreach (var item in config.MainNodes)
            {
                Console.WriteLine(item.Key);
            }

            foreach (var item in config.Nodes)
            {
                Console.Write($"Node {item.Key} has following events: ");
                foreach (var item1 in graph.NodeEvents[item.Value])
                {
                    Console.Write("{0}, ", item1.Name);
                }
                Console.WriteLine();
            }

            //Internal relations
            foreach (var item in graph.Events)
            {
                Console.WriteLine("{0} has following relations: ", item.Key);
                if (item.Value.Conditions.Count > 0)
                {
                    Console.Write("Conditions: ");
                    foreach (var c in item.Value.Conditions)
                    {
                        Console.Write("{0}, ", c.Name);
                    }
                    Console.WriteLine();
                }
                
                if (item.Value.Responses.Count > 0)
                {
                    Console.Write("Responses: ");
                    foreach (var r in item.Value.Responses)
                    {
                        Console.Write("{0}, ", r.Name);
                    }
                    Console.WriteLine();
                }
                
                if (item.Value.Milestones.Count > 0)
                {
                    Console.Write("Milestones: ");
                    foreach (var m in item.Value.Milestones)
                    {
                        Console.Write("{0}, ", m.Name);
                    }
                    Console.WriteLine();
                }   
                
                if (item.Value.Includes.Count > 0)
                {
                    Console.Write("Includes: ");
                    foreach (var i in item.Value.Includes)
                    {
                        Console.Write("{0}, ", i.Name);
                    }
                    Console.WriteLine();
                }
                    
                if (item.Value.Excludes.Count > 0)
                {
                    Console.Write("Excludes: ");
                    foreach (var x in item.Value.Excludes)
                    {
                        Console.Write("{0}, ", x.Name);
                    }
                    Console.WriteLine();
                }

                //External relations
                Console.Write("{0} has following external relations: ", item.Key);
                if (item.Value.ConditionsExternal.Count > 0)
                {
                    Console.Write("Conditions: ");
                    foreach (var c in item.Value.ConditionsExternal)
                    {
                        Console.Write("{0}, ", c.Name);
                    }
                    Console.WriteLine();
                }

                Console.WriteLine();
                if (item.Value.ResponsesExternal.Count > 0)
                {
                    Console.Write("Responses: ");
                    foreach (var r in item.Value.ResponsesExternal)
                    {
                        Console.Write("{0}, ", r.Name);
                    }
                    Console.WriteLine();
                }

                if (item.Value.MilestonesExternal.Count > 0)
                {
                    Console.Write("Milestones: ");
                    foreach (var m in item.Value.MilestonesExternal)
                    {
                        Console.Write("{0}, ", m.Name);
                    }
                    Console.WriteLine();
                }

                if (item.Value.IncludesExternal.Count > 0)
                {
                    Console.Write("Includes: ");
                    foreach (var i in item.Value.IncludesExternal)
                    {
                        Console.Write("{0}, ", i.Name);
                    }
                    Console.WriteLine();
                }

                if (item.Value.ExcludesExternal.Count > 0)
                {
                    Console.Write("Excludes: ");
                    foreach (var x in item.Value.ExcludesExternal)
                    {
                        Console.Write("{0}, ", x.Name);
                    }
                    Console.WriteLine();
                }

            }
            Console.WriteLine("Current markings of local events:");
            Console.WriteLine(graph.GetAllMarkings());
            Console.WriteLine();
            //Debugging writelines end.
            listen.Start();
            
            
        }
    }
}