using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using DDCR.Interfaces;

namespace DDCR
{
    public class Graph : IGraph
    {
        private Dictionary<IPAddress, HashSet<EventInternal>> nodeEvents =
            new Dictionary<IPAddress, HashSet<EventInternal>>();

        private Dictionary<string, EventInternal> events = new Dictionary<string, EventInternal>();

        /// <summary>
        /// Mapping of string EventExternal names to references of their instantiations, for fast access.
        /// </summary>
        private Dictionary<string, EventExternal> eventsExternal =
            new Dictionary<string, EventExternal>();

        private Dictionary<Guid, List<EventInternal>> blocked =
            new Dictionary<Guid, List<EventInternal>>();

        private Dictionary<Guid, EventForeign> eventsForeign = new Dictionary<Guid, EventForeign>();


        private List<Guid> getLogIds = new List<Guid>();

        private List<Guid> getAcceptingIds = new List<Guid>();

        private ILog log;
        private IConfig config;
        public Dictionary<IPAddress, HashSet<EventInternal>> NodeEvents  => nodeEvents;
        public Dictionary<Guid, EventForeign> EventsForeign  => eventsForeign; 
        public List<Guid> GetLogIds => getLogIds;
        public List<Guid> GetAcceptingIds => getAcceptingIds;
        public Dictionary<Guid, List<EventInternal>> Blocked => blocked;
        public Dictionary<string, EventInternal> Events => events; 
        public ILog Log => log;  
        public Dictionary<string, EventExternal> EventsExternal => eventsExternal;

        public Graph(ILog log, IConfig config)
        {
            this.log = log;
            this.config = config;
            foreach (var node in config.Nodes)
            {
                nodeEvents.Add(node.Value, new HashSet<EventInternal>());
            }
        }

        public void Load(string path)
        {
            XElement graphXML = XElement.Load(path);
            var events = graphXML.Elements("specification").Elements("resources").Elements("labelMappings");
            var markings = graphXML.Elements("runtime").Elements("marking");
            var relations = graphXML.Elements("specification").Elements("constraints");
            var eventNameMappingExternal = new Dictionary<string, EventExternal>(); //Kan der ikke ske duplicate keys?
            var eventMarkingMappingExternal = new Dictionary<string, bool[]>();
            
            foreach (var ev in events.Elements("labelMapping"))
            {
                var value = ev.Attribute("eventId").Value;
                var mainNode = value.Substring(0, value.IndexOf('_'));
                var label = ev.Attribute("labelId").Value;
                var name = value.Substring(mainNode.Length + 1);
                bool executed = (bool)markings.Elements("executed").Descendants().Attributes("id").Any(x => x.Value == value);
                bool included = (bool)markings.Elements("included").Descendants().Attributes("id").Any(x => x.Value == value);
                bool pending = (bool)markings.Elements("pendingResponses").Descendants().Attributes("id").Any(x => x.Value == value);
                //Create Internal Events
                if (mainNode == config.Name)
                {
                    foreach (KeyValuePair<string, IPAddress> node in config.Nodes)
                    {
                        foreach (string internalEvent in config.NodeEventsMapping[node.Value])
                        {
                            if (internalEvent == name)
                                CreateEvent(name, label, node.Key.Split(), included, pending, executed);
                        }
                    }
                }
                //Create temporary External Events, which may become permanent.
                else
                {
                    var tempEventExternal = new EventExternal(name, label, mainNode);
                    eventNameMappingExternal.Add(name, tempEventExternal);
                    eventMarkingMappingExternal.Add(name, new bool[3] { executed, included, pending });
                }
            }
            LoadRelations(relations.Elements("conditions").Elements(), Relation.Condition, eventNameMappingExternal, eventMarkingMappingExternal);
            LoadRelations(relations.Elements("responses").Elements(), Relation.Response, eventNameMappingExternal, eventMarkingMappingExternal);
            LoadRelations(relations.Elements("excludes").Elements(), Relation.Exclude, eventNameMappingExternal, eventMarkingMappingExternal);
            LoadRelations(relations.Elements("includes").Elements(), Relation.Include, eventNameMappingExternal, eventMarkingMappingExternal);
            LoadRelations(relations.Elements("milestones").Elements(), Relation.Milestone, eventNameMappingExternal, eventMarkingMappingExternal);
        }

        /// <summary>
        /// Load the relations between events by looking at the input dictionaries and sets the markings of the events accordinly.
        /// </summary>
        /// <param name="relations">An IEnumerable of XML elements, representing of relations</param>
        /// <param name="relationType">The enum type of the relation.</param>
        /// <param name="eventNameMapping">A dictionary with the EventExternal name as key and the EventExternal as value.</param>
        /// <param name="eventMarkingMapping">A dictionary with the EventExternal name as key and an array of bools as value - representing the markings.</param>
        private void LoadRelations(IEnumerable<XElement> relations, Relation relationType,
            Dictionary<string, EventExternal> eventNameMapping, Dictionary<string, bool[]> eventMarkingMapping)
        {
            foreach (var relation in relations)
            {
                var sValue = relation.Attribute("sourceId").Value;
                var sName = sValue.Substring(sValue.IndexOf('_') + 1);
                var tValue = relation.Attribute("targetId").Value;
                var tName = tValue.Substring(tValue.IndexOf('_') + 1);

                //Check if Local -> Local
                if (Events.ContainsKey(sName) && Events.ContainsKey(tName))
                {
                    AddRelationInternal(sName, tName, relationType);
                }

                //Check if Local -> External
                else if (Events.ContainsKey(sName))
                {
                    var tMainNode = eventNameMapping[tName].Peer;
                    var tLabel = eventNameMapping[tName].Label;

                    if (!EventsExternal.ContainsKey(tName))
                        EventsExternal.Add(tName, new EventExternal(tName, tLabel, tMainNode));

                    AddRelationExternal(sName, tName, relationType);
                }

                //Check if External -> Local
                else if (Events.ContainsKey(tName))
                {
                    bool[] sMarking = eventMarkingMapping[sName];
                    switch (relationType)
                    {
                        case Relation.Condition:
                            if (!sMarking[0] && sMarking[1])
                                Events[tName].Condition++;
                            break;
                        case Relation.Milestone:
                            if (!sMarking[0] && sMarking[2])
                                Events[tName].Milestone++;
                            break;
                    }
                }
            }
        }

        public string GetNodeEvents(IPAddress ip)
        {
            string msg = string.Empty;
            foreach (EventInternal ev in nodeEvents[ip])
                msg += string.Format("{0} ", ev.Name);
            if (msg.Length == 0)
                return "NONE";

            //remove last space
            msg = msg.Substring(0, msg.Length - 1);
            return msg;
        }

        public void CreateEvent(string name, string label, string[] owners, bool included = true,
            bool pending = false, bool executed = false)
        {
            //Default events are included and non-pending.
            if (!events.ContainsKey(name))
            {
                events.Add(name, new EventInternal(name, label, included, pending, executed));
            }

            foreach (string owner in owners)
            {
                IPAddress client = config.Nodes[owner];
                nodeEvents[client].Add(events[name]);
            }
        }

        public void CreateEventExternal(string name, string label, string peer)
        {
            EventExternal e = new EventExternal(name, label, peer);
            eventsExternal.Add(name, e);
        }

        public string GetMarking(EventInternal e)
        {
            return string.Format(
                "Included: {0}\nPending: {1}\nCondition: {2}\nMilestone: {3}\nExecuted: {4}",
                e.Included, e.Pending, e.Condition, e.Milestone, e.Executed);
        }

        public string GetAllMarkings()
        {
            
            int largestEvName = Events.Values.Max(x => x.Name.Length);
            if (largestEvName < "Event".Length)
                largestEvName = "Event".Length;

            string eventMarkings = string.Format("{0," + -largestEvName + "}\t{1,-5}\t{2,-5}\t{3,-5}\t{4,-5}\t{5,-5}",
                            "Event", "Incl", "Pend", "Cond", "Mile", "Exec");

            foreach (var ev in Events.Values)
            {
                eventMarkings += string.Format("\n{0,"+ -largestEvName+"}\t{1,-5}\t{2,-5}\t{3,-5}\t{4,-5}\t{5,-5}",
                ev.Name, ev.Included, ev.Pending, ev.Condition, ev.Milestone, ev.Executed);
            }

            return eventMarkings;
        }

        public void AddRelationExternal(string eNameFrom, string eNameTo, Relation relation)
        {
            EventInternal eFrom = events[eNameFrom];
            EventExternal eTo = eventsExternal[eNameTo];
            switch (relation)
            {
                case Relation.Condition:
                    eFrom.ConditionsExternal.Add(eTo);
                    break;

                case Relation.Response:
                    eFrom.ResponsesExternal.Add(eTo);
                    break;

                case Relation.Milestone:
                    eFrom.MilestonesExternal.Add(eTo);
                    break;

                case Relation.Include:
                    eFrom.IncludesExternal.Add(eTo);
                    break;

                case Relation.Exclude:
                    eFrom.ExcludesExternal.Add(eTo);
                    break;
            }
        }
        
        public void AddRelationInternal(string eNameFrom, string eNameTo, Relation relation)
        {
            EventInternal eFrom = events[eNameFrom];
            EventInternal eTo = events[eNameTo];
            switch (relation)
            {
                case Relation.Response:
                    eFrom.Responses.Add(eTo);
                    break;

                case Relation.Include:
                    eFrom.Includes.Add(eTo);
                    break;

                case Relation.Exclude:
                    eFrom.Excludes.Add(eTo);
                    break;

                case Relation.Condition:
                    eFrom.Conditions.Add(eTo);
                    if (eFrom.Included && !eFrom.Executed)
                        eTo.Condition++;
                    break;

                case Relation.Milestone:
                    eFrom.Milestones.Add(eTo);
                    if (eFrom.Included && eFrom.Pending)
                        eTo.Milestone++;
                    break;
            }
        }

        public bool GetAcceptingInternal()
        {
            //Local graph check
            foreach (EventInternal e in events.Values)
            {
                if (e.Pending && e.Included)
                    return false;
            }

            return true;
        }

        public bool GetAcceptingExternal(Dictionary<string, IClient> peers, string prevPeers,
            Guid id)
        {
            List<Task<string>> tasks = new List<Task<string>>();
            string peerNames = prevPeers;

            //Add all peers to message so it knows who the message has already been propagated to
            foreach (string peer in peers.Keys)
                peerNames += " " + peer;

            foreach (IClient client in peers.Values)
            {
                tasks.Add(client.SendAsync(string.Format("ACCEPTING {0}\n{1}", id.ToString(),
                    peerNames), true));
            }

            Task.WaitAll(tasks.ToArray());

            foreach (Task<string> task in tasks)
            {
                if (task.Result == "FALSE")
                    return false;
            }
            foreach (Task<string> task in tasks)
            {
                if (task.Result == "UNAVAILABLE")
                    throw new Exception("GetAcceptingExternal peer was unavailable");
            }

            return true;
        }

        public void UnblockEvents(Guid execId)
        {
            if (blocked.ContainsKey(execId))
            {
                foreach (EventInternal b in blocked[execId])
                    b.Block = new Tuple<bool, Guid>(false, Guid.Empty);

                blocked.Remove(execId);
            }
        }

        public Dictionary<string, string> GetPeerMessages(Event ev)
        {

            //For each IP we send to, we need a list of requests (ExternalEvent name + client)
            LinkedDictionary peerMessages = new LinkedDictionary();

            if (ev is EventInternal evI)
            {
                //Exclude
                foreach (EventExternal e in evI.ExcludesExternal)
                    peerMessages.Add(e.Peer, e.Name, "exclude");

                // Response
                foreach (EventExternal r in evI.ResponsesExternal)
                    peerMessages.Add(r.Peer, r.Name, "response");

                // Includes: Peers must check the included event for Milestone and Condition
                foreach (EventExternal i in evI.IncludesExternal)
                    peerMessages.Add(i.Peer, i.Name, "include");

                // Condition
                if (!evI.Executed) //If ev has already been executed, Conditions do not have any effect anymore.
                {
                    foreach (EventExternal c in evI.ConditionsExternal)
                        peerMessages.Add(c.Peer, c.Name, "condition-");
                }

                if (ev.Pending)
                //If ev is pending, then we send a milestone msg because if we send an Execute message, the peer will know to do Milestone-- since this event is no longer pending after being executed
                {
                    foreach (EventExternal m in evI.MilestonesExternal)
                        peerMessages.Add(m.Peer, m.Name, "milestone-");
                }
            }

            //Response e2 -> Milestone e3e
            foreach (EventInternal e2 in ev.Responses)
            {
                if (e2.Included && !e2.Pending)
                {
                    foreach (EventExternal e3e in e2.MilestonesExternal)
                        peerMessages.Add(e3e.Peer, e3e.Name, "milestone+");
                }
            }

            //Include e2 -> Condition e3e & Milestone e3e
            foreach (EventInternal e2 in ev.Includes)
            {
                if (!e2.Included)
                {
                    if (!e2.Executed)
                    {
                        foreach (EventExternal e3e in e2.ConditionsExternal)
                            peerMessages.Add(e3e.Peer, e3e.Name, "condition+");
                    }

                    if (e2.Pending)
                    {
                        foreach (EventExternal e3e in e2.MilestonesExternal)
                            peerMessages.Add(e3e.Peer, e3e.Name, "milestone+");
                    }
                }
            }

            //Exclude e2 -> Condition e3e & Milestone e3e
            foreach (EventInternal e2 in ev.Excludes)
            {
                if (e2.Included && ev != e2)
                {
                    if (!e2.Executed)
                    {
                        foreach (EventExternal e3e in e2.ConditionsExternal)
                            peerMessages.Add(e3e.Peer, e3e.Name, "condition-");
                    }

                    if (e2.Pending)
                    {
                        foreach (EventExternal e3e in e2.MilestonesExternal)
                            peerMessages.Add(e3e.Peer, e3e.Name, "milestone-");
                    }
                }
            }

            Dictionary<string, string> messages = new Dictionary<string, string>();
            foreach (KeyValuePair<string, HashSet<string>> peerName in peerMessages.Dictionary)
            {
                string msg = "";
                foreach (string eventName in peerMessages.Dictionary[peerName.Key])
                {
                    msg += eventName;
                    foreach (string relation in peerMessages.SubDictionary[eventName])
                        msg += " " + relation;
                    msg += "\n";
                }

                //substring to remove last newline
                messages.Add(peerName.Key, msg.Substring(0, msg.Count() - 1));
            }
            return messages;
        }

        public bool CheckBlocked(string[] evs)
        {
            foreach (string e in evs)
            {
                if (events.ContainsKey(e) && events[e].Block.Item1)
                    return true;
            }
            return false;
        }

        public bool TryBlockInternal(Event ev, Guid execId)
        {
            // Exclude
            foreach (EventInternal e in ev.Excludes)
            {
                if (e != ev)
                {
                    e.Semaphore.WaitOne();
                    if (e.Block.Item1 && e.Block.Item2 != execId)
                    {
                        e.Semaphore.Release();
                        return false;
                    }

                    if (e.Block.Item1 == false)
                    {
                        e.Block = new Tuple<bool, Guid>(true, execId);
                        blocked[execId].Add(e);
                    }

                    e.Semaphore.Release();
                }
            }

            // Response
            foreach (EventInternal r in ev.Responses)
            {
                //Special case needs to be covered where event E has a response to itself
                if (r != ev)
                {
                    r.Semaphore.WaitOne();
                    if (r.Block.Item1 == false)
                    {
                        r.Block = new Tuple<bool, Guid>(true, execId);
                        blocked[execId].Add(r);
                    }
                }

                if (r.Included)
                {
                    foreach (EventInternal m in r.Milestones)
                    {
                        if (m != ev)
                        {
                            m.Semaphore.WaitOne();

                            if (m.Block.Item1 && m.Block.Item2 != execId)
                            {
                                m.Semaphore.Release();

                                if (r != ev)
                                    r.Semaphore.Release();

                                return false;
                            }

                            if (m.Block.Item1 == false)
                            {
                                m.Block = new Tuple<bool, Guid>(true, execId);
                                blocked[execId].Add(m);
                            }

                            m.Semaphore.Release();
                        }
                    }
                }
                if (r != ev)
                    r.Semaphore.Release();
            }
            // Includes
            foreach (EventInternal i in ev.Includes)
            {
                i.Semaphore.WaitOne();
                if (i.Block.Item1 && i.Block.Item2 != execId)
                {
                    i.Semaphore.Release();
                    return false;
                }
                foreach (EventInternal c in i.Conditions)
                {
                    if (c != ev)
                    {
                        c.Semaphore.WaitOne();
                        if (c.Block.Item1 && c.Block.Item2 != execId)
                        {
                            c.Semaphore.Release();
                            i.Semaphore.Release();
                            return false;
                        }

                        //if false, we are not already blocking c
                        if (c.Block.Item1 == false)
                        {
                            c.Block = new Tuple<bool, Guid>(true, execId);
                            blocked[execId].Add(c);
                        }

                        c.Semaphore.Release();
                    }
                }
                if (i.Pending)
                {
                    foreach (EventInternal m in i.Milestones)
                    {
                        if (m != ev)
                        {
                            m.Semaphore.WaitOne();
                            if (m.Block.Item1 && m.Block.Item2 != execId)
                            {
                                m.Semaphore.Release();
                                i.Semaphore.Release();
                                return false;
                            }

                            if (m.Block.Item1 == false)
                            {
                                m.Block = new Tuple<bool, Guid>(true, execId);
                                blocked[execId].Add(m);
                            }

                            m.Semaphore.Release();
                        }
                    }
                }
                if (i.Block.Item1 == false)
                {
                    i.Block = new Tuple<bool, Guid>(true, execId);
                    blocked[execId].Add(i);
                }
                
                i.Semaphore.Release();
            }
            return true;
        }

        public bool TryBlockExternal(Dictionary<string, string> peerMessages, Guid executeId,
            string executeTime)
        {
            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>>();

            foreach (string peerName in peerMessages.Keys)
            {
                tasks[peerName] = config.MainNodes[peerName].SendAsync(string.Format("BLOCK {0} {1}\n{2}",
                    executeId, executeTime, peerMessages[peerName]), false);
            }

            Task.WaitAll(tasks.Values.ToArray());

            if (CheckPeerReplies(tasks))
            {
                MessagePeers("UNBLOCK", executeId, tasks, true);
                return false;
            }

            return true;
        }

        public Dictionary<string, Task<string>> MessagePeers(string command, Guid executeId,
            Dictionary<string, Task<string>> tasksPrev, bool lastMsg)
        {
            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>>();

            foreach (string peerName in tasksPrev.Keys)
            {
                if (tasksPrev[peerName].Result != "UNAVAILABLE")
                {
                    tasks[peerName] = config.MainNodes[peerName]
                        .SendAsync(string.Format("{0} {1}", command, executeId), lastMsg);
                }
            }

            Task.WaitAll(tasks.Values.ToArray());

            return tasks;
        }

        public bool CheckPeerReplies(Dictionary<string, Task<string>> tasks) 
        {
            foreach (KeyValuePair<string, Task<string>> t in tasks)
            {
                if (t.Value.Result == "UNAVAILABLE")
                    return true;
            }

            return false;
        }

        public List<(EventInternal, Relation, bool)> GetNewMarkingsReversible(EventForeign ev)
        {
            List<(EventInternal, Relation, bool)> actions =
                new List<(EventInternal, Relation, bool)>();

            //Response
            foreach (EventInternal e2 in ev.Responses)
            {
                if (e2.Included && !e2.Pending)
                {
                    foreach (EventInternal e3 in e2.Milestones)
                        actions.Add((e3, Relation.Milestone, true));
                }

                if (!e2.Pending)
                    actions.Add((e2, Relation.Response, true));
            }

            //Include
            foreach (EventInternal e2 in ev.Includes)
            {
                if (!e2.Included)
                {
                    if (!e2.Executed)
                    {
                        foreach (EventInternal e3 in e2.Conditions)
                            actions.Add((e3, Relation.Condition, true));
                    }

                    if (e2.Pending)
                    {
                        foreach (EventInternal e3 in e2.Milestones)
                            actions.Add((e3, Relation.Milestone, true));
                    }

                    actions.Add((e2, Relation.Include, true));
                }
            }

            //Exclude
            foreach (EventInternal e2 in ev.Excludes)
            {
                if (e2.Included)
                {
                    if (!e2.Executed)
                    {
                        foreach (EventInternal e3 in e2.Conditions)
                            actions.Add((e3, Relation.Condition, false));
                    }

                    if (e2.Pending)
                    {
                        foreach (EventInternal e3 in e2.Milestones)
                            actions.Add((e3, Relation.Milestone, false));
                    }

                    actions.Add((e2, Relation.Exclude, true));
                }
            }

            //Condition
            if (ev.Included)
            {
                foreach (EventInternal e2 in ev.Conditions)
                    actions.Add((e2, Relation.Condition, false));
            }


            //Milestone 
            if (ev.Pending && ev.Included)
            {
                foreach (EventInternal e2 in ev.Milestones)
                    actions.Add((e2, Relation.Milestone, false));
            }

            return actions;
        }
      
        /// <summary>
        /// Updates markings of local events.
        /// </summary>
        /// <param name="ev">Update markings based on the event ev's relations.</param>
        private void UpdateMarkingsInternal(EventInternal ev)
        {
            //Response
            foreach (EventInternal e2 in ev.Responses)
            {
                if (e2.Included && !e2.Pending)
                {
                    foreach (EventInternal e3 in e2.Milestones)
                        e3.Milestone++;
                }

                e2.Pending = true;
            }

            //Include
            foreach (EventInternal e2 in ev.Includes)
            {
                if (!e2.Included)
                {
                    if (!e2.Executed)
                    {
                        foreach (EventInternal e3 in e2.Conditions)
                            e3.Condition++;
                    }

                    if (e2.Pending)
                    {
                        foreach (EventInternal e3 in e2.Milestones)
                            e3.Milestone++;
                    }

                    e2.Included = true;
                }
            }

            //Exclude
            foreach (EventInternal e2 in ev.Excludes)
            {
                if (e2.Included)
                {
                    if (!e2.Executed)
                    {
                        foreach (EventInternal e3 in e2.Conditions)
                            e3.Condition--;
                    }

                    if (e2.Pending)
                    {
                        foreach (EventInternal e3 in e2.Milestones)
                            e3.Milestone--;
                    }
                    e2.Included = false;
                }
                
            }

            //Condition
            if (!ev.Executed && ev.Included)
            {
                foreach (EventInternal e2 in ev.Conditions)
                    e2.Condition--;
            }

            //Milestone
            if (ev.Pending && ev.Included)
            {
                foreach (EventInternal e2 in ev.Milestones)
                    e2.Milestone--;
            }
        }

        public void ChangeMarkings(EventForeign ev, bool revert)
        {
            foreach ((EventInternal, Relation, bool) action in ev.actions)
            {
                EventInternal e = events[action.Item1.Name];
                bool change = revert ? !action.Item3 : action.Item3;

                switch (action.Item2)
                {
                    case Relation.Condition:
                        if (change)
                            e.Condition++;
                        else
                            e.Condition--;

                        break;
                    case Relation.Response:
                        if (change)
                            e.Pending = true;
                        else
                            e.Pending = false;

                        break;
                    case Relation.Milestone:
                        if (change)
                            e.Milestone++;
                        else
                            e.Milestone--;

                        break;
                    case Relation.Include:
                        if (change)
                            e.Included = true;
                        else
                            e.Included = false;

                        break;
                    case Relation.Exclude:
                        if (change)
                            e.Included = false;
                        else
                            e.Included = true;

                        break;
                }
            }
        }

        public bool Execute(string eventName)
        {
            EventInternal ev = events[eventName];

            //Lock semaphore
            ev.Semaphore.WaitOne();
            if (ev.Block.Item1) //Check if ev is Blocked
            {
                ev.Semaphore.Release();
                return false;
            }

            // Execution ID associated with the execution.
            Guid execId = Guid.NewGuid();
            ev.Block = new Tuple<bool, Guid>(true, execId);
            ev.Semaphore.Release();

            blocked.Add(execId, new List<EventInternal>());
            blocked[execId].Add(ev);

            DateTime execTime = DateTime.Now;

            // Blocks internal if the event is enabled.
            if (!ev.Enabled() || !TryBlockInternal(ev, execId))
            {
                UnblockEvents(execId);
                return false;
            }

            // Blocking external events, if any
            Dictionary<string, string> peerMessages = GetPeerMessages(ev);
            if (peerMessages.Count != 0 &&
                !TryBlockExternal(peerMessages, execId, execTime.ToString(config.Culture)))
            {
                UnblockEvents(execId);
                return false;
            }

            Dictionary<string, Task<string>> tasksEmpty = new Dictionary<string, Task<string>>();
            foreach (string p in peerMessages.Keys)
                tasksEmpty.Add(p, Task.FromResult(string.Empty));

            bool externalResult = TryExecuteExternal(tasksEmpty, execId);

            if (externalResult) //If execution was successful externally.
            {
                // Update local markings
                UpdateMarkingsInternal(ev);
                ev.Executed = true;
                ev.Pending = false;

                log.Add(execTime, ev.Name);

                //Unblock relevante peers
                Dictionary<string, Task<string>> tasksUnblock =
                    MessagePeers("UNBLOCK", execId, tasksEmpty, true);

                // Tries to unblock dead peers.
                IEnumerable<string> deadPeers =
                    from x in tasksEmpty.Keys.Except(tasksUnblock.Keys) select x;
                foreach (string deadPeer in deadPeers)
                    TryUnblockPeerAsync(deadPeer, execId.ToString());
            }
            UnblockEvents(execId);
            return externalResult;
        }

        public bool TryExecuteExternal(Dictionary<string, Task<string>> tasks, Guid execId)
        {
            Dictionary<string, Task<string>> tasksExecute = MessagePeers("EXECUTE", execId, tasks, false);

            if (CheckPeerReplies(tasksExecute))
            {
                Dictionary<string, Task<string>> tasksRevert =
                    MessagePeers("REVERT", execId, tasksExecute, true);

                return false;
            }
            else
                return true;
        }


        public async void TryUnblockPeerAsync(string peerName, string execId)
        {
            string reply;
            int tries = 0;
            do
            {
                reply = await config.MainNodes[peerName].SendAsync(string.Format("UNBLOCK {0}", execId), true);
                tries++;
            } 
            while (reply != "SUCCESS" && tries < config.MaxConnectionAttempts);
        }
    }
}