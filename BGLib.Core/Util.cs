using System.Collections.Generic;

namespace BGLib.Core
{
    internal static class Util
    {
        private static readonly IDictionary<ushort, string> s_errors = new Dictionary<ushort, string>()
        {
            [0x0000] = "The operation was successful.",
            // BGAPI Errors
            [0x0180] = "Command contained invalid parameter.",
            [0x0181] = "Device is in wrong state to receive command.",
            [0x0182] = "Device has run out of memory.",
            [0x0183] = "Feature is not implemented.",
            [0x0184] = "Command was not recognized.",
            [0x0185] = "Command or Procedure failed due to timeout.",
            [0x0186] = "Connection handle passed is to command is not a valid handle.",
            [0x0187] = "Command would cause either underflow or overflow error.",
            [0x0188] = "User attribute was accessed through API which is not supported.",
            [0x0189] = "No valid license key found.",
            [0x018A] = "Command maximum length exceeded.",
            [0x018B] = "Bonding procedure can't be started because device has no space left for bond.",
            [0x018C] = "Module was reset due to script stack overflow.",
            // Bluetooth Errors
            [0x0205] = "Pairing or authentication failed due to incorrect results in the pairing or authentication procedure. This could be due to an incorrect PIN or Link Key.",
            [0x0206] = "Pairing failed because of missing PIN, or authentication failed because of missing Key.",
            [0x0207] = "Controller is out of memory.",
            [0x0208] = "Link supervision timeout has expired.",
            [0x0209] = "Controller is at limit of connections it can support.",
            [0x020C] = "Command requested cannot be executed because the Controller is in a state where it cannot process this command at this time.",
            [0x0212] = "Command contained invalid parameters.",
            [0x0213] = "User on the remote device terminated the connection.",
            [0x0216] = "Local device terminated the connection.",
            [0x0222] = "Connection terminated due to link-layer procedure timeout.",
            [0x0228] = "Received link-layer control packet where instant was in the past.",
            [0x023A] = "Operation was rejected because the controller is busy and unable to process the request.",
            [0x023B] = "The Unacceptable Connection Interval error code indicates that the remote device terminated the connection because of an unacceptable connection interval.",
            [0x023C] = "Directed advertising completed without a connection being created.",
            [0x023D] = "Connection was terminated because the Message Integrity Check (MIC) failed on a received packet.",
            [0x023E] = "LL initiated a connection but the connection has failed to be established. Controller did not receive any packets from remote end.",
            // Security Manager Protocol Errors
            [0x0301] = "The user input of passkey failed, for example, the user cancelled the operation.",
            [0x0302] = "Out of Band data is not available for authentication.",
            [0x0303] = "The pairing procedure cannot be performed as authentication requirements cannot be met due to IO capabilities of one or both devices.",
            [0x0304] = "The confirm value does not match the calculated compare value.",
            [0x0305] = "Pairing is not supported by the device.",
            [0x0306] = "The resultant encryption key size is insufficient for the security requirements of this device.",
            [0x0307] = "The SMP command received is not supported on this device.",
            [0x0308] = "Pairing failed due to an unspecified reason.",
            [0x0309] = "Pairing or authentication procedure is disallowed because too little time has elapsed since last pairing request or security request.",
            [0x030A] = "The Invalid Parameters error code indicates: the command length is invalid or a parameter is outside of the specified range.",
            // Attribute Protocol Errors
            [0x0401] = "The attribute handle given was not valid on this server.",
            [0x0402] = "The attribute cannot be read.",
            [0x0403] = "The attribute cannot be written.",
            [0x0404] = "The attribute PDU was invalid.",
            [0x0405] = "The attribute requires authentication before it can be read or written.",
            [0x0406] = "Attribute Server does not support the request received from the client.",
            [0x0407] = "Offset specified was past the end of the attribute.",
            [0x0408] = "The attribute requires authorization before it can be read or written.",
            [0x0409] = "Too many prepare writes have been queueud.",
            [0x040A] = "No attribute found within the given attribute handle range.",
            [0x040B] = "The attribute cannot be read or written using the Read Blob Request.",
            [0x040C] = "The Encryption Key Size used for encrypting this link is insufficient.",
            [0x040D] = "The attribute value length is invalid for the operation.",
            [0x040E] = "The attribute request that was requested has encountered an error that was unlikely, and therefore could not be completed as requested.",
            [0x040F] = "The attribute requires encryption before it can be read or written.",
            [0x0410] = "The attribute type is not a supported grouping attribute as defined by a higher layer specification.",
            [0x0411] = "Insufficient Resources to complete the request.",
            [0x0480] = "Application error code defined by a higher layer specification.",
        };

        public static string GetMessage(ushort errorCode)
        {
            return s_errors.TryGetValue(errorCode, out var message)
                ? message
                : $"Unknown error with code: {errorCode}.";
        }
    }
}
