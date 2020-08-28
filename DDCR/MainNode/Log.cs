using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DDCR.Interfaces;

namespace DDCR
{
    
    public class Log : List<(DateTime, string)>, ILog
    {
        IConfig config;

        /// <summary>
        /// Creates an instance of Log identical to the first parameter which uses the formatting defined by the config parameter
        /// </summary>
        /// <param name="config">Defines formatting</param>
        public Log(IEnumerable<(DateTime, string)> collection, IConfig config) : base(collection)
        {
            this.config = config;
        }

        /// <summary>
        /// Creates an instance of Log using the formatting defined by the config parameter
        /// </summary>
        /// <param name="config">Defines formatting</param>
        public Log(IConfig config)
        {
            this.config = config;
        }
        
        public void Add(DateTime time, string eventName)
        {
            Add((time, eventName));
        }
        public void SortLog()
        {
            Sort((a, b) => a.Item1.CompareTo(b.Item1));
        }

        
        public ILog GetGlobal(Dictionary<string, IClient> peers, string prevPeers, Guid id)
        {
            List<Task<string>> tasks = new List<Task<string>>();
            string peerNames = prevPeers;
            foreach (string peer in peers.Keys)
                peerNames += " " + peer;

            foreach (IClient client in peers.Values)
                tasks.Add(client.SendAsync(string.Format("LOG {0}\n{1}", id.ToString(), peerNames), true));
                

            Task.WaitAll(tasks.ToArray());

            Log globalLog = new Log(this, config);
            foreach (Task<string> task in tasks)
            {
                string[] line = task.Result.Split('\n');

                if (line[0] == "LOG IGNORE" || line[0] == "LOG EMPTY")
                    continue;

                if (line[0] == "UNAVAILABLE")
                    return null;

                foreach (string entry in line)
                {
                    string[] curLine = entry.Split(' ');
                    /*The method DateTime.ToString() converts to a string with a space, 
                      which are separated with String.Split. We recombine them and parse
                      them back to the DateTime class below. */

                    DateTime time = DateTime.Parse(string.Format("{0} {1}", curLine[0], curLine[1]),
                        config.Culture);
                    //curLine[2] should be event name of the executed event.
                    globalLog.Add((time, curLine[2]));
                }
            }
            return globalLog;
        }
        
        public override string ToString()
        {
            if (this != null && this.Count == 0)
                return "LOG EMPTY";

            string msg = string.Empty;
            foreach ((DateTime, string) entry in this)
            {
                msg += string.Format("{0} {1}\n", entry.Item1.ToString(config.Culture),
                    entry.Item2);
            }

            return msg.Substring(0, msg.Count() - 1);
        }
    }
}