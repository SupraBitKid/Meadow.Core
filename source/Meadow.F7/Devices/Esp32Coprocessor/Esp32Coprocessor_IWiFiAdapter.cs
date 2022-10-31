﻿using Meadow.Devices.Esp32.MessagePayloads;
using Meadow.Gateway.WiFi;
using Meadow.Hardware;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Devices
{
    /// <summary>
	/// This file holds the WiFi specific methods, properties etc for the IWiFiAdapter interface.
	/// </summary>
    public partial class Esp32Coprocessor : NetworkAdapterBase, IWiFiNetworkAdapter
    {
        #region Events

        /// <summary>
        /// Raised when the device connects to WiFi.
        /// </summary>
        public event NetworkConnectionHandler NetworkConnected = delegate { };

        /// <summary>
        /// Raised when the device disconnects from WiFi.
        /// </summary>
        public event NetworkDisconnectionHandler NetworkDisconnected = delegate { };

        /// <summary>
        /// Raised when the WiFi interface starts.
        /// </summary>
        public event EventHandler WiFiInterfaceStarted = delegate { };

        /// <summary>
        /// Raised when the WiFi interface stops.
        /// </summary>
        public event EventHandler WiFiInterfaceStopped = delegate { };

        /// <summary>
        /// Raise the NTP time changed event.
        /// </summary>
        public event EventHandler NtpTimeChanged = delegate { };

        #endregion Events


        /// <summary>
        /// Default delay between WiFi network scans <see cref="ScanPeriod"/>.
        /// </summary>
        public static TimeSpan DefaultScanPeriod = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Minimum delay that can be used for the <see cref="ScanPeriod"/>.
        /// </summary>
        public static TimeSpan MinimumScanPeriod = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum delay that can be used for the <see cref="ScanPeriod"/>.
        /// </summary>
        public static TimeSpan MaximumScanPeriod = TimeSpan.FromSeconds(60);


        #region Member variables.

        /// <summary>
        /// Lock object to make sure the events and the methods do not try to access
        /// properties simultaneously.
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// 
        /// </summary>
        private readonly Semaphore _connectionSemaphore = new Semaphore(0, 1);

        #endregion Member variables.


        /// <summary>
        /// Record if the WiFi ESP32 is connected to an access point.
        /// </summary>
        public override bool IsConnected { get => _isConnected; }

        /// <summary>
        /// Current onboard antenna in use.
        /// </summary>
        public AntennaType CurrentAntenna => _antenna;

        /// <summary>
        /// Private copy of the currently selected antenna.
        /// </summary>
        protected AntennaType _antenna;

        /// <summary>
        /// MAC address as used by the ESP32 when acting as a client.
        /// </summary>
        public PhysicalAddress MacAddress
        {
            get
            {
                if (_mac == null)
                {
                    byte[] mac = new byte[6];
                    F7PlatformOS.GetByteArray(IPlatformOS.ConfigurationValues.MacAddress, mac);
                    _mac = new PhysicalAddress(mac);
                }
                return _mac;
            }
        }
        private PhysicalAddress _mac;

        /// <summary>
        /// MAC address as used by the ESP32 when acting as an access point.
        /// </summary>
        public PhysicalAddress ApMacAddress
        {
            get
            {
                if (_apMac == null)
                {
                    byte[] mac = new byte[6];
                    F7PlatformOS.GetByteArray(IPlatformOS.ConfigurationValues.SoftApMacAddress, mac);
                    _apMac = new PhysicalAddress(mac);
                }
                return _apMac;
            }
        }
        private PhysicalAddress _apMac;

        /// <summary>
        /// Gets or sets whether to automatically start the network interface when the board reboots.
        /// </summary>
        /// <remarks>
        /// This will automatically connect to any preconfigured access points if they are available.
        /// </remarks>
        public bool AutoConnect
        {
            get => F7PlatformOS.GetBoolean(IPlatformOS.ConfigurationValues.AutomaticallyStartNetwork);
        }

        /// <summary>
        /// Automatically try to reconnect to an access point if there is a problem / disconnection?
        /// </summary>
        public bool AutoReconnect
        {
            get => F7PlatformOS.GetBoolean(IPlatformOS.ConfigurationValues.AutomaticallyReconnect);
        }

        /// <summary>
        /// Default access point to try to connect to if the network interface is started and the board
        /// is configured to automatically reconnect.
        /// </summary>
        public string DefaultSsid => F7PlatformOS.GetString(IPlatformOS.ConfigurationValues.DefaultAccessPoint);

        /// <summary>
        /// Access point the ESP32 is currently connected to.
        /// </summary>
        public string? Ssid { get; private set; }

        /// <summary>
        /// BSSID of the access point the ESP32 is currently connected to.
        /// </summary>
        public PhysicalAddress Bssid { get; private set; }

        /// <summary>
        /// WiFi channel the ESP32 and the access point are using for communication.
        /// </summary>
        public int Channel { get; private set; }

        /// <summary>
        /// The maximum number of times the ESP32 will retry an operation before returning an error.
        /// </summary>
        /// <remarks>
        /// This property enforces a minimum value of 3.
        /// </remarks>

        public uint MaximumRetryCount
        {
            get => F7PlatformOS.GetUInt(IPlatformOS.ConfigurationValues.MaximumNetworkRetryCount);

            set
            {
                uint retryCount = value;
                if (retryCount < 3)
                {
                    retryCount = 3;
                }
                F7PlatformOS.SetUInt(IPlatformOS.ConfigurationValues.MaximumNetworkRetryCount, retryCount);
            }
        }

        /// <summary>
        /// Does the access point the WiFi adapter is currently connected to have internet access?
        /// </summary>
        public bool HasInternetAccess => CurrentState == WiFiState.Connected; // not sure this is true - this just means we're connected

        #region Methods

        /// <summary>
        /// Delay (in milliseconds) between network scans.
        /// </summary>
        /// <remarks>
        /// This will default to the <see cref="DefaultScanPeriod"/> value.
        ///
        /// The ScanFrequency should be between <see cref="MinimumScanPeriod"/> and
        /// <see cref="MaximumScanPeriod"/> (inclusive).
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Exception is thrown if value is less than <see cref="MinimumScanPeriod"/> or greater than <see cref="MaximumScanPeriod"/>.</exception>
        public TimeSpan ScanPeriod
        {
            get { return (_scanPeriod); }
            set
            {
                if ((value < MinimumScanPeriod) || (value > MaximumScanPeriod))
                {
                    throw new ArgumentOutOfRangeException($"{nameof(ScanPeriod)} should be between {MinimumScanPeriod} and {MaximumScanPeriod} (inclusive).");
                }
                _scanPeriod = value;
            }
        }
        private TimeSpan _scanPeriod = DefaultScanPeriod;
        private bool _isConnected;

        /// <summary>
        /// Use the event data to work out which event to invoke and create any event args that will be consumed.
        /// </summary>
        /// <param name="eventId">Event ID.</param>
        /// <param name="statusCode">Status of the event.</param>
        /// <param name="payload">Optional payload containing data specific to the result of the event.</param>
        protected void InvokeEvent(WiFiFunction eventId, StatusCodes statusCode, byte[] payload)
        {
            // look for errors first
            if (statusCode != StatusCodes.CompletedOk)
            {
                _lastStatus = statusCode;
                CurrentState = WiFiState.Error;
                Resolver.Log.Debug($"Wifi function {eventId} returned {statusCode}");
                return;
            }

            switch (eventId)
            {
                case WiFiFunction.ConnectToAccessPointEvent:
                    byte channel = 0;

                    ConnectEventData connectEventData = Encoders.ExtractConnectEventData(payload, 0);
                    lock (_lock)
                    {
                        Ssid = connectEventData.Ssid;
                        Bssid = new PhysicalAddress(connectEventData.Bssid);
                        Channel = channel;
                        _isConnected = true;
                        _authenticationType = (NetworkAuthenticationType)connectEventData.AuthenticationMode;
                    }

                    CurrentState = WiFiState.Connected;
                    break;
                case WiFiFunction.DisconnectFromAccessPointEvent:
                    CurrentState = WiFiState.Disconnected;
                    break;
                case WiFiFunction.StartWiFiInterfaceEvent:
                    RaiseWiFiInterfaceStarted(statusCode, payload);
                    break;
                case WiFiFunction.StopWiFiInterfaceEvent:
                    RaiseWiFiInterfaceStopped(statusCode, payload);
                    break;
                case WiFiFunction.NtpUpdateEvent:
                    RaiseNtpTimeChangedEvent();
                    break;
                default:
                    throw new NotImplementedException($"WiFi event not implemented ({eventId}).");
            }
        }

        /// <summary>
        /// Clear the IP address, subnet mask and gateway details.
        /// </summary>
        private void ClearNetworkDetails()
        {
            lock (_lock)
            {
                byte[] addressBytes = new byte[4];
                Array.Clear(addressBytes, 0, addressBytes.Length);
                Ssid = string.Empty;
                Bssid = PhysicalAddress.None;
                Channel = 0;
            }
        }

        // TODO: Mark, this should be async. But i think it requires the `SendCommand()` method to be async.
        /// <summary>
        /// Scan for networks.
        /// </summary>
        /// <remarks>
        /// The network must be started before this method can be called.
        /// </remarks>
        public async Task<IList<WifiNetwork>> Scan(TimeSpan timeout)
        {
            var src = new CancellationTokenSource();
            return await Scan(timeout, src.Token);
        }

        /// <summary>
        /// Scan for WiFiNetworks (access points).
        /// </summary>
        /// <param name="token">Cancellation token for the connection attempt</param>
        /// <returns>List of WiFiNetwork objects.</returns>
        public async Task<IList<WifiNetwork>> Scan(CancellationToken token)
        {
            return await Scan(TimeSpan.Zero, token);
        }

        /// <summary>
        /// Scan for WiFiNetworks (access points).
        /// </summary>
        /// <param name="timeout">Length of time to run the scan for before the scan is declared a failure.</param>
        /// <param name="token">Cancellation token for the connection attempt</param>
        /// <returns>List of WiFiNetwork objects.</returns>
        private Task<IList<WifiNetwork>> Scan(TimeSpan timeout, CancellationToken token)
        {
            var networks = new List<WifiNetwork>();
            var resultBuffer = new byte[MAXIMUM_SPI_BUFFER_LENGTH];
            var tasks = new List<Task>();

            var scanTask = Task.Run(() =>
              {
                  token.ThrowIfCancellationRequested();

                  try
                  {
                      // note: this is synchronous, so all we can really do is wait for completion in the background and throw away the result
                      var result = SendCommand((byte)Esp32Interfaces.WiFi, (UInt32)WiFiFunction.GetAccessPoints, true, resultBuffer);

                      token.ThrowIfCancellationRequested();

                      if (result == StatusCodes.CompletedOk)
                      {
                          var accessPointList = Encoders.ExtractAccessPointList(resultBuffer, 0);
                          var accessPoints = new AccessPoint[accessPointList.NumberOfAccessPoints];

                          if (accessPointList.NumberOfAccessPoints > 0)
                          {
                              int accessPointOffset = 0;
                              for (int count = 0; count < accessPointList.NumberOfAccessPoints; count++)
                              {
                                  var accessPoint = Encoders.ExtractAccessPoint(accessPointList.AccessPoints, accessPointOffset);
                                  accessPointOffset += Encoders.EncodedAccessPointBufferSize(accessPoint);
                                  var bssid = new PhysicalAddress(accessPoint.Bssid);
                                  var network = new WifiNetwork(accessPoint.Ssid, bssid, NetworkType.Infrastructure, PhyType.Unknown,
                                      new NetworkSecuritySettings((NetworkAuthenticationType)accessPoint.AuthenticationMode, NetworkEncryptionType.Unknown),
                                      accessPoint.PrimaryChannel, (NetworkProtocol)accessPoint.Protocols, accessPoint.Rssi);
                                  networks.Add(network);
                              }
                          }
                      }
                      else
                      {
                          Console.WriteLine($"Error getting access points: {result}");
                      }
                      return (networks);
                  }
                  catch (Exception ex)
                  {
                      Console.WriteLine($"Error getting access points: {ex.Message}");

                      token.ThrowIfCancellationRequested();
                      throw ex;
                  }
              }, token);

            tasks.Add(scanTask);

            if (timeout.TotalMilliseconds > 0)
            {
                tasks.Add(Task.Delay(timeout));
            }

            var index = Task.WaitAny(tasks.ToArray());
            if (index == 1)
            {
                throw new TimeoutException();
            }

            return Task.FromResult(scanTask.Result as IList<WifiNetwork>);
        }

        /// <summary>
        /// Start the network interface on the WiFi adapter.
        /// </summary>
        /// <remarks>
        /// This method starts the network interface hardware.  The result of this action depends upon the
        /// settings stored in the WiFi adapter memory.
        ///
        /// No Stored Configuration
        /// If no settings are stored in the adapter then the hardware will simply start.  IP addresses
        /// will not be obtained in this mode.
        ///
        /// In this case, the return result indicates if the hardware started successfully.
        ///
        /// Stored Configuration Present NOTE NOT IMPLEMENTED IN THIS RELEASE
        /// If a default access point (and optional password) are stored in the adapter then the network
        /// interface and the system is set to connect at startup then the system will then attempt to
        /// connect to the specified access point.
        ///
        /// In this case, the return result indicates if the interface was started successfully and a
        /// connection to the access point was made.
        /// </remarks>
        /// <returns>true if the adapter was started successfully, false if there was an error.</returns>
        public async Task<bool> StartWiFiInterface()
        {
            return await Task.Run(() =>
            {
                StatusCodes result = SendCommand((byte)Esp32Interfaces.WiFi, (UInt32)WiFiFunction.StartWiFiInterface, true, null);
                return (result == StatusCodes.CompletedOk);
            });
        }

        /// <summary>
        /// Stop the WiFi interface,
        /// </summary>
        /// <remarks>
        /// Stopping the WiFi interface will release all resources associated with the WiFi running on the ESP32.
        ///
        /// Errors could occur if the adapter was not started.
        /// </remarks>
        /// <returns>true if the adapter was successfully turned off, false if there was a problem.</returns>
        public async Task<bool> StopWiFiInterface()
        {
            return await Task.Run(() =>
            {
                StatusCodes result = SendCommand((byte)Esp32Interfaces.WiFi, (UInt32)WiFiFunction.StopWiFiInterface, true, null);
                return (result == StatusCodes.CompletedOk);
            });
        }

        /// <summary>
        /// Connect to a WiFi network.
        /// </summary>
        /// <param name="ssid">SSID of the network to connect to</param>
        /// <param name="password">Password for the WiFi access point.</param>
        /// <param name="timeout">Timeout period for the connection attempt</param>
        /// <param name="token">Cancellation token for the connection attempt</param>
        /// <param name="reconnection">Determine if the adapter should automatically attempt to reconnect (see <see cref="ReconnectionType"/>) to the access point if it becomes disconnected for any reason.</param>
        /// <returns>The connection result</returns>
        public async Task Connect(string ssid, string password, TimeSpan timeout, CancellationToken token, ReconnectionType reconnection = ReconnectionType.Automatic)
        {
            switch (CurrentState)
            {
                case WiFiState.Connecting:
                    throw new Exception("Adapter already connecting");
                case WiFiState.Disconnecting:
                    throw new Exception("Adapter is in the process of disconnecting");
            }

            if (string.IsNullOrEmpty(ssid))
            {
                throw new ArgumentNullException("Invalid SSID.");
            }
            if (password == null)
            {
                throw new ArgumentNullException($"{nameof(password)} cannot be null.");
            }

            CurrentState = WiFiState.Connecting;

            var connectTask = Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();

                WiFiCredentials request = new WiFiCredentials()
                {
                    NetworkName = ssid,
                    Password = password
                };
                byte[] encodedPayload = Encoders.EncodeWiFiCredentials(request);
                byte[] resultBuffer = new byte[MAXIMUM_SPI_BUFFER_LENGTH];

                ClearNetworkDetails();

                try
                {
                    // TODO: should be async and awaited
                    var result = SendCommand((byte)Esp32Interfaces.WiFi, (UInt32)WiFiFunction.ConnectToAccessPoint, true, encodedPayload, resultBuffer);

                    // NOTE: 'result' here is only the result of the ioctl, *not* the result of the connection request
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to access point: {ex.Message}");

                    token.ThrowIfCancellationRequested();
                    throw ex;
                }

                token.ThrowIfCancellationRequested();

                var t = 0;

                // wait for a state transition
                while (CurrentState == WiFiState.Connecting)
                {
                    await Task.Delay(500);
                    t += 500;
                    if ((timeout.TotalMilliseconds > 0) && (t > timeout.TotalMilliseconds))
                    {
                        throw new TimeoutException();
                    }
                }
            }, token);

            await connectTask;

            if (CurrentState == WiFiState.Error)
            {
                throw new Exception($"Connection error: {_lastStatus}");
            }
        }

        /// <summary>
        /// Disconnect from the current access point.
        /// </summary>
        /// <param name="turnOffWiFiInterface">Stop the WiFi interface.</param>
        /// <returns></returns>
        public async Task<ConnectionResult> Disconnect(bool turnOffWiFiInterface)
        {
            var t = await Task.Run<ConnectionResult>(() =>
            {
                StatusCodes result = DisconnectFromAccessPoint(turnOffWiFiInterface);
                ConnectionResult connectionResult;
                switch (result)
                {
                    case StatusCodes.CompletedOk:
                        ClearNetworkDetails();
                        connectionResult = new ConnectionResult(ConnectionStatus.Success);
                        break;
                    case StatusCodes.Failure:
                        connectionResult = new ConnectionResult(ConnectionStatus.UnspecifiedFailure);
                        break;
                    case StatusCodes.EspWiFiNotStarted:
                        connectionResult = new ConnectionResult(ConnectionStatus.WiFiNotStarted);
                        break;
                    default:
                        connectionResult = new ConnectionResult(ConnectionStatus.UnspecifiedFailure);
                        break;
                }
                return (connectionResult);
            });
            return (t);
        }

        /// <summary>
        /// Disconnect from the the currently active access point.
        /// </summary>
        /// <remarks>
        /// Setting turnOffWiFiInterface to true will call StopWiFiInterface following
        /// the disconnection from the current access point.
        /// </remarks>
        /// <param name="turnOffWiFiInterface">Should the WiFi interface be turned off?</param>
        private StatusCodes DisconnectFromAccessPoint(bool turnOffWiFiInterface)
        {
            DisconnectFromAccessPointRequest request = new DisconnectFromAccessPointRequest()
            {
                TurnOffWiFiInterface = (byte)((turnOffWiFiInterface ? 1 : 0) & 0xff)
            };
            byte[] encodedRequest = Encoders.EncodeDisconnectFromAccessPointRequest(request);

            StatusCodes result = SendCommand((byte)Esp32Interfaces.WiFi, (UInt32)WiFiFunction.DisconnectFromAccessPoint, true, encodedRequest, null);
            return (result);
        }

        /// <summary>
        /// Change the current WiFi antenna.
        /// </summary>
        /// <remarks>
        /// Allows the application to change the current antenna used by the WiFi adapter.  This
        /// can be made to persist between reboots / power cycles by setting the persist option
        /// to true.
        /// </remarks>
        /// <param name="antenna">New antenna to use.</param>
        /// <param name="persist">Make the antenna change persistent.</param>
        public void SetAntenna(AntennaType antenna, bool persist = true)
        {
            if (antenna == AntennaType.NotKnown)
            {
                throw new ArgumentException("Setting the antenna type NotKnown is not allowed.");
            }

            SetAntennaRequest request = new SetAntennaRequest();
            if (persist)
            {
                request.Persist = 1;
            }
            else
            {
                request.Persist = 0;
            }
            if (antenna == AntennaType.OnBoard)
            {
                request.Antenna = (byte)AntennaTypes.OnBoard;
            }
            else
            {
                request.Antenna = (byte)AntennaTypes.External;
            }
            byte[] encodedPayload = Encoders.EncodeSetAntennaRequest(request);
            byte[] encodedResult = new byte[4000];
            StatusCodes result = SendCommand((byte)Esp32Interfaces.WiFi, (UInt32)WiFiFunction.SetAntenna, true, encodedPayload, encodedResult);
            if (result == StatusCodes.CompletedOk)
            {
                _antenna = antenna;
            }
            else
            {
                throw new Exception("Failed to change the antenna in use.");
            }
        }

        #endregion Methods

        #region Event raising methods

        /// <summary>
        /// Process the Disconnected event extracting any event data from the
        /// payload and create an EventArg object if necessary
        /// </summary>
        /// <param name="statusCode">Status code for the WiFi disconnection request.</param>
        /// <param name="payload">Event data encoded in the payload.</param>
        protected void RaiseWiFiDisconnected(StatusCodes statusCode, byte[] payload)
        {
            ClearNetworkDetails();
            _isConnected = false;

            var e = new WiFiDisconnectEventArgs(statusCode);
            NetworkDisconnected?.Invoke(this);
        }

        /// <summary>
        /// Process the InterfaceStarted event extracing any event data from the
        /// payload and create an EventArg object if necessary
        /// </summary>
        /// <param name="statusCode">Status code for the WiFi interface start event (should be CompletedOK).</param>
        /// <param name="payload">Event data encoded in the payload.</param>
        protected void RaiseWiFiInterfaceStarted(StatusCodes statusCode, byte[] payload)
        {
            WiFiInterfaceStartedEventArgs e = new WiFiInterfaceStartedEventArgs(statusCode);
            WiFiInterfaceStarted?.Invoke(this, e);
        }

        /// <summary>
        /// Process the InterfaceStopped event extracing any event data from the
        /// payload and create an EventArg object if necessary
        /// </summary>
        /// <param name="statusCode">Status code for the WiFi interface stop event (should be CompletedOK).</param>
        /// <param name="payload">Event data encoded in the payload.</param>
        protected void RaiseWiFiInterfaceStopped(StatusCodes statusCode, byte[] payload)
        {
            WiFiInterfaceStoppedEventArgs e = new WiFiInterfaceStoppedEventArgs(statusCode);
            WiFiInterfaceStopped?.Invoke(this, e);
        }

        /// <summary>
        /// Process the NtpTimeChanged event.
        /// </summary>
        protected void RaiseNtpTimeChangedEvent()
        {
            NtpTimeChangedEventArgs e = new NtpTimeChangedEventArgs();

            NtpTimeChanged?.Invoke(this, e);
        }

        #endregion Event raising methods

        private WiFiState _state;
        private NetworkAuthenticationType _authenticationType;
        private StatusCodes _lastStatus;

        private WiFiState CurrentState
        {
            get => _state;
            set
            {
                if (value == _state) return;

                _state = value;

                switch (CurrentState)
                {
                    case WiFiState.Connecting:
                        break;
                    case WiFiState.Connected:
                        var args = new WirelessNetworkConnectionEventArgs(IpAddress, SubnetMask, Gateway, Ssid, Bssid, (byte)Channel, _authenticationType);

                        NetworkConnected?.Invoke(this, args);
                        break;
                    case WiFiState.Disconnecting:
                        break;
                    case WiFiState.Disconnected:
                        break;
                    case WiFiState.Error:
                        break;
                }
            }
        }

        private enum WiFiState
        {
            Unknown,
            Disconnected,
            Connecting,
            Connected,
            Disconnecting,
            Error
        }
    }
}
