namespace DDCR
{
    /// <summary>
    /// A struct representing an EventInternal located on another, known MainNode.
    /// </summary>
    public struct EventExternal
    {
        /// <summary>
        /// The name of the event
        /// </summary>
        public string Name;
        /// <summary>
        /// The label of the event
        /// </summary>
        public string Label;
        /// <summary>
        /// The name of the peer that this EventExternal instance is located on. Used to index in the MainNodes dictionary.
        /// </summary>
        public string Peer;

        /// <summary>
        /// Creates a struct representing an EventInternal, which is located on another known MainNode.
        /// </summary>
        /// <param name="name">The name of the external event. Must be identical to the name on the external MainNode which the event is located on.</param>
        /// <param name="label">Currently unused</param>
        /// <param name="peer">Name of the peer which the external event is located. Peer should reference a key in the MainNodes dictionary in the Graph class</param>
        public EventExternal(string name, string label, string peer)
        {
            Name = name;
            Label = label;
            Peer = peer;
        }
    }
}