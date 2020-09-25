//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System.Collections;

namespace Windows.Devices.I2c
{
    internal sealed class I2cControllerManager
    {
        private static object _syncLock;

        // backing field for ControllersCollection
        // to store the controllers that are open
        private static ArrayList s_controllersCollection;

        /// <summary>
        /// <see cref="I2cController"/> collection.
        /// </summary>
        /// <remarks>
        /// This collection is for internal use only.
        /// </remarks>
        internal static ArrayList ControllersCollection
        {
            get
            {
                if (s_controllersCollection == null)
                {
                    if(_syncLock == null)
                    {
                        _syncLock = new object();
                    }

                    lock (_syncLock)
                    {
                        if (s_controllersCollection == null)
                        {
                            s_controllersCollection = new ArrayList();
                        }
                    }
                }

                return s_controllersCollection;
            }

            set
            {
                s_controllersCollection = value;
            }
        }
    }
}
