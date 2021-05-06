using System;

namespace BGLib.API
{
    #region Commands/Responses

    internal enum SystemCommand : byte
    {
        Reset = 0x00,
        Hello = 0x01,
        AddressGet = 0x02,
        //RegWrite = 0x03,
        //RegRead = 0x04,
        GetCounters = 0x05,
        GetConnections = 0x06,
        //ReadMemory = 0x07,
        GetInfo = 0x08,
        EndpointTX = 0x09,
        WhitelistAppend = 0x0A,
        WhitelistRemove = 0x0B,
        WhitelistClear = 0x0C,
        EndpointRX = 0x0D,
        EndpointSetWatermarks = 0x0E,
        AesSetKey = 0x0F,
        AesEncrypt = 0x10,
        AesDecrypt = 0x11,
        UsbEnumerationStatusGet = 0x12,
        GetBootloader = 0x13,
        DelayReset = 0x14,
    }

    internal enum PSCommand : byte
    {
        PSDefrag = 0x00,
        PSDump = 0x01,
        PSEraseAll = 0x02,
        PSSave = 0x03,
        PSLoad = 0x04,
        PSErase = 0x05,
        ErasePage = 0x06,
        WriteData = 0x07,
        ReadData = 0x08,
    }

    internal enum AttributeDatabaseCommand : byte
    {
        Write = 0x00,
        Read = 0x01,
        ReadType = 0x02,
        UserReadResponse = 0x03,
        UserWriteResponse = 0x04,
        Send = 0x05,
    }

    internal enum ConnectionCommand : byte
    {
        Disconnect = 0x00,
        GetRSSI = 0x01,
        Update = 0x02,
        VersionUpdate = 0x03,
        ChannelMapGet = 0x04,
        ChannelMapSet = 0x05,
        //FeaturesGet = 0x06,
        GetStatus = 0x07,
        //RawTX = 0x08,
        SlaveLatencyDisable = 0x09,
    }

    internal enum AttributeClientCommand : byte
    {
        FindByTypeValue = 0x00,
        ReadByGroupType = 0x01,
        ReadByType = 0x02,
        FindInformation = 0x03,
        ReadByHandle = 0x04,
        AttributeWrite = 0x05,
        WriteCommand = 0x06,
        IndicateConfirm = 0x07,
        ReadLong = 0x08,
        PrepareWirte = 0x09,
        ExecuteWrite = 0x0A,
        ReadMultiple = 0x0B,
    }

    internal enum SMCommand : byte
    {
        EncryptStart = 0x00,
        SetBondableMode = 0x01,
        DeleteBonding = 0x02,
        SetParameters = 0x03,
        PasskeyEntry = 0x04,
        GetBonds = 0x05,
        SetOobData = 0x06,
        WhitelistBonds = 0x07,
        SetPairingDistributionKeys = 0x08,
    }

    internal enum GapCommand : byte
    {
        SetPrivacyFlags = 0x00,
        SetMode = 0x01,
        Discover = 0x02,
        ConnectDirect = 0x03,
        EndProcedure = 0x04,
        ConnectSelective = 0x05,
        SetFiltering = 0x06,
        SetScanParameters = 0x07,
        SetAdvParameters = 0x08,
        SetAdvData = 0x09,
        SetDirectedConnectableMode = 0x0A,
        SetInitialingConParameters = 0x0B,
        SetNonresolvableAddress = 0x0C,
    }

    internal enum HardwareCommand : byte
    {
        IOPortConfigIRQ = 0x00,
        SetSoftTimer = 0x01,
        AdcRead = 0x02,
        IOPortConfigDirection = 0x03,
        IOPortConfigFunction = 0x04,
        IOPortConfigPull = 0x05,
        IOPortWrite = 0x06,
        IOPortRead = 0x07,
        SpiConfig = 0x08,
        SpiTransfer = 0x09,
        I2cRead = 0x0A,
        I2cWrite = 0x0B,
        SetTXPower = 0x0C,
        TimerComparator = 0x0D,
        IOPortIrqEnable = 0x0E,
        IOPortIrqDirection = 0x0F,
        AnalogComparatorEnable = 0x10,
        AnalogComparatorRead = 0x11,
        AnalogComparatorConfigIRQ = 0x12,
        SetRXGain = 0x13,
        UsbEnable = 0x14,
        SleepEnable = 0x15,
        GetTimestamp = 0x16,
    }

    internal enum TestingCommand : byte
    {
        PhyTX = 0x00,
        PhyRX = 0x01,
        PhyEnd = 0x02,
        //PhyReset = 0x03,
        GetChannelMap = 0x04,
        //Debug = 0x05,
        ChannelMode = 0x06,
    }

    internal enum DfuCommand : byte
    {
        Reset = 0x00,
        FlashSetAddress = 0x01,
        FlashUpload = 0x02,
        FlashUploadFinish = 0x03,
    }

    #endregion

    #region Events

    internal enum SystemEvent : byte
    {
        Boot = 0x00,
        //Debug = 0x01,
        EndpointWatermarkRX = 0x02,
        EndpointWatermarkTX = 0x03,
        ScriptFailure = 0x04,
        NoLicenseKey = 0x05,
        ProtocolError = 0x06,
        UsbEnumerated = 0x07,
    }

    internal enum PSEvent : byte
    {
        PSKey = 0x00,
    }

    internal enum AttributeDatabaseEvent : byte
    {
        Value = 0x00,
        UserReadRequest = 0x01,
        Status = 0x02,
    }

    internal enum ConnectionEvent : byte
    {
        Status = 0x00,
        VersionInd = 0x01,
        FeatureInd = 0x02,
        //RawRX = 0x03,
        Disconnected = 0x04,
    }

    internal enum AttributeClientEvent : byte
    {
        Indicated = 0x00,
        ProcedureCompleted = 0x01,
        GroupFound = 0x02,
        //AttributeFound = 0x03,
        FindInformationFound = 0x04,
        AttributeValue = 0x05,
        ReadMultipleResponse = 0x06,
    }

    internal enum SMEvent : byte
    {
        //SmpData = 0x00,
        BondingFail = 0x01,
        PassKeyDisplay = 0x02,
        PasskeyRequest = 0x03,
        BondStatus = 0x04,
    }

    internal enum GapEvent : byte
    {
        ScanResponse = 0x00,
        //ModeChanged = 0x01,
    }

    internal enum HardwareEvent : byte
    {
        IOPortStatus = 0x00,
        SoftTimer = 0x01,
        AdcResult = 0x02,
        AnalogComparatorStatus = 0x03,
        RadioError = 0x04,
    }

    internal enum DfuEvent : byte
    {
        Boot = 0x00,
    }

    #endregion
}

