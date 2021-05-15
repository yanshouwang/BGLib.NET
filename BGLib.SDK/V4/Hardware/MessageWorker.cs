using System;
using System.Threading.Tasks;

namespace BGLib.SDK.V4.Hardware
{
    /// <summary>
    /// The Hardware class provides methods to access the local devices hardware interfaces such as : A/D
    /// converters, IO and timers, I2C interface etc.
    /// </summary>
    public class MessageWorker : SDK.MessageWorker
    {
        internal MessageWorker(MessageHub messageHub)
            : base(0x07, messageHub)
        {
        }

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var timestamp = BitConverter.ToUInt32(eventValue, 0);
                        var port = eventValue[4];
                        var irq = eventValue[5];
                        var state = eventValue[6];
                        var eventArgs = new IOPortStatusEventArgs(timestamp, port, irq, state);
                        IOPortStatus?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x01:
                    {
                        var handle = eventValue[0];
                        var eventArgs = new SoftTimerEventArgs(handle);
                        SoftTimer?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x02:
                    {
                        var input = eventValue[0];
                        var value = BitConverter.ToUInt16(eventValue, 1);
                        var eventArgs = new AdcResultEventArgs(input, value);
                        AdcResult?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x03:
                    {
                        var timestamp = BitConverter.ToUInt32(eventValue, 0);
                        var output = eventValue[4];
                        var eventArgs = new AnalogComparatorStatusEventArgs(timestamp, output);
                        AnalogComparatorStatus?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x04:
                    {
                        RadioError?.Invoke(this, EventArgs.Empty);
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
        /// <para>This command configures the locals I/O-port interrupts.</para>
        /// <para>
        /// Interrupts on I/O-port 2 can be enabled only for BLE113 and BLE121LR chip. In this case P2_0 and
        /// P2_1 pins are available.
        /// </para>
        /// </summary>
        /// <param name="port">
        /// <para>I/O port selection</para>
        /// <para>Values: 0 - 2</para>
        /// </param>
        /// <param name="enableBits">
        /// <para>A bit mask which tells which I/O generate an interrupt</para>
        /// <para>bit 0: Interrupt is enabled</para>
        /// <para>bit 1: Interrupt is disabled</para>
        /// </param>
        /// <param name="fallingEdge">
        /// <para>Interrupt sense for port.</para>
        /// <para>0 : rising edge</para>
        /// <para>1 : falling edge</para>
        /// <para>Note: affects all IRQ enabled pins on the port</para>
        /// </param>
        /// <returns></returns>
        [Obsolete("This command is deprecated in and Io Port Irq Enable and Io Port Irq Direction commands should beused instead.")]
        public async Task IOPortConfigIrqAsync(byte port, byte enableBits, byte fallingEdge)
        {
            var commandValue = new[] { port, enableBits, fallingEdge };
            var responseValue = await WriteAsync(0x00, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command configures the local software timer. The timer is 22 bits so the maximum value with BLE112 is
        /// 2^22 = 4194304/32768Hz = 128 seconds.With BLED112 USB dongle the maximum value is 2^22 = 4194304
        /// /32000Hz = 131 seconds.
        /// </summary>
        /// <param name="time">
        /// <para>Timer interrupt period in units of local crystal frequency.</para>
        /// <para>time : 1/32768 seconds for modules where the external sleep oscillator must be enabled.</para>
        /// <para>time : 1/32000 seconds for the dongle where internal RC oscillator is used. If time is 0, scheduled timer is removed.</para>
        /// </param>
        /// <param name="handle">Handle that is sent back within triggered event at timeout</param>
        /// <param name="singleShot">
        /// <para>Timer mode.</para>
        /// <para>
        /// 0: Repeating timeout: the timer event is triggered at intervals defined with time
        /// . The stack only supports one repeating timer at a time for reliability purposes.
        /// Starting a repeating soft timer removes the current one if any.
        /// </para>
        /// <para>
        /// 1: Single timeout: the timer event is triggered only once after a period defined
        /// with time.There can be up to 8 non-repeating software timers running at the
        /// same time (max number actually depends on the current activities of the stack,
        /// so it might be lower than 8 at times.)
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task SetSoftTimerAsync(uint time, byte handle, byte singleShot)
        {
            var timeValue = BitConverter.GetBytes(time);
            var commandValue = new byte[6];
            Array.Copy(timeValue, commandValue, 4);
            commandValue[4] = handle;
            commandValue[5] = singleShot;
            var responseValue = await WriteAsync(0x01, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command reads the devices local A/D converter. Only a single channel may be read at a time, and each
        /// conversion must complete before another one is requested.The completion of each conversion is indicated by
        /// the hardware_adc_result event.
        /// </summary>
        /// <param name="input">
        /// <para>Selects the ADC input.</para>
        /// <para>0x0: AIN0 (pin 0 of port P0, denoted as A0 in the ADC row of datasheet's table 3)</para>
        /// <para>0x1: AIN1</para>
        /// <para>0x2: AIN2</para>
        /// <para>0x3: AIN3</para>
        /// <para>0x4: AIN4</para>
        /// <para>0x5: AIN5</para>
        /// <para>0x6: AIN6</para>
        /// <para>0x7: AIN7</para>
        /// <para>0x8: AIN0--AIN1 differential</para>
        /// <para>0x9: AIN2--AIN3 differential</para>
        /// <para>0xA: AIN4--AIN5 differential</para>
        /// <para>0xB: AIN6--AIN7 differential</para>
        /// <para>0xC: GND</para>
        /// <para>0xD: Reserved</para>
        /// <para>0xE: Temperature sensor</para>
        /// <para>0xF: VDD/3</para>
        /// </param>
        /// <param name="decimation">
        /// <para>Select resolution and conversion rate for conversion, result is always stored in MSB bits.</para>
        /// <para>0: 7 effective bits</para>
        /// <para>1: 9 effective bits</para>
        /// <para>2: 10 effective bits</para>
        /// <para>3: 12 effective bits</para>
        /// </param>
        /// <param name="referenceSelection">
        /// <para>Selects the reference for the ADC. Reference corresponds to the maximum allowed input value.</para>
        /// <para>0: Internal reference (1.24V)</para>
        /// <para>1: External reference on AIN7 pin</para>
        /// <para>2: AVDD pin</para>
        /// <para>3: External reference on AIN6--AIN7 differential input</para>
        /// </param>
        /// <returns></returns>
        public async Task AdcReadAsync(byte input, byte decimation, byte referenceSelection)
        {
            var commandValue = new[] { input, decimation, referenceSelection };
            var responseValue = await WriteAsync(0x02, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// The command configiures I/O-port directions
        /// </summary>
        /// <param name="port">I/0 PORT index: 0, 1 or 2</param>
        /// <param name="direction">
        /// <para>Bitmask for each individual pin direction</para>
        /// <para>bit0 means input (default)</para>
        /// <para>bit1 means output</para>
        /// <para>Example:</para>
        /// <para>To configure all port's pins as output use 0xff</para>
        /// </param>
        /// <returns></returns>
        public async Task IOPortConfigDirectionAsync(byte port, byte direction)
        {
            var commandValue = new[] { port, direction };
            var responseValue = await WriteAsync(0x03, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command configures the I/O-ports function.</para>
        /// <para>
        /// If bit is set in function parameter then the corresponding I/O port is set to peripheral function, otherwise it is
        /// general purpose I/O pin.
        /// </para>
        /// </summary>
        /// <param name="port">I/O port: 0,1 or 2</param>
        /// <param name="function">peripheral selection bit for pins</param>
        /// <returns></returns>
        public async Task IOPortConfigFunctionAsync(byte port, byte function)
        {
            var commandValue = new[] { port, function };
            var responseValue = await WriteAsync(0x04, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>Configure I/O-port pull-up/pull-down</para>
        /// <para>Pins P1_0 and P1_1 do not have pull-up/pull-down.</para>
        /// </summary>
        /// <param name="port">I/O port select: 0, 1 or 2</param>
        /// <param name="tristateMask">If this bit is set, disabled pull on pin</param>
        /// <param name="pullUp">
        /// <para>0: pull all port's pins down</para>
        /// <para>1: pull all port's pins up</para>
        /// </param>
        /// <returns></returns>
        public async Task IOPortConfigPullAsync(byte port, byte tristateMask, byte pullUp)
        {
            var commandValue = new[] { port, tristateMask, pullUp };
            var responseValue = await WriteAsync(0x05, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// Write I/O-port statuses
        /// </summary>
        /// <param name="port">
        /// <para>I/O port to write to</para>
        /// <para>Values: 0,1 or 2</para>
        /// </param>
        /// <param name="mask">
        /// <para>Bit mask to tell which I/O pins to write</para>
        /// <para>Example:</para>
        /// <para>To write the status of all IO pins use 0xFF</para>
        /// </param>
        /// <param name="data">
        /// <para>Bit mask to tell which state to write</para>
        /// <para>bit0: I/O is disabled</para>
        /// <para>bit1: I/O is enabled</para>
        /// <para>Example:</para>
        /// <para>To enable all IO pins use 0xFF</para>
        /// </param>
        /// <returns></returns>
        public async Task IOPortWriteAsync(byte port, byte mask, byte data)
        {
            var commandValue = new[] { port, mask, data };
            var responseValue = await WriteAsync(0x06, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// Read I/O-port
        /// </summary>
        /// <param name="port">
        /// <para>I/O port to read</para>
        /// <para>Values: 0,1 or 2</para>
        /// </param>
        /// <param name="mask">
        /// <para>Bit mask to tell which I/O pins to read</para>
        /// <para>Example:</para>
        /// <para>To read the status of all IO pins use 0xFF</para>
        /// </param>
        /// <returns>I/O port pin state</returns>
        public async Task<byte> IOPortReadAsync(byte port, byte mask)
        {
            var commandValue = new[] { port, mask };
            var responseValue = await WriteAsync(0x07, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var data = responseValue[3];
            return data;
        }

        /// <summary>
        /// The command configures the SPI interface
        /// </summary>
        /// <param name="channel">
        /// <para>USART channel</para>
        /// <para>Values: 0 or 1</para>
        /// </param>
        /// <param name="polarity">
        /// <para>Clock polarity</para>
        /// <para>Values: 0 or 1</para>
        /// </param>
        /// <param name="phase">
        /// <para>Clock phase</para>
        /// <para>Values: 0 or 1</para>
        /// </param>
        /// <param name="bitOrder">
        /// <para>Endianness</para>
        /// <para>0: LSB</para>
        /// <para>1: MSB</para>
        /// </param>
        /// <param name="baudE">baud rate exponent value</param>
        /// <param name="baudM">baud rate mantissa value</param>
        /// <returns></returns>
        public async Task SpiConfigAsync(byte channel, byte polarity, byte phase, byte bitOrder, byte baudE, byte baudM)
        {
            var commandValue = new[] { channel, polarity, phase, bitOrder, baudE, baudM };
            var responseValue = await WriteAsync(0x08, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command is used to transfer SPI data when in master mode. Maximum of 64 bytes can be transferred at a
        /// time.
        /// </para>
        /// <para>
        /// Slave select pin is not controlled automatically when transferring data while in SPI master mode, so it
        /// must be controlled by the application using normal GPIO control commands like IO Port Write
        /// command.
        /// </para>
        /// </summary>
        /// <param name="channel">
        /// <para>SPI channel</para>
        /// <para>Value: 0 or 1</para>
        /// </param>
        /// <param name="data">
        /// <para>Data to transmit</para>
        /// <para>Maximum length is 64 bytes</para>
        /// </param>
        /// <returns>data received from SPI</returns>
        public async Task<byte[]> SpiTransferAsync(byte channel, byte[] data)
        {
            var commandValue = new byte[2 + data.Length];
            commandValue[0] = channel;
            commandValue[1] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 2, data.Length);
            var responseValue = await WriteAsync(0x09, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var data1Length = responseValue[3];
            var data1 = new byte[data1Length];
            Array.Copy(responseValue, 4, data1, 0, data1.Length);
            return data1;
        }

        /// <summary>
        /// <para>The command reads data from I2C bus.</para>
        /// <para>
        /// BLE112 module: uses bit-bang method and only master-mode is supported in current firmwares, I2C CLK is
        /// fixed to P1_7 and I2C DATA to P1_6(pull - up must be enabled on both pins), the clock rate is approximately 20 -
        /// 25 kHz and it does vary slightly because other functionality has higher interrupt priority, such as the BLE radio.
        /// </para>
        /// <para>
        /// BLE113/BLE121LR modules: only master-mode is supported in current firmwares, I2C pins are 14/24 (I2C CLK)
        /// and 15/25 (I2C DATA) as seen in the datasheet, operates at 267kHz.
        /// </para>
        /// <para>
        /// To convert a 7-bit I2C address to an 8-bit one, shift left by one bit. For example, a 7-bit address of
        /// 0x40 (dec 64) would be used as 0x80 (dec 128).
        /// </para>
        /// <para>
        /// I2C commands got a timeout of about 250 ms. If the read operation is timeouted then the
        /// corresponding command result is returned.
        /// </para>
        /// </summary>
        /// <param name="address">
        /// I2C's 8-bit slave address according to the note above. Keep read/write bit (LSB) set
        /// to zero, as the firmware will set it automatically.
        /// </param>
        /// <param name="stop">If nonzero Send I2C stop condition after transmission</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Data read</returns>
        public async Task<byte[]> I2cReadAsync(byte address, byte stop, byte length)
        {
            var commandValue = new[] { address, stop, length };
            var responseValue = await WriteAsync(0x0A, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var dataLength = responseValue[2];
            var data = new byte[dataLength];
            Array.Copy(responseValue, 3, data, 0, data.Length);
            return data;
        }

        /// <summary>
        /// <para>Write data to I2C bus.</para>
        /// <para>
        /// BLE112: uses bit-bang method, only master-mode is supported in current firmwares, I2C CLK is fixed to P1_7
        /// and I2C DATA to P1_6(pull-up must be enabled on both pins), the clock rate is approximately 20-25 kHz and it
        /// does vary slightly because other functionality has higher interrupt priority, such as the BLE radio.
        /// </para>
        /// <para>
        /// BLE113/BLE121LR: only master-mode is supported in current firmwares, I2C pins are 14/24 (I2C CLK) and 15
        /// /25 (I2C DATA) as seen in the datasheet, operates at 267kHz.
        /// </para>
        /// <para>
        /// To convert a 7-bit address to an 8-bit one, shift left by one bit. For example, a 7-bit address of 0x40
        /// (dec 64) would be used as 0x80 (dec 128).
        /// </para>
        /// <para>
        /// I2C commands got a timeout of about 250 ms. If the write operation is timeouted then the written bytes
        /// value is 0.
        /// </para>
        /// </summary>
        /// <param name="address">
        /// I2C's 8-bit slave address according to the note above. Keep read/write bit
        /// (LSB) set to zero, as the firmware will set it automatically.
        /// </param>
        /// <param name="stop">If nonzero Send I2C stop condition after transmission</param>
        /// <param name="data">Data to write</param>
        /// <returns>Bytes written</returns>
        public async Task<byte> I2cWriteAsync(byte address, byte stop, byte[] data)
        {
            var commandValue = new byte[3 + data.Length];
            commandValue[0] = address;
            commandValue[1] = stop;
            commandValue[2] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 3, data.Length);
            var responseValue = await WriteAsync(0x0B, commandValue);
            var written = responseValue[0];
            return written;
        }

        /// <summary>
        /// Re-configure TX output power.
        /// </summary>
        /// <param name="power">
        /// <para>TX output power level to use</para>
        /// <para>Range:</para>
        /// <para>0 to 15 with the BLE112 and the BLED112</para>
        /// <para>0 to 14 with the BLE113</para>
        /// <para>0 to 9 with the BLE121LR</para>
        /// <para>For more information, refer to the &lt;txpower&gt; tag in the hardware.xml configuration file.</para>
        /// </param>
        /// <returns></returns>
        public async Task SetTXPowerAsync(byte power)
        {
            var commandValue = new[] { power };
            await WriteAsync(0x0C, commandValue);
        }

        /// <summary>
        /// <para>Set comparator for timer channel.</para>
        /// <para>
        /// This command may be used to generate e.g. PWM signals with hardware timer. More information on different
        /// comparator modes and their usage may be found from Texas Instruments CC2540 User's Guide (SWRU191B),
        /// section 9.8 Output Compare Mode.
        /// </para>
        /// </summary>
        /// <param name="timer">Timer</param>
        /// <param name="channel">Timer channel</param>
        /// <param name="mode">Comparator mode</param>
        /// <param name="comparatorValue">Comparator value</param>
        /// <returns></returns>
        public async Task TimerComparatorAsync(byte timer, byte channel, byte mode, ushort comparatorValue)
        {
            var comparatorValueValue = BitConverter.GetBytes(comparatorValue);
            var commandValue = new byte[5];
            commandValue[0] = timer;
            commandValue[1] = channel;
            commandValue[2] = mode;
            Array.Copy(comparatorValueValue, 0, commandValue, 3, 2);
            var responseValue = await WriteAsync(0x0D, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// Enable I/O-port interrupts. When enabled, I/O-port interrupts are triggered on either rising or falling edge. The
        /// direction when the interrupt occurs may be configured with IO Port Irq Direction command.
        /// </para>
        /// <para>
        /// Interrupts on I/O-port 2 can be enabled only for BLE113 and BLE121LR chip. In this case P2_0 and
        /// P2_1 pins are available.
        /// </para>
        /// </summary>
        /// <param name="port">
        /// <para>I/O Port</para>
        /// <para>Value: 0 - 2</para>
        /// </param>
        /// <param name="enableBits">
        /// <para>Interrupt enable mask for pins</para>
        /// <para>bit0 means interrupt is disabled</para>
        /// <para>bit1 means interrupt is enabled</para>
        /// <para>Example:</para>
        /// <para>To enable interrupts an all pins use 0xFF</para>
        /// </param>
        /// <returns></returns>
        public async Task IOPortIrqEnableAsync(byte port, byte enableBits)
        {
            var commandValue = new[] { port, enableBits };
            var responseValue = await WriteAsync(0x0E, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>Set I/O-port interrupt direction. The direction applies for every pin in the given I/O-port.</para>
        /// <para>
        /// Interrupts on I/O-port 2 can be enabled only for BLE113 and BLE121LR chip. In this case P2_0 and
        /// P2_1 pins are available.
        /// </para>
        /// </summary>
        /// <param name="port">
        /// <para>I/O Port</para>
        /// <para>Values: 0 - 2</para>
        /// </param>
        /// <param name="fallingEdge">
        /// <para>Interrupt edge direction for port</para>
        /// <para>0: rising edge</para>
        /// <para>1: falling edge</para>
        /// </param>
        /// <returns></returns>
        public async Task IOPortIrqDirectionAsync(byte port, byte fallingEdge)
        {
            var commandValue = new[] { port, fallingEdge };
            var responseValue = await WriteAsync(0x0F, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// Enables or disables the analog comparator. Analog comparator has to be enabled prior using any other analog
        /// comparator commands.
        /// </summary>
        /// <param name="enable">
        /// <para>0: disable</para>
        /// <para>1: enable</para>
        /// </param>
        /// <returns></returns>
        public async Task AnalogComparatorEnableAsync(byte enable)
        {
            var commandValue = new[] { enable };
            await WriteAsync(0x10, commandValue);
        }

        /// <summary>
        /// The command reads analog comparator output. Before using this command, analog comparator has to be
        /// enabled with <see cref="ControlAnalogComparatorAsync(bool)"/> command.
        /// </summary>
        /// <returns>
        /// <para>Analog comparator output</para>
        /// <para>1: V+ > V-</para>
        /// <para>0: V+ &lt; V-</para>
        /// </returns>
        public async Task<byte> AnalogComparatorReadAsync()
        {
            var responseValue = await WriteAsync(0x11);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var output = responseValue[2];
            return output;
        }

        /// <summary>
        /// <para>
        /// This command configures analog comparator interrupts. Before enabling this interrupt, analog comparator has
        /// to be first enabled with Analog Comparator Enable command.
        /// </para>
        /// <para>
        /// Analog comparator interrupts are generated by default on rising edge, i.e. when condition V+ > V- becomes
        /// true. It is also possible to configure the opposite functionality, i.e.interrupts are generated on falling edge when
        /// V+ &lt; V- becomes true. The interrupt direction may be configured with Io Port Irq Direction command, by setting I
        /// /O-port 0 direction.Please note that this configuration affects both analog comparator interrupt direction and all I
        /// /O-port 0 pin interrupt directions.
        /// </para>
        /// <para>
        /// Analog comparator interrupts are automatically disabled once triggered , so that a high frequency signal doesn't
        /// cause unintended consequences.Continuous operation may be achieved by re-enabling the interrupt as soon
        /// as the Analog Comparator Status event has been received.
        /// </para>
        /// </summary>
        /// <param name="enabled">
        /// <para>0: disable interrupts</para>
        /// <para>1: enable interrupts</para>
        /// </param>
        /// <returns></returns>
        public async Task AnalogComparatorConfigIrqAsync(byte enabled)
        {
            var commandValue = new[] { enabled };
            var responseValue = await WriteAsync(0x12, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command sets the radio receiver (RX) sensitivity to either high (default) or standard. The exact sensitivity
        /// value is dependent on the used hardware(refer to the appropriate data sheet).
        /// </summary>
        /// <param name="gain">
        /// <para>0: standard gain</para>
        /// <para>1: high gain (default)</para>
        /// </param>
        /// <returns></returns>
        public async Task SetRXGainAsync(byte gain)
        {
            var commandValue = new[] { gain };
            await WriteAsync(0x13, commandValue);
        }

        /// <summary>
        /// This command activates (enable) or deactivates USB controller on the BLE112 Bluetooth Low Energy module.
        /// The USB controller is activated by default when USB is set on in the hardware configuration.On the other
        /// hand, the USB controller cannot be activated if the USB is not set on in the hardware configuration.
        /// </summary>
        /// <param name="enable">
        /// <para>0: disable USB</para>
        /// <para>1: enable USB</para>
        /// </param>
        /// <returns></returns>
        public async Task UsbEnableAsync(byte enable)
        {
            var commandValue = new[] { enable };
            var responseValue = await WriteAsync(0x14, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command enables or disables sleep mode.
        /// </summary>
        /// <param name="enable">
        /// <para>0: disables sleep mode</para>
        /// <para>1: enables sleep mode</para>
        /// </param>
        /// <returns></returns>
        public async Task SleepEnableAsync(byte enable)
        {
            var commandValue = new[] { enable };
            var responseValue = await WriteAsync(0x15, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command returns value of hardware Sleep Timer count.</para>
        /// <para>
        /// It can be used (e. g.) for the estimation of statement execution time, as a timestamp, or in code termination after
        /// a timeout.Value of timestamp isn't incremented when the module is in PM3 power mode.
        /// </para>
        /// </summary>
        /// <returns>Sleep Timer count value</returns>
        public async Task<uint> GetTimestampAsync()
        {
            var responseValue = await WriteAsync(0x16);
            var value = BitConverter.ToUInt32(responseValue, 0);
            return value;
        }

        #endregion

        #region Events

        /// <summary>
        /// <para>This event is produced when I/O port status changes.</para>
        /// <para>
        /// The timestamp is only valid if the module doesn't go to PM3 because in that mode the low frequency
        /// oscillator is turned off.Example of such situation is the module in master mode, but not connected to
        /// any slave. If module wakes up from an IO interrupt, then the timestamp in the event will not be
        /// accurate.
        /// </para>
        /// <para>
        /// Setting up the timer by the Set Soft Timer command prevents the module from going to PM3 and
        /// makes timestamps be valid all the time.
        /// </para>
        /// </summary>
        public event EventHandler<IOPortStatusEventArgs> IOPortStatus;
        /// <summary>
        /// This event is produced when software timer interrupt is generated.
        /// </summary>
        public event EventHandler<SoftTimerEventArgs> SoftTimer;
        /// <summary>
        /// This events is produced when an A/D converter result is received.
        /// </summary>
        public event EventHandler<AdcResultEventArgs> AdcResult;
        /// <summary>
        /// <para>This event is produced when analog comparator output changes in the configured direction.</para>
        /// <para>
        /// The timestamp is only valid if the module doesn't go to PM3 because in that mode the low frequency
        /// oscillator is turned off.Example of such situation is the module in master mode, but not connected to
        /// any slave. If module wakes up from an analog comparator interrupt, then the timestamp in the event
        /// will not be accurate.
        /// </para>
        /// <para>
        /// Setting up the timer by the Set Soft Timer command prevents the module from going to PM3 and
        /// makes timestamps be valid all the time.
        /// </para>
        /// </summary>
        public event EventHandler<AnalogComparatorStatusEventArgs> AnalogComparatorStatus;
        /// <summary>
        /// This event is produced when the radio hardware error appears. The radio hardware error is caused by an
        /// incorrect state of the radio receiver that reports wrong values of length of packets.The FIFO queue of thereceiver is then wrongly read and as a result the device stops responding. After receiving such event the device
        /// must be restarted in order to recover.
        /// </summary>
        public event EventHandler RadioError;

        #endregion
    }
}
