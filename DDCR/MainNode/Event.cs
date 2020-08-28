using System.Collections.Generic;

namespace DDCR
{
    /// <summary>
    /// Abstract class that models internal relations and markings of an event.
    /// </summary>
    public abstract class Event
    {
        public HashSet<EventInternal> Conditions = new HashSet<EventInternal>();
        public HashSet<EventInternal> Responses = new HashSet<EventInternal>();
        public HashSet<EventInternal> Milestones = new HashSet<EventInternal>();
        public HashSet<EventInternal> Includes = new HashSet<EventInternal>();
        public HashSet<EventInternal> Excludes = new HashSet<EventInternal>();

        /// <summary>
        /// Boolean indicating whether the Event is included.
        /// </summary>
        public bool Included;
        /// <summary>
        /// Boolean indicating whether the Event is in a pending state.
        /// </summary>
        public bool Pending;
        /// <summary>
        /// Boolean indicating whether the Event is currently blocked by another Event's Condition relation.
        /// </summary>
        public int Condition = 0;
        /// <summary>
        /// Boolean indicating whether the Event is currently blocked by another Event's Milestone relation.
        /// </summary>
        public int Milestone = 0;
    }
}