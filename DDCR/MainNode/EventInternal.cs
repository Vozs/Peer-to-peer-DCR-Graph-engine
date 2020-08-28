using System;
using System.Collections.Generic;
using System.Threading;

namespace DDCR
{
    /// <summary>
    /// Represents an internal event in the graph.
    /// </summary>
    public class EventInternal : Event
    {
        /// <summary>
        /// Semaphore used in order to safely modify the Block field.
        /// </summary>
        public Semaphore Semaphore = new Semaphore(1, 1);
        /// <summary>
        /// The name of the event
        /// </summary>
        public string Name;
        /// <summary>
        /// The label of the event
        /// </summary>
        public string Label;

        /// <summary>
        /// Boolean indicating whether the event has been executed before.
        /// </summary>
        public bool Executed = false;

        /// <summary>
        /// A tuple indicating whether or not an event is blocked and by which ID is blocking it.
        /// Blocking means that the EventInternal is effectively Locked, meaning its properties cannot be modified by anyone other than the method that Blocked it, indicated by the Guid.
        /// </summary>
        public Tuple<bool, Guid> Block { get => block; set => block = value; }


        private Tuple<bool, Guid> block = new Tuple<bool, Guid>(false, Guid.Empty);
        public HashSet<EventExternal> ConditionsExternal = new HashSet<EventExternal>();
        public HashSet<EventExternal> ResponsesExternal = new HashSet<EventExternal>();
        public HashSet<EventExternal> MilestonesExternal = new HashSet<EventExternal>();
        public HashSet<EventExternal> IncludesExternal = new HashSet<EventExternal>();
        public HashSet<EventExternal> ExcludesExternal = new HashSet<EventExternal>();

        public EventInternal(string name, string label, bool included = true, bool pending = false, bool executed = false)
        {
            Name = name;
            Label = label;
            Included = included;
            Pending = pending;
            Executed = executed;
        }
        /// <summary>
        /// Checks the markings of an event to see if it is enabled.
        /// </summary>
        /// <returns>Returns a bool indicating whether or not an event is enabled.</returns>
        public bool Enabled()
        {
            if (Included && Condition <= 0 && Milestone <= 0)
                return true;
            else
                return false;
        }
    }
}