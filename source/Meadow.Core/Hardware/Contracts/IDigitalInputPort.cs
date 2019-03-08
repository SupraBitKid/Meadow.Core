﻿using System;

namespace Meadow.Hardware
{
    public interface IDigitalInputPort : IDigitalPort
    {
        bool State { get; }

        event EventHandler<PortEventArgs> Changed;
    }
}