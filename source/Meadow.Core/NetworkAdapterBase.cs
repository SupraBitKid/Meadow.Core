﻿using Meadow.Hardware;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Meadow
{
    /// <summary>
    /// A base class for INetworkAdapter implementations
    /// </summary>
    public abstract class NetworkAdapterBase : INetworkAdapter
    {
        /// <summary>
        /// Raised when the device connects to a network.
        /// </summary>
        public event NetworkConnectionHandler NetworkConnected = delegate { };
        /// <summary>
        /// Raised when the device disconnects from a network.
        /// </summary>
        public event NetworkDisconnectionHandler NetworkDisconnected = delegate { };
        /// <summary>
        /// Raised when a network error occurs
        /// </summary>
        public event NetworkErrorHandler NetworkError = delegate { };

        private NetworkInterface? nativeInterface = default!;

        /// <summary>
        /// returns the connection state of the NetworkAdapter
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Gets the physical (MAC) address of the network adapter
        /// </summary>
        public PhysicalAddress MacAddress { get; private set; } = PhysicalAddress.None;

        /// <summary>
        /// Gets the network interface type
        /// </summary>
        public NetworkInterfaceType InterfaceType { get; }

        /// <summary>
        /// Constructor for the NetworkAdapterBase class
        /// </summary>
        /// <param name="expectedType"></param>
        protected internal NetworkAdapterBase(NetworkInterfaceType expectedType)
        {
            InterfaceType = expectedType;

            Refresh();
        }

        /// <summary>
        /// Raises the <see cref="NetworkConnected"/> event
        /// </summary>
        /// <param name="args"></param>
        protected void RaiseNetworkConnected(WirelessNetworkConnectionEventArgs args)
        {
            NetworkConnected?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="NetworkDisconnected"/> event
        /// </summary>
        protected void RaiseNetworkDisconnected()
        {
            NetworkDisconnected?.Invoke(this);
        }

        /// <summary>
        /// Raises the <see cref="NetworkError"/> event
        /// </summary>
        /// <param name="args"></param>
        protected void RaiseNetworkError(NetworkErrorEventArgs args)
        {
            NetworkError?.Invoke(this, args);
        }

        /// <summary>
        /// Refreshes the NetworkAdapter's information
        /// </summary>
        protected void Refresh()
        {
            nativeInterface = LoadAdapterInfo();
        }

        /// <summary>
        /// IP Address of the network adapter.
        /// </summary>
        public IPAddress IpAddress
        {
            get
            {
                if (nativeInterface == null)
                {
                    return IPAddress.None;
                }

                return nativeInterface?.GetIPProperties()?.UnicastAddresses?.FirstOrDefault()?.Address ?? IPAddress.None;
            }
        }

        /// <summary>
        /// Subnet mask of the adapter.
        /// </summary>
        public IPAddress SubnetMask
        {
            get
            {
                if (nativeInterface == null)
                {
                    return IPAddress.None;
                }

                return nativeInterface?.GetIPProperties()?.UnicastAddresses?.FirstOrDefault()?.IPv4Mask ?? IPAddress.None;
            }
        }

        /// <summary>
        /// Default gateway for the adapter.
        /// </summary>
        public IPAddress Gateway
        {
            get
            {
                if (nativeInterface == null)
                {
                    return IPAddress.None;
                }

                return nativeInterface?.GetIPProperties()?.GatewayAddresses?.FirstOrDefault()?.Address ?? IPAddress.None;
            }
        }

        private NetworkInterface? LoadAdapterInfo()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                if (interfaces.Length > 0)
                {
                    foreach (var intf in interfaces)
                    {
                        var p = intf.GetIPProperties();

                        MacAddress = intf.GetPhysicalAddress();

                        if (intf.NetworkInterfaceType == InterfaceType)
                        {
                            Resolver.Log.Trace($"Interface: {intf.Id}: {intf.Name} {intf.NetworkInterfaceType} {intf.OperationalStatus}");
                            return intf;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Resolver.Log.Error(ex.Message);
            }
            return null;
        }
    }
}
