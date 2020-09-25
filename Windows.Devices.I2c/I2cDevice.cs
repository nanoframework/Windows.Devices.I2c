//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

namespace Windows.Devices.I2c
{
    /// <summary>
    /// Represents a communications channel to a device on an inter-integrated circuit (I2C) bus.
    /// </summary>
	public sealed class I2cDevice : IDisposable
    {
        // the device unique ID for the device is achieve by joining the I2C bus ID and the slave address
        // should be unique enough by encoding it as: (I2C bus number x 1000 + slave address)
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private const int deviceUniqueIdMultiplier = 1000;

        // this is used as the lock object 
        // a lock is required because multiple threads can access the device
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private object _syncLock;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly int _deviceId;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly I2cConnectionSettings _connectionSettings;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private bool _disposed;

        internal I2cDevice(string i2cBus, I2cConnectionSettings settings)
        {
            // generate a unique ID for the device by joining the I2C bus ID and the slave address, should be pretty unique
            // the encoding is (I2C bus number x 1000 + slave address)
            // i2cBus is an ASCII string with the bus name in format 'I2Cn'
            // need to grab 'n' from the string and convert that to the integer value from the ASCII code (do this by subtracting 48 from the char value)
            var controllerId = i2cBus[3] - 48;
            var deviceId = (controllerId * deviceUniqueIdMultiplier) + settings.SlaveAddress;

            I2cController controller = I2cController.FindController(controllerId);

            if (controller == null)
            {
                // this controller doesn't exist yet, create it...
                controller = new I2cController(i2cBus);
            }

            // check if this device ID already exists
            var device = FindDevice(controller, deviceId);

            if (device == null)
            {
                // device doesn't exist, create it...
                _connectionSettings = new I2cConnectionSettings(settings.SlaveAddress)
                {
                    BusSpeed = settings.BusSpeed,
                    SharingMode = settings.SharingMode
                };

                // save device ID
                _deviceId = deviceId;

                // call native init to allow HAL/PAL inits related with I2C hardware
                NativeInit();

                // ... and add this device
                controller.DeviceCollection.Add(this);

                _syncLock = new object();
            }
            else
            {
                // this device already exists, throw an exception
                throw new I2cDeviceAlreadyInUseException();
            }
        }

        /// <summary>
        /// Gets the connection settings used for communication with the inter-integrated circuit (I2C) device.
        /// </summary>
        /// <value>
        /// The connection settings used for communication with the inter-integrated circuit (I2C) device.
        /// </value>
        public I2cConnectionSettings ConnectionSettings
        {
            get
            {
                lock (_syncLock)
                {
                    // check if device has been disposed
                    if (!_disposed)
                    {
                        // need to return a copy so that the caller doesn't change the settings
                        return new I2cConnectionSettings(_connectionSettings);
                    }

                    throw new ObjectDisposedException();
                }
            }
        }

        /// <summary>
        /// Gets the plug and play device identifier of the inter-integrated circuit (I2C) bus controller for the device.
        /// </summary>
        /// <value>
        /// The plug and play device identifier of the inter-integrated circuit (I2C) bus controller for the device.
        /// </value>
        public string DeviceId
        {
            get
            {
                lock (_syncLock)
                {
                    // check if device has been disposed
                    if (!_disposed) { return _deviceId.ToString(); }

                    throw new ObjectDisposedException();
                }
            }
        }

        /// <summary>
        /// Retrieves an <see cref="I2cDevice"/> object for the inter-integrated circuit (I2C) bus controller that has the specified plug and play device identifier, using the specified connection settings.
        /// </summary>
        /// <param name="i2cBus">The plug and play device identifier of the I2C bus controller for which you want to create an <see cref="I2cDevice"/> object.</param>
        /// <param name="settings">The connection settings to use for communication with the I2C bus controller that deviceId specifies.</param>
        /// <returns>An operation that returns the I2cDevice object.</returns>
        /// <remarks>
        /// This method is specific to nanoFramework. The equivalent method in the UWP API is: FromIdAsync.
        /// </remarks>
        /// <exception cref="I2cDeviceAlreadyInUseException">T</exception>
        public static I2cDevice FromId(String i2cBus, I2cConnectionSettings settings)
        {
            return new I2cDevice(i2cBus, settings);
        }

        /// <summary>
        /// Retrieves an Advanced Query Syntax (AQS) string for the inter-integrated circuit (I2C) bus that has the specified friendly name. You can use this string with the DeviceInformation.FindAll
        /// method to get a DeviceInformation object for that bus.
        /// </summary>
        /// <param name="friendlyName">A friendly name for the particular I2C bus on a particular hardware platform for which you want to get the AQS string.</param>
        /// <returns>An AQS string for the I2C bus that friendlyName specifies, which you can use with the DeviceInformation.FindAllAsync method to get a DeviceInformation object for that bus.</returns>
        public static string GetDeviceSelector(String friendlyName)
        {
            return GetDeviceSelector();
        }

        /// <summary>
        /// Retrieves an Advanced Query Syntax (AQS) string for all of the inter-integrated circuit (I2C) bus controllers on the system. You can use this string with the DeviceInformation.FindAll
        /// method to get DeviceInformation objects for those bus controllers.
        /// </summary>
        /// <returns>An AQS string for all of the I2C bus controllers on the system, which you can use with the DeviceInformation.FindAllAsync method to get DeviceInformation 
        /// objects for those bus controllers.</returns>
        public static string GetDeviceSelector()
        {
            return I2cController.GetDeviceSelector();
        }

        /// <summary>
        /// Reads data from the inter-integrated circuit (I2C) bus on which the device is connected into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to which you want to read the data from the I2C bus. The length of the buffer determines how much data to request from the device.</param>
        public void Read(Byte[] buffer)
        {
            ReadPartial(buffer);
        }

        /// <summary>
        /// Reads data from the inter-integrated circuit (I2C) bus on which the device is connected into the specified buffer, and returns information about the success of the 
        /// operation that you can use for error handling.
        /// </summary>
        /// <param name="buffer">The buffer to which you want to read the data from the I2C bus. The length of the buffer determines how much data to request from the device.</param>
        /// <returns>A structure that contains information about the success of the read operation and the actual number of bytes that the operation read into the buffer.</returns>
        public I2cTransferResult ReadPartial(Byte[] buffer)
        {
            lock (_syncLock)
            {
                // check if device has been disposed
                if (_disposed) throw new ObjectDisposedException();

                return NativeTransmit(null, buffer);
            }
        }

        /// <summary>
        /// Writes data to the inter-integrated circuit (I2C) bus on which the device is connected, based on the bus address specified in the I2cConnectionSetting s object
        /// that you used to create the I2cDevice object.
        /// </summary>
        /// <param name="buffer">A buffer that contains the data that you want to write to the I2C device. This data should not include the bus address.</param>
        public void Write(Byte[] buffer)
        {
            WritePartial(buffer);
        }

        /// <summary>
        /// Writes data to the inter-integrated circuit (I2C) bus on which the device is connected, and returns information about the success of the operation that you can use for error handling.
        /// </summary>
        /// <param name="buffer">A buffer that contains the data that you want to write to the I2C device. This data should not include the bus address.</param>
        /// <returns>A structure that contains information about the success of the write operation and the actual number of bytes that the operation wrote into the buffer.</returns>
        public I2cTransferResult WritePartial(Byte[] buffer)
        {
            lock (_syncLock)
            {
                // check if device has been disposed
                if (_disposed) throw new ObjectDisposedException();

                return NativeTransmit(buffer, null);
            }
        }

        /// <summary>
        /// Performs an atomic operation to write data to and then read data from the inter-integrated circuit (I2C) bus on which the device is connected, and sends a restart
        /// condition between the write and read operations.
        /// </summary>
        /// <param name="writeBuffer">A buffer that contains the data that you want to write to the I2C device. This data should not include the bus address.</param>
        /// <param name="readBuffer">The buffer to which you want to read the data from the I2C bus. The length of the buffer determines how much data to request from the device.</param>
        public void WriteRead(Byte[] writeBuffer, Byte[] readBuffer)
        {
            WriteReadPartial(writeBuffer, readBuffer);
        }

        /// <summary>
        /// Performs an atomic operation to write data to and then read data from the inter-integrated circuit (I2C) bus on which the device is connected, and returns information about the
        /// success of the operation that you can use for error handling.
        /// </summary>
        /// <param name="writeBuffer">A buffer that contains the data that you want to write to the I2C device. This data should not include the bus address.</param>
        /// <param name="readBuffer">The buffer to which you want to read the data from the I2C bus. The length of the buffer determines how much data to request from the device.</param>
        /// <returns>A structure that contains information about whether both the read and write parts of the operation succeeded and the sum of the actual number of bytes that the
        /// operation wrote and the actual number of bytes that the operation read.</returns>
        public I2cTransferResult WriteReadPartial(Byte[] writeBuffer, Byte[] readBuffer)
        {
            lock (_syncLock)
            {
                // check if device has been disposed
                if (_disposed) throw new ObjectDisposedException();

                return NativeTransmit(writeBuffer, readBuffer);
            }
        }

        #region IDisposable Support

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                bool disposeController = false;

                if (disposing)
                {
                    // get the controller
                    var controller = I2cController.FindController(_deviceId / deviceUniqueIdMultiplier);

                    if (controller != null)
                    {
                        // find device
                        var device = FindDevice(controller, _deviceId);

                        if (device != null)
                        {
                            // remove from device collection
                            controller.DeviceCollection.Remove(device);

                            // it's OK to also remove the controller, if there is no other device associated
                            if (controller.DeviceCollection.Count == 0)
                            {
                                I2cControllerManager.ControllersCollection.Remove(controller);

                                // flag this to native dispose
                                disposeController = true;
                            }
                        }
                    }
                }

                NativeDispose(disposeController);

                _disposed = true;
            }
        }

        #pragma warning disable 1591
        ~I2cDevice()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                if (!_disposed)
                {
                    Dispose(true);

                    GC.SuppressFinalize(this);
                }
            }
        }

        #endregion

        internal static I2cDevice FindDevice(I2cController controller, int index)
        {
            for (int i = 0; i < controller.DeviceCollection.Count; i++)
            {
                if (((I2cDevice)controller.DeviceCollection[i])._deviceId == index)
                {
                    return (I2cDevice)controller.DeviceCollection[i];
                }
            }

            return null;
        }

        #region external calls to native implementations

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeInit();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeDispose(bool disposeController);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern I2cTransferResult NativeTransmit(byte[] writeBuffer, byte[] readBuffer);

        #endregion
    }

    /// <summary>
    /// Exception thrown when a check in driver's constructor finds a device that already exists with the same settings (I2C bus AND slave address)
    /// </summary>
    [Serializable]
    public class I2cDeviceAlreadyInUseException : Exception
    {
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() { return base.Message; }
    }
}
