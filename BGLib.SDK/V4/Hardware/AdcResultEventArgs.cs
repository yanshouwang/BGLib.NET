using System;

namespace BGLib.SDK.V4.Hardware
{
    public class AdcResultEventArgs : EventArgs
    {
        public AdcResultEventArgs(byte input, ushort value)
        {
            Input = input;
            Value = value;
        }

        /// <summary>
        /// <para>A/D input from which value is received from</para>
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
        /// <para>0xA: AIN4--AIN5 differentia</para>
        /// <para>0xB: AIN6--AIN7 differential</para>
        /// <para>0xC: GND</para>
        /// <para>0xD: Reserved</para>
        /// <para>0xE: Temperature sensor</para>
        /// <para>0xF: VDD/3</para>
        /// </summary>
        public byte Input { get; }
        /// <summary>
        /// <para>A/D value.</para>
        /// <para>
        /// In the example case of 12 effective bits decimation, you will need to read the left-
        /// most 12 bits of the value to interpret it. It is a 12-bit 2's complement value left-
        /// aligned to the MSB of the 16-bit container, which means that negative values (which
        /// are uncommon but not impossible) are 0x8000 or higher, and positive values are
        /// 0x7FF0 or lower.Since it is only 12 bits, the last nibble will always be 0 (0xnnn0).
        /// You can divide the value by 16 (that is, bit-shift 4 bits to the right) to obtain the
        /// expected 12-bit value.
        /// </para>
        /// </summary>
        public ushort Value { get; }
    }
}