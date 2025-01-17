﻿using Meadow.Hardware;
using System.Collections;
using System.Collections.Generic;

namespace Meadow
{
    /// <summary>
    /// A collection of INetworkAdapter-derived instances
    /// </summary>
    public class NetworkAdapterCollection : INetworkAdapterCollection
    {
        /// <summary>
        /// Event raised when a network is connected on any adapter
        /// </summary>
        public event NetworkConnectionHandler NetworkConnected = delegate { };
        /// <summary>
        /// Event raised when a network is disconnected on any adapter
        /// </summary>
        public event NetworkDisconnectionHandler NetworkDisconnected = delegate { };

        private List<INetworkAdapter> _adapters = new List<INetworkAdapter>();

        /// <summary>
        /// Gets an INetworkAdapter from the collection by position index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public INetworkAdapter this[int index] => _adapters[index];

        /// <summary>
        /// Adds an INetworkAdapter to the collection
        /// </summary>
        /// <param name="adapter"></param>
        public void Add(INetworkAdapter adapter)
        {
            _adapters.Add(adapter);

            adapter.NetworkConnected += (s, e) => NetworkConnected.Invoke(s, e);
            adapter.NetworkDisconnected += (s) => NetworkDisconnected.Invoke(s);
        }

        /// <summary>
        /// Enumerates all INetworkAdapters in the collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator<INetworkAdapter> GetEnumerator()
        {
            return _adapters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
