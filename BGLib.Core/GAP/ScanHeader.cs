using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGLib.Core.GAP
{
    /// <summary>
    /// Scan header flags
    /// </summary>
    public enum ScanHeader : byte
    {
        /// <summary>
        /// Connectable undirected advertising event
        /// </summary>
        AdvInd = 0,
        /// <summary>
        /// Connectable directed advertising event
        /// </summary>
        AdvDirectInd = 1,
        /// <summary>
        /// Non-connectable undirected advertising event
        /// </summary>
        AdvNonconnInd = 2,
        /// <summary>
        /// Scanner wants information from Advertiser
        /// </summary>
        ScanReq = 3,
        /// <summary>
        /// Advertiser gives more information to Scanner
        /// </summary>
        ScanRsp = 4,
        /// <summary>
        /// Initiator wants to connect to Advertiser
        /// </summary>
        ConnectReq = 5,
        /// <summary>
        /// Non-connectable undirected advertising event
        /// </summary>
        AdvDiscoverInd = 6,
    }
}
