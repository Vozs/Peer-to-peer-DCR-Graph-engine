using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDCR
{
    /// <summary>
    /// Represents a tempoary event from another graph. Contains markings to be changed a peers to propagate to.
    /// </summary>
    public class EventForeign : Event
    {
        /// <summary>
        /// A list of triples containing which marking should change and how.
        /// </summary>
        public IEnumerable<(EventInternal, Relation, bool)> actions;

        /// <summary>
        /// Dictionary of peers to propagate to.
        /// </summary>
        public Dictionary<string, Task<string>> peersPropagate =
            new Dictionary<string, Task<string>>();

        public EventForeign()
        {
            Included = true;
            Pending = false;
        }
    }
}