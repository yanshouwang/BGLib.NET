﻿using System;

namespace BGLib.SDK.V4.AttributeDatabase
{
    /// <summary>
    /// Attribute status flags
    /// </summary>
    [Flags]
    public enum AttributeStatus : byte
    {
        /// <summary>
        /// Notifications are enabled
        /// </summary>
        Notify = 1,
        /// <summary>
        ///  Indications are enabled
        /// </summary>
        Indicate = 2,
    }
}