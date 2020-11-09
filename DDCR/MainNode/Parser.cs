using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using DDCR.Interfaces;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;

namespace DDCR
{
    /// <summary>
    /// Responsible for parsing the received messages from other Main nodes and nodes.
    /// </summary>
    public class Parser
    {
        private readonly IGraph graph;
        private readonly IConfig config;
        private char Terminator;
        private string UNAVAILABLE = "UNAVAILABLE";
        private string SUCCESS = "SUCCESS";
        public Parser(IGraph graph, IConfig config)
        {
            this.graph = graph;
            this.config = config;
            this.Terminator = config.Terminator;

            //UNAVAILABLE should always close the connection
            this.UNAVAILABLE += Terminator;
        }

        /// <summary>
        /// Handles messages sent from a Main Node and creating a reply for the Main Node.
        /// </summary>
        /// <param name="message">Incoming message from a Main Node</param>
        /// <returns>Returns a reply to a Main Node</returns>
        public string ParseMainNode(string message)
        {
            string[] newLineMessage = message.Split("\n");
            string[] splitMessage = newLineMessage[0].Split(' ');

            switch (splitMessage[0])
            {
                case "BLOCK":
                    {
                        Guid execId = Guid.Parse(splitMessage[1]);
                        string execTime =
                            splitMessage[2] + " " + splitMessage[3];

                        string[] events = new string[newLineMessage.Length - 1];
                        for (int i = 1; i < newLineMessage.Length; i++)
                        {
                            string[] line = newLineMessage[i].Split(' ');
                            if (graph.Events.ContainsKey(line[0]))
                                events[i - 1] = line[0];
                            else
                                return UNAVAILABLE;
                        }

                        List<(EventInternal, Relation, bool)> actionsTemp =
                            new List<(EventInternal, Relation, bool)>();

                        EventForeign e = new EventForeign();

                        for (int i = 1; i < newLineMessage.Length; i++)
                        {
                            string[] line = newLineMessage[i].Split(' ');
                            EventInternal ev = graph.Events[line[0]];
                            for (int j = 1; j < line.Length; j++)
                            {
                                switch (line[j])
                                {
                                    case "exclude":
                                        e.Excludes.Add(ev);
                                        break;

                                    case "include":
                                        e.Includes.Add(ev);
                                        break;

                                    case "response":
                                        e.Responses.Add(ev);
                                        break;

                                    case "milestone+":
                                        e.Pending = true;
                                        actionsTemp.Add((ev, Relation.Milestone, true));
                                        break;

                                    case "milestone-":
                                        actionsTemp.Add((ev, Relation.Milestone, false));
                                        break;

                                    case "condition+":
                                        actionsTemp.Add((ev, Relation.Condition, true));
                                        break;

                                    case "condition-":
                                        actionsTemp.Add((ev, Relation.Condition, false));
                                        break;
                                }
                            }
                        }

                        graph.EventsForeign.Add(execId, e);

                        if (!graph.Blocked.ContainsKey(execId))
                            graph.Blocked.Add(execId, new List<EventInternal>());

                        // Check if affected events are blocked, if they are not - try to block them. If blocking them fails, Unblock blocked events
                        if (graph.CheckBlocked(events))
                        {
                            graph.UnblockEvents(execId);
                            break;
                        }

                        else if (!graph.TryBlockInternal(e, execId))
                        {
                            graph.UnblockEvents(execId);
                            break;
                        }

                        // Blocking external
                        Dictionary<string, string> peerMessages = graph.GetPeerMessages(e);
                        if (peerMessages.Count != 0 && !graph.TryBlockExternal(peerMessages, execId, execTime))
                        {
                            graph.UnblockEvents(execId);
                            break;
                        }

                        (EventInternal, Relation, bool)[] actions1 = actionsTemp.ToArray();
                        (EventInternal, Relation, bool)[] actions2 =
                            graph.GetNewMarkingsReversible(e).ToArray();

                        //save actions for later execution/reverse
                        e.actions = actions1.Concat(actions2);

                        foreach (string peerName in peerMessages.Keys)
                            e.peersPropagate.Add(peerName, Task.FromResult(string.Empty));

                        return SUCCESS;
                    }

                case "EXECUTE":
                    {
                        Guid execId = Guid.Parse(splitMessage[1]);
                        if (graph.EventsForeign.ContainsKey(execId))
                        {
                            EventForeign foreignEvent = graph.EventsForeign[execId];

                            //Propagate markings, and ensure reversibility by reverting if peers aren't available
                            bool propagateResult =
                                graph.TryExecuteExternal(foreignEvent.peersPropagate, execId);

                            if (!propagateResult)
                            {
                                graph.UnblockEvents(execId);
                                graph.EventsForeign.Remove(execId);
                            }
                            else
                            {
                                graph.ChangeMarkings(foreignEvent, false);
                                return SUCCESS;
                            }
                        }
                        break;
                    }

                case "REVERT": 
                    {
                        Guid execId = Guid.Parse(splitMessage[1]);
                        if (graph.EventsForeign.ContainsKey(execId))
                        {
                            EventForeign foreignEvent = graph.EventsForeign[execId];

                            //propagates a revert message
                            Dictionary<string, Task<string>> tasksRevert =
                                graph.MessagePeers("REVERT", execId, foreignEvent.peersPropagate, true);
                            
                            // Reverts marking changes
                            graph.ChangeMarkings(foreignEvent, true);

                            graph.UnblockEvents(execId);
                            graph.EventsForeign.Remove(execId);

                            return SUCCESS + Terminator;
                        }
                        break;
                    }

                case "UNBLOCK":
                    {
                        Guid execId = Guid.Parse(splitMessage[1]);
                        if (graph.EventsForeign.ContainsKey(execId) && graph.Blocked.ContainsKey(execId))
                        {
                            EventForeign foreignEvent = graph.EventsForeign[execId];
                            Dictionary<string, Task<string>> tasksUnblock =
                                graph.MessagePeers("UNBLOCK", execId, foreignEvent.peersPropagate, true);

                            IEnumerable<string> deadPeers =
                                from x in foreignEvent.peersPropagate.Keys.Except(tasksUnblock.Keys)
                                select x;

                            foreach (string deadPeer in deadPeers)
                                graph.TryUnblockPeerAsync(deadPeer, execId.ToString());

                            graph.UnblockEvents(execId);
                            graph.EventsForeign.Remove(execId);

                            return SUCCESS + Terminator;
                        }
                        break;
                    }

                case "ACCEPTING":
                    {
                        if (graph.GetAcceptingInternal())
                        {
                            Guid id = Guid.Parse(splitMessage[1]);
                            if (graph.GetAcceptingIds.Contains(id))
                                return "ACCEPTING IGNORE" + Terminator;

                            graph.GetAcceptingIds.Add(id);

                            string[] receivedPeers = newLineMessage[1].Split(' ');
                            Dictionary<string, IClient> uniquePeers = new Dictionary<string, IClient>();

                            foreach (string peerName in config.MainNodes.Keys)
                            {
                                if (!receivedPeers.Contains(peerName))
                                    uniquePeers[peerName] = config.MainNodes[peerName];
                            }

                            string prevPeers = newLineMessage[1];
                            bool reply = graph.GetAcceptingExternal(uniquePeers, prevPeers, id);
                            if (reply)
                                return "TRUE" + Terminator;
                        }
                        return "FALSE" + Terminator;
                    }

                case "LOG":
                    {
                        Guid id = Guid.Parse(splitMessage[1]);
                        if (graph.GetLogIds.Contains(id))
                            return "LOG IGNORE" + Terminator;
                        graph.GetLogIds.Add(id);
                        string[] receivedPeers = newLineMessage[1].Split(' ');
                        Dictionary<string, IClient> uniquePeers = new Dictionary<string, IClient>();

                        foreach (string peerName in config.MainNodes.Keys)
                        {
                            if (!receivedPeers.Contains(peerName))
                                uniquePeers[peerName] = config.MainNodes[peerName];
                        }

                        string prevPeers = newLineMessage[1];
                        
                        ILog reply = graph.Log.GetGlobal(uniquePeers, prevPeers, id);

                        return reply.ToString() + Terminator;
                    }
            }
            return UNAVAILABLE;
        }
        /// <summary>
        ///  Handles messages sent from a Node and creating a reply for the Node.
        /// </summary>
        /// <param name="message"> The incoming message from a node</param>
        /// <param name="ip">The IP address of the node, used to identify the node</param>
        /// <returns>Returns a reply to the node as a string</returns>
        public string ParseNode(string message, IPAddress ip)
        {
            string[] splitMessage = message.Split(' ');
            switch (splitMessage[0])
            {
                case "PERMISSIONS":
                    return graph.GetNodeEvents(ip);

                case "EXECUTE":
                    {
                        EventInternal ev;
                        try
                        {
                            ev = graph.Events[splitMessage[1]];
                        }
                        catch (Exception)
                        {
                            return "SPECIFIED EVENT INVALID";
                        }

                        if (graph.NodeEvents[ip].Contains(ev))
                            return graph.Execute(splitMessage[1]).ToString();
                        else
                            return "INVALID PERMISSION";
                    }
                case "ACCEPTING":
                    {
                        try
                        {
                            return (graph.GetAcceptingInternal() && graph.GetAcceptingExternal(config.MainNodes, config.Name, Guid.NewGuid())
                            ).ToString();
                        }
                        catch (Exception)
                        {
                            return UNAVAILABLE;
                        }
                        
                    }

                case "LOG":
                    {
                        ILog log = graph.Log.GetGlobal(config.MainNodes, config.Name, Guid.NewGuid());
                        if (log == null)
                            return UNAVAILABLE;
                        log.SortLog();
                        return log.ToString();
                    }

                case "MARKING":
                    {
                        EventInternal ev;
                        try
                        {
                            ev = graph.Events[splitMessage[1]];
                        }
                        catch (Exception)
                        {
                            return "SPECIFIED EVENT INVALID";
                        }

                        return graph.GetMarking(ev);
                    }
                case "ALLMARKINGS":
                    {
                        return graph.GetAllMarkings();
                    }

                default:
                    return "UNKNOWN COMMAND";
            }
        }

        /// <summary>
        /// Converts a Byte array to a string and replaces the tailing whitespace with empty strings.
        /// </summary>
        /// <param name="msg">The incoming message</param>
        /// <returns>Returns the incoming message as a string.</returns>
        public static string DecodeMessage(byte[] msg)
        {
            string ret = Encoding.UTF8.GetString(msg);
            return ret.Replace("\0", string.Empty);
        }
    }
}