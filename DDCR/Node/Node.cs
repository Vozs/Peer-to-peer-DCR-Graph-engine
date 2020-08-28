using System;
using Node.Interfaces;

namespace Node
{
    public class Node
    {
        public string[] Events { get => events; set => events = value; }
        /// <summary>
        /// List of input that HandleInput() accepts.
        /// </summary>
        private readonly string[,] commands = new string[,]
        {
            {"HELP", "Lists available commands and their usage" },
            {"PERMISSIONS", "Sends a TCP request to the main node for Events which are executable by this node. These events are then added to a local list of known events." },
            {"ACCEPTING", "Sends a TCP request to the main node, to check whether the global graph is in an accepting state. Prints a True or False." },
            {"LOG", "Sends a TCP request to the main node, to receive the global log of all executed events and their timestamps." },
            {"MARKING <Event Name>", "Sends a TCP request to the main node to reply with the current marking of a given event." },
            {"ALLMARKINGS", "Sends a TCP request to the main node to reply with the markings of all its local events" },
            {"EXECUTE <Event Name>", "Sends a TCP request to the main node to execute the provided event. This only works if the event has already been added to the list of local events, using the \"PERMISSION\" command." }
        };

        private string[] events = {""};
        IConfig config;
        public Node(IConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Checks whether the eventName parameter is contained in the Node.events array
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <returns>True if match was found, otherwise false.</returns>
        private bool CheckEventContains(string eventName)
        {
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i] == eventName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Requests execution permissions from the associated MainNode, in order to locally determine what events may be executed in the future.
        /// This is done by saving the reply, which contains the executable events, in the Node.events array.
        /// </summary>
        /// <returns>The reply from MainNode as a string if the Node has permissions to execute any events, otherwise "NONE"</returns>
        public string GetEvents()
        {
            string reply = config.MainNode.Send("PERMISSIONS");
            if (reply != "NONE")
                events = reply.Split(' ');

            return reply;
        }
        /// <summary>
        /// Processes commands that are not associated with an event.
        /// </summary>
        /// <param name="cmd">The command to process</param>
        /// <returns>The result of the command after being processed by the Main Node</returns>
        public string Command(string cmd)
        {
            string write = string.Empty;
            if (cmd == "HELP")
            {
                for (int i = 0; i < commands.Length/2; i++)
                {
                    write += string.Format("{0,-10}: {1}\n\n", commands[i, 0], commands[i, 1]);
                }
            }
            else if (cmd == "PERMISSIONS")
            {
                write = GetEvents();
            }
            else
            {
                write = config.MainNode.Send(cmd);
            }

            return write;
        }

        /// <summary>
        /// Processes commands that are associated with an event.
        /// </summary>
        /// <param name="cmd">The command to process</param>
        /// <param name="eventName">The name of the event associated with the command.</param>
        /// <returns>The result of the command being sent to the associated Main Node, if the event is located in "events". Otherwise an error message .</returns>
        public string Command(string cmd, string eventName)
        {
            if (CheckEventContains(eventName))
            {
                return config.MainNode.Send(string.Format("{0} {1}", cmd, eventName));
            }
            else
                return "Invalid event (local).";
        }

        /// <summary>
        /// Parses input, and determines the type of command that the input matches.
        /// </summary>
        /// <param name="input">The input message</param>
        /// <returns>The result of the command being sent to the associated Main Node, if the command is valid. For  valid commands, see "commands".</returns>
        public string HandleInput(string input)
        {
            var inputs = input.Split(' ');
            inputs[0] = inputs[0].ToUpper();
            bool invalid = true;
            for (int i = 0; i < commands.Length/2; i++)
            {
                if (inputs[0] == commands[i, 0].Split(' ')[0])
                {
                    invalid = false;
                    break;
                }
            }
            if (invalid)
                return string.Format("Invalid command: {0} - Enter \"HELP\" to view commands.", input);
            else if (inputs.Length > 1)
                return Command(inputs[0], inputs[1]);
            else
                return Command(inputs[0]);
        }
    }
}
