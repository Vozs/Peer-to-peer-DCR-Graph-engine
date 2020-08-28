using DDCR.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace DDCR
{
    /// <summary>
    /// Configuration specifying the name of this Main Node, port it should listen to, as well as other settings such as which Nodes are known, which events each of these Nodes can execute, and more...
    /// Reads from the DDCR.ini file, located in the same folder as the executable.
    /// </summary>
    public class Config : IConfig
    {
        
        public int ClientTimeoutMs { get => clientTimeoutMs; set => clientTimeoutMs = value; }
        
        public CultureInfo Culture { get => culture; set => culture = value; }
        
        public int ListenPort { get => listenPort; set => listenPort = value; }
        
        public int MaxConnectionAttempts { get => maxConnectionAttempts; set => maxConnectionAttempts = value; }
        
        public string Name { get => name; set => name = value; }
        
        public Dictionary<string, IPAddress> Nodes => nodes;
        
        public Dictionary<string, IClient> MainNodes => mainNodes;
        
        
        public Dictionary<IPAddress, HashSet<string>> NodeEventsMapping => nodeEventsMapping;

        public readonly char Terminator = '\u0017';

        private string name;
        private int listenPort;
        private int clientTimeoutMs = 10000; //ms
        private int maxConnectionAttempts = 5;
        private CultureInfo culture = CultureInfo.CreateSpecificCulture("en-GB");
        
        private Dictionary<string, IClient> mainNodes = new Dictionary<string, IClient>();
        private Dictionary<string, IPAddress> nodes = new Dictionary<string, IPAddress>();
        private Dictionary<IPAddress, HashSet<string>> nodeEventsMapping = new Dictionary<IPAddress, HashSet<string>>();

        /// <summary>
        /// Constructs an instance of Config by reading from the file designated by the "path" parameter.
        /// </summary>
        /// <param name="path">The path of the config file</param>
        public Config(string path)
        {
            StreamReader reader = new StreamReader(path);
            string file = reader.ReadToEnd();
            reader.Close();

            string[] lines = file.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i != lines.Length)
            {
                switch (lines[i])
                {
                    case "[Properties]":
                        i = SetProperties(lines, i + 1);
                        break;
                    case "[MainNodes]":
                        i = SetMainNodes(lines, i + 1);
                        break;
                    case "[Nodes]":
                        i = SetNodes(lines, i + 1);
                        break;
                    case "[NodeEventsMapping]":
                        i = SetNodeEventsMapping(lines, i + 1);
                        break;
                    default:
                        i++;
                        break;
                }
            }  
        }
        /// <summary>
        /// Sets the properties matching the ones in the .ini file by converting the input to the type of the property.
        /// </summary>
        /// <param name="lines">All the lines of the .ini document</param>
        /// <param name="i">The line to start at, and iterate with</param>
        /// <returns>The line where the properties end</returns>
        private int SetProperties(string[] lines, int i)
        {
            Type type = this.GetType();
            while (i != lines.Length && lines[i][0] != '[')
            {
                string pName = lines[i].Substring(0, lines[i].IndexOf('='));
                string pValue = lines[i].Substring(lines[i].IndexOf('=') + 1);
                PropertyInfo pInfo = type.GetProperty(pName);
                i++;
                switch (pInfo.PropertyType.Name)
                {
                    case "String":
                        pInfo.SetValue(this, pValue);
                        break;
                    case "Int32":
                        pInfo.SetValue(this, Convert.ToInt32(pValue));
                        break;

                    case "CultureInfo":
                        pInfo.SetValue(this, CultureInfo.CreateSpecificCulture((string)pValue));
                        break;
                }
            }
            return i;
        }

        /// <summary>
        /// Adds each Main Node in the .ini config file to the mainNodes dictionary.
        /// </summary>
        /// <param name="lines">All the lines of the .ini document</param>
        /// <param name="i">The line to start at, and iterate with</param>
        /// <returns>The line where the properties end</returns>
        private int SetMainNodes(string[] lines, int i)
        {
            while (i != lines.Length && lines[i][0] != '[')
            {
                int ipStart = lines[i].Substring(0, lines[i].IndexOf('=')).Length;
                int portStart = lines[i].Substring(0, lines[i].IndexOf(':')).Length;

                string mName = lines[i].Substring(0, ipStart);
                string mIP = lines[i].Substring(ipStart + 1, lines[i].Length - (lines[i].Length - portStart) - ipStart - 1);
                string mPort = lines[i].Substring(portStart + 1);

                Client client = new Client(mIP, Convert.ToInt32(mPort), this);
                mainNodes.Add(mName, client);
                i++;
            }
            return i;
        }

        /// <summary>
        /// Adds each Node in the .ini config file to the nodes dictionary.
        /// </summary>
        /// <param name="lines">All the lines of the .ini document</param>
        /// <param name="i">The line to start at, and iterate with</param>
        /// <returns>The line where the properties end</returns>
        private int SetNodes(string[] lines, int i)
        {
            while (i != lines.Length && lines[i][0] != '[')
            {
                string mName = lines[i].Substring(0, lines[i].IndexOf('='));
                string mIP = lines[i].Substring(lines[i].IndexOf('=')+1);
                nodes.Add(mName, IPAddress.Parse(mIP));
                nodeEventsMapping.Add(IPAddress.Parse(mIP), new HashSet<string>());
                i++;
            }
            return i;
        }
        /// <summary>
        /// Adds an entry in the nodeEventsMapping dictionary with the Node as the key, and the value as a Hashset containing all the string names of the events that the given node has permission to execute.
        /// </summary>
        /// <param name="lines">All the lines of the .ini document</param>
        /// <param name="i">The line to start at, and iterate with</param>
        /// <returns>The line where the properties end</returns>
        private int SetNodeEventsMapping(string[] lines, int i)
        {
            while (i != lines.Length && lines[i][0] != '[')
            {
                string NodeName = lines[i].Substring(0, lines[i].IndexOf('='));
                string eventNames = lines[i].Substring(lines[i].IndexOf('=') + 1);
                string[] events = eventNames.Split(' ');
                foreach (string ev in events)
                {
                    nodeEventsMapping[nodes[NodeName]].Add(ev);
                }
                i++;
            }
            return i;
        }
    }


}