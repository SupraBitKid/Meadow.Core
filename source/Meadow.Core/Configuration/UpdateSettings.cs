﻿using Meadow.Update;

namespace Meadow;

/// <summary>
/// An implementation of IUpdateSettings backed by the Meadow applicaication.config file
/// </summary>
public class UpdateSettings : ConfigurableObject, IUpdateSettings
{
    /// <summary>
    /// Gets the desired enabled state of the service
    /// </summary>
    public bool Enabled => GetConfiguredBool(nameof(Enabled), false);
    /// <summary>
    /// Gets the address of the Update (API) server to use
    /// </summary>
    public string UpdateServer => GetConfiguredString(nameof(UpdateServer), "mqtt.meadowcloud.co");
    /// <summary>
    /// Gets the port of the Update (API) server to use
    /// </summary>
    public int UpdatePort => GetConfiguredInt(nameof(UpdatePort), 1883);
    /// <summary>
    /// Gets the address of the authentication server to use
    /// </summary>
    public string AuthServer => GetConfiguredString(nameof(AuthServer), "https://www.meadowcloud.co");
    /// <summary>
    /// Gets the port of the authentication server to use
    /// </summary>
    public int AuthPort => GetConfiguredInt(nameof(AuthPort), 443);
    /// <summary>
    /// Gets the root MQTT topic to subscribe to for updates
    /// </summary>
    public string RootTopic => GetConfiguredString(nameof(RootTopic), "ota;ota/{ID}/updates");
    /// <summary>
    /// Reconnect period used when a disconnection from the Update server occrs
    /// </summary>
    public int CloudConnectRetrySeconds => GetConfiguredInt(nameof(CloudConnectRetrySeconds), 15);
    /// <summary>
    /// Gets the preference for using authentication when connecting to the Update server
    /// </summary>
    public bool UseAuthentication => GetConfiguredBool(nameof(UseAuthentication), true);
    /// <summary>
    /// Gets the Organization the device is registered to
    /// </summary>
    public string Organization => GetConfiguredString(nameof(Organization), "Default organization");
}
