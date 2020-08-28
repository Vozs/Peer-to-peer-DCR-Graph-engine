using DDCR.Interfaces;
using System;
using System.Collections.Generic;

namespace DDCR
{
    /// <summary>
    /// Represents a Log of executed events, along with their timestamps.
    /// </summary>
    public interface ILog : IList<(DateTime, string)>
    {
        /// <summary>
        /// Adds an entry to the Log
        /// </summary>
        /// <param name="time">The date and time the event was executed.</param>
        /// <param name="eventName">The unique name of the event.</param>
        void Add(DateTime time, string eventName);

        /// <summary>
        /// Retrieves a global Log of all executed events in the entire graph by requesting local Logs from all MainNodes
        /// </summary>
        /// <param name="peers">Peers to propagate the request to</param>
        /// <param name="prevPeers">Peers that have already received a request, and thus should be ignored</param>
        /// <param name="id">Guid for the current Log request</param>
        /// <returns>A sorted Log containing all local Logs retrieved from the peers.</returns>
        ILog GetGlobal(Dictionary<string, IClient> peers, string prevPeers, Guid id);

        /// <summary>
        /// Sorts the log by the DateTime, such that the earliest entries are first.
        /// </summary>
        void SortLog();
        /// <summary>
        /// Converts the Log object to an easily readable string, using the Date/Time format defined in Config.Culture.
        /// </summary>
        /// <returns>The Log as a string</returns>
        string ToString();
    }
}