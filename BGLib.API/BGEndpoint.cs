﻿namespace BGLib.API
{
    /// <summary>
    /// Data Endpoints used in data routing and interface configuration
    /// </summary>
    public enum BGEndpoint : byte
    {
        /// <summary>
        /// Command Parser
        /// </summary>
        API = 0,
        /// <summary>
        /// Radio Test
        /// </summary>
        Test = 1,
        /// <summary>
        /// BGScript (not used)
        /// </summary>
        Script = 2,
        /// <summary>
        /// USB Interface
        /// </summary>
        USB = 3,
        /// <summary>
        /// USART 0
        /// </summary>
        UART0 = 4,
        /// <summary>
        /// USART 1
        /// </summary>
        UART1 = 5,
    }
}