using System;
using System.Threading.Tasks;

namespace BGLib.Core.SM
{
    /// <summary>
    /// The Security Manager (SM) class provides access to the Bluetooth low energy Security Manager and methods
    /// such as : bonding management and modes and encryption control.
    /// </summary>
    public class Messenger : BaseMessenger
    {
        internal Messenger(MessageHub messageHub)
            : base(messageHub)
        {
        }

        protected override byte Category => 0x05;

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x01:
                    {
                        var handle = eventValue[0];
                        var errorCode = BitConverter.ToUInt16(eventValue, 1);
                        var eventArgs = new BondingFailEventArgs(handle, errorCode);
                        BondingFail?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x02:
                    {
                        var handle = eventValue[0];
                        var passkey = BitConverter.ToUInt32(eventValue, 1);
                        var eventArgs = new PasskeyDisplayEventArgs(handle, passkey);
                        PasskeyDisplay?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x03:
                    {
                        var handle = eventValue[0];
                        var eventArgs = new PasskeyRequestEventArgs(handle);
                        PasskeyRequest?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x04:
                    {
                        var bond = eventValue[0];
                        var keySize = eventValue[1];
                        var mitm = eventValue[2];
                        var keys = (BondingKey)eventValue[3];
                        var eventArgs = new BondStatusEventArgs(bond, keySize, mitm, keys);
                        BondStatus?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #region Commands

        /// <summary>
        /// <para>This command starts the encryption for a given connection.</para>
        /// <para>
        /// Since iOS 9.1 update pairing without bonding is not any more supported by iOS. Calling this APIcommand without being in bondable mode, will cause the connection to fail with devices running iOS
        /// 9.1 or newer.
        /// </para>
        /// <para>
        /// Before using this API command with iOS9.1 or newer you must enable bonding mode with command
        /// Set Bondable Mode and you must also set then bonding parameter in this API command to 1 (Create
        /// bonding).
        /// </para>
        /// </summary>
        /// <param name="handle">Connection handle</param>
        /// <param name="bonding">
        /// <para>Create bonding if devices are not already bonded</para>
        /// <para>0: Do not create bonding</para>
        /// <para>1: Creating bonding</para>
        /// </param>
        /// <returns></returns>
        public async Task EncryptStartAsync(byte handle, byte bonding)
        {
            var commandValue = new[] { handle, bonding };
            var responseValue = await WriteAsync(0x00, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// Set device to bondable mode
        /// </summary>
        /// <param name="bondable">
        /// <para>Enables or disables bonding mode</para>
        /// <para>0 : the device is not bondable</para>
        /// <para>1 : the device is bondable</para>
        /// </param>
        /// <returns></returns>
        public async Task SetBondableModeAsync(byte bondable)
        {
            var commandValue = new[] { bondable };
            await WriteAsync(0x01, commandValue);
        }

        /// <summary>
        /// This command deletes a bonding from the local security database. There can be a maximum of 8 bonded
        /// devices stored at the same time, and one of them must be deleted if you need bonding with a 9th device.
        /// </summary>
        /// <param name="handle">
        /// <para>Bonding handle of a device.</para>
        /// <para>This handle can be obtained for example from events like:</para>
        /// <para>Scan Response</para>
        /// <para>Status</para>
        /// <para>If handle is 0xFF, all bondings will be deleted</para>
        /// </param>
        /// <returns></returns>
        public async Task DeleteBondingAsync(byte handle)
        {
            var commandValue = new[] { handle };
            var responseValue = await WriteAsync(0x02, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command is used to configure the local Security Manager and its features.
        /// </summary>
        /// <param name="mitm">
        /// <para>0: No Man-in-the-middle protection</para>
        /// <para>1: Man-in-the-middle protection required</para>
        /// <para>Default: 0</para>
        /// </param>
        /// <param name="minKeySize">
        /// <para>Minimum key size in Bytes</para>
        /// <para>Range: 7-16</para>
        /// <para>Default: 7 (56bits)</para>
        /// </param>
        /// <param name="ioCapabilities">
        /// <para>Configures the local devices I/O capabilities.</para>
        /// <para>See: SMP IO Capabilities for options.</para>
        /// <para>Default: No Input and No Output</para>
        /// </param>
        /// <returns></returns>
        public async Task SetParametersAsync(byte mitm, byte minKeySize, IOCapability ioCapabilities)
        {
            var ioCapabilitiesValue = (byte)ioCapabilities;
            var commandValue = new[] { mitm, minKeySize, ioCapabilitiesValue };
            await WriteAsync(0x03, commandValue);
        }

        /// <summary>
        /// This command is used to enter a passkey required for Man-in-the-Middle pairing. It should be sent as a
        /// response to Passkey Request event.
        /// </summary>
        /// <param name="handle">Connection Handle</param>
        /// <param name="passkey">
        /// <para>Passkey</para>
        /// <para>Range: 000000-999999</para>
        /// </param>
        /// <returns></returns>
        public async Task PasskeyEntryAsync(byte handle, uint passkey)
        {
            var passkeyValue = BitConverter.GetBytes(passkey);
            var commandValue = new byte[5];
            commandValue[0] = handle;
            Array.Copy(passkeyValue, 0, commandValue, 1, 4);
            var responseValue = await WriteAsync(0x04, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command lists all bonded devices. There can be a maximum of 8 bonded devices. The information related
        /// to the bonded devices is stored in the Flash memory, so it is persistent across resets and power-cycles.
        /// </summary>
        /// <returns>Num of currently bonded devices</returns>
        public async Task<byte> GetBondsAsync()
        {
            var responseValue = await WriteAsync(0x05);
            var bonds = responseValue[0];
            return bonds;
        }

        /// <summary>
        /// <para>This commands sets the Out-of-Band encryption data for a device.</para>
        /// <para>Device does not allow any other kind of pairing except OoB if the OoB data is set.</para>
        /// </summary>
        /// <param name="oob">
        /// <para>The OoB data to set, which must be 16 or 0 octets long.</para>
        /// <para>If the data is empty it clears the previous OoB data.</para>
        /// </param>
        /// <returns></returns>
        public async Task SetOobDataAsync(byte[] oob)
        {
            var commandValue = new byte[1 + oob.Length];
            commandValue[0] = oob.GetByteLength();
            Array.Copy(oob, 0, commandValue, 1, oob.Length);
            await WriteAsync(0x06, commandValue);
        }

        /// <summary>
        /// <para>
        /// This command will add all bonded devices with a known public or static address to the local devices white list.
        /// Previous entries in the white list will be first cleared.
        /// </para>
        /// <para>
        /// This command can't be used while advertising, scanning or being connected.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public async Task<byte> WhitelistBondsAsync()
        {
            var responseValue = await WriteAsync(0x07);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var count = responseValue[2];
            return count;
        }

        /// <summary>
        /// Change keys distribution fields in pairing request and response. By default all keys are distributed.
        /// </summary>
        /// <param name="initiatorKeys">
        /// <para>Initiator Key Distribution</para>
        /// <para>bit0: EncKey (LTK)</para>
        /// <para>bit1: IdKey (IRK)</para>
        /// <para>bit2: Sign (CSRK)</para>
        /// <para>bits3-7: Reserved</para>
        /// <para>Default: 0x07</para>
        /// </param>
        /// <param name="responderKeys">
        /// <para>Responder Key Distribution</para>
        /// <para>bit0: EncKey (LTK)</para>
        /// <para>bit1: IdKey (IRK)</para>
        /// <para>bit2: Sign (CSRK)</para>
        /// <para>bits3-7: Reserved</para>
        /// <para>Default: 0x07</para>
        /// </param>
        /// <returns></returns>
        public async Task SetPairingDistributionKeysAsync(byte initiatorKeys, byte responderKeys)
        {
            var commandValue = new[] { initiatorKeys, responderKeys };
            var responseValue = await WriteAsync(0x08, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This event indicates the bonding has failed for a connection.
        /// </summary>
        public event EventHandler<BondingFailEventArgs> BondingFail;
        /// <summary>
        /// This event tells a passkey should be printed to the user for bonding. This passkey must be entered in the
        /// remote device for bonding to be successful.
        /// </summary>
        public event EventHandler<PasskeyDisplayEventArgs> PasskeyDisplay;
        /// <summary>
        /// <para>
        /// This event indicates the Security Manager requests the user to enter passkey. The passkey the user needs to
        /// enter is displayed by the remote device.
        /// </para>
        /// <para>Use Passkey Entry command to respond to request</para>
        /// </summary>
        public event EventHandler<PasskeyRequestEventArgs> PasskeyRequest;
        /// <summary>
        /// This event outputs bonding status information.
        /// </summary>
        public event EventHandler<BondStatusEventArgs> BondStatus;

        #endregion
    }
}
