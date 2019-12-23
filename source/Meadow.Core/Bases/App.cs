﻿using System;
using Meadow.Hardware;

namespace Meadow
{
    /// <summary>
    /// Provides a base implementation for the Meadow IApp contract. Use this 
    /// class for Meadow applications to get strongly-typed access to the current
    /// device information.
    /// </summary>
    public abstract class App<D, A> : IApp 
        where A : class, IApp 
        where D : class, IIODevice//<P> where P : IPinDefinitions
    {
        /// <summary>
        /// </summary>
        /// <value>The current.</value>
        public static A Current
        {
            get { return _current; }
        } private static A _current;

        protected App()
        {
            _current = this as A;
            _device = Activator.CreateInstance<D>();

            // set our device on the MeadowOS class
            MeadowOS.Init(_device);
        }

        public static D Device
        {
            get { return _device; }
        } private static D _device;

        /// <summary>
        /// Called when the application is put to sleep.
        /// </summary>
        public virtual void WillSleep() {}

        /// <summary>
        /// Called when the application wakes up from sleep.
        /// </summary>
        public virtual void OnWake() {}

        /// <summary>
        /// 
        /// </summary>
        public virtual void WillReset() {}
    }
}
