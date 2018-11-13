//
// Copyright (c) 2017 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;

namespace Windows.Devices.I2c
{
    /// <summary>
    /// Represents the I2C controller for the system.
    /// </summary>
	public sealed class I2cController
    {
        // this is used as the lock object 
        // a lock is required because multiple threads can access the I2C controller
        readonly static object _syncLock = new object();

        // we can have only one instance of the I2cController
        // need to do a lazy initialization of this field to make sure it exists when called elsewhere.
        private static I2cController s_instance;

        // backing field for DeviceCollection
        private static Hashtable s_deviceCollection;

        // field to keep track on how many different I2C buses are being used
        private static ArrayList s_busIdCollection;

        /// <summary>
        /// Device collection associated with this <see cref="I2cController"/>.
        /// </summary>
        /// <remarks>
        /// This collection is for internal use only.
        /// </remarks>
        internal static Hashtable DeviceCollection
        {
            get
            {
                if (s_deviceCollection == null)
                {
                    lock (_syncLock)
                    {
                        if (s_deviceCollection == null)
                        {
                            s_deviceCollection = new Hashtable();
                        }
                    }
                }

                return s_deviceCollection;
            }

            set
            {
                s_deviceCollection = value;
            }
        }
        /// <summary>
        /// I2C bus collection associated with this <see cref="I2cController"/>.
        /// </summary>
        /// <remarks>
        /// This collection is for internal use only.
        /// </remarks>
        internal static ArrayList BusIdCollection
        {
            get
            {
                if (s_busIdCollection == null)
                {
                    lock (_syncLock)
                    {
                        if (s_busIdCollection == null)
                        {
                            s_busIdCollection = new ArrayList();
                        }
                    }
                }

                return s_busIdCollection;
            }

            set
            {
                s_busIdCollection = value;
            }
        }

        /// <summary>
        /// Gets the default I2C controller on the system.
        /// </summary>
        /// <returns>The default I2C controller on the system, or null if the system has no I2C controller.</returns>
        public static I2cController GetDefault()
        {
            if (s_instance == null)
            {
                lock (_syncLock)
                {
                    if (s_instance == null)
                    {
                        s_instance = new I2cController();
                    }
                }
            }

            return s_instance;
        }

        /// <summary>
        /// Gets the I2C device with the specified settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>The desired connection settings.</returns>
        public I2cDevice GetDevice(I2cConnectionSettings settings)
        {
            //TODO: fix return value. Should return an existing device (if any)
            return new I2cDevice(String.Empty, settings);
        }
    }
}
