using System;

namespace BGLib.API
{
    #region Commands/Responses

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
        FeaturesGet = 0x06,
        GetStatus = 0x07,
        RawTX = 0x08,
        SlaveLatencyDisable = 0x09,
    }

    internal enum GenericAccessProfileCommand : byte
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
        I2CRead = 0x0A,
        I2CWrite = 0x0B,
        SetTXPower = 0x0C,
        TimerComparator = 0x0D,
        IOPortIrqEnable = 0x0E,
        IOPortIrqDirection = 0x0F,
        AnalogComparatorEnable = 0x10,
        AnalogCOmparatorRead = 0x11,
        AnalogComparatorConfigIRQ = 0x12,
        SetRXGain = 0x13,
        UsbEnable = 0x14,
        SleepEnable = 0x15,
        GetTimestamp = 0x16,
    }

    internal enum PersistentStoreCommand : byte
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

    internal enum SecurityManagerCommand : byte
    {
        DeleteBonding = 0x02,
        EncryptStart = 0x00,
        GetBonds = 0x05,
        PasskeyEntry = 0x04,
        SetBondableMode = 0x01,
        SetOobData = 0x06,
        SetPairingDistributionKeys = 0x08,
        SetParameters = 0x03,
        WhitelistBonds = 0x07,
    }

    internal enum SystemCommand : byte
    {
        Reset = 0x00,
        Hello = 0x01,
        AddressGet = 0x02,
        RegWrite = 0x03,
        RegRead = 0x04,
        GetCounters = 0x05,
        GetConnections = 0x06,
        ReadMemory = 0x07,
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

    internal enum TestingCommand : byte
    {
        PhyTX = 0x00,
        PhyRX = 0x01,
        PhyEnd = 0x02,
        PhyReset = 0x03,
        GetChannelMap = 0x04,
        Debug = 0x05,
        ChannelMode = 0x06,
    }

    internal enum DeviceFirmwareUpgradeCommand : byte
    {
        Reset = 0x00,
        FlashSetAddress = 0x01,
        FlashUpload = 0x02,
        FlashUploadFinish = 0x03,
    }

    #endregion

    #region Events

    internal enum AttributeClientEvent : byte
    {
        Indicated = 0x00,
        ProcedureCompleted = 0x01,
        GroupFound = 0x02,
        AttributeFound = 0x03,
        FindInformationFound = 0x04,
        AttributeValue = 0x05,
        ReadMultipleResponse = 0x06,
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
        RawRX = 0x03,
        Disconnected = 0x04,
    }

    internal enum GenericAccessProfileEvent : byte
    {
        ScanResponse = 0x00,
        ModeChanged = 0x01,
    }

    internal enum HardwareEvent : byte
    {
        IOPortStatus = 0x00,
        SoftTimer = 0x01,
        AdcResult = 0x02,
        AnalogComparatorStatus = 0x03,
        RadioError = 0x04,
    }

    internal enum PersistentStoreEvent : byte
    {
        PSKey = 0x00,
    }

    internal enum SecurityManagerEvent : byte
    {
        SmpData = 0x00,
        BondingFail = 0x01,
        PassKeyDisplay = 0x02,
        PasskeyRequest = 0x03,
        BondStatus = 0x04,
    }

    internal enum SystemEvent : byte
    {
        Boot = 0x00,
        Debug = 0x01,
        EndpointWatermarkRX = 0x02,
        EndpointWatermarkTX = 0x03,
        ScriptFailure = 0x04,
        NoLicenseKey = 0x05,
        ProtocolError = 0x06,
        UsbEnumerated = 0x07,
    }

    internal enum DeviceFirmwareUpgradeEvent : byte
    {
        Boot = 0x00,
    }

    #endregion

    #region Attribute Client

    internal enum AttributeValueType
    {
        Read,
        Notify,
        Indicate,
        ReadByType,
        ReadBlob,
        IndicateRspReq,
    }

    #endregion

    #region Attribute Database

    internal enum AttributeChangeReason
    {
        WriteRequest,
        WriteCommand,
        WriteRequestUser,
    }

    internal enum AttributeStatusFlag
    {
        Notify,
        Indicate,
    }
    #endregion

    #region Connection

    internal enum ConnectionStatusFlag
    {
        Connected,
        Encrypted,
        Completed,
        ParametersChange,
    }

    #endregion

    #region Generic Access Profile

    internal enum AdFlag
    {
        LimitedDiscoverable = 0x01,
        GeneralDiscoverable = 0x02,
        BredrNotSupported = 0x04,
        SimultaneousLEBredrCtrl = 0x10,
        SimultaneousLEBredrHost = 0x20,
        Mask = 0x1F,
    }

    internal enum AdvertisingPolicy
    {
        All,
        WhitelistScan,
        WhitelistConnect,
        WhitelistAll,
    }

    internal enum GapConnectableMode
    {
        NonConnectable,
        DirectedConnectable,
        UndirectedConnectable,
        ScannableNonConnectable,
    }

    internal enum GapDiscoverableMode
    {
        NonDiscoverable,
        LimitedDiscoverable,
        GeneralDiscoverable,
        Broadcast,
        UserData,
        EnhancedBroadcasting = 0x80,
    }

    internal enum ScanHeaderFlag
    {
        AdvInd,
        AdvDirectInd,
        AdvNonconnInd,
        ScanReq,
        ScanRsp,
        ConnectReq,
        AdvDiscoverInd,
    }

    internal enum ScanPolicy
    {
        All,
        Whitelist,
    }

    #endregion

    #region Hardware

    #endregion

    #region Persistent Store

    #endregion

    #region Security Manager

    [Flags]
    internal enum BondingKey
    {
        LTK = 0x01,
        PublicAddress = 0x02,
        StaticAddress = 0x04,
        IRK = 0x08,
        EdivRand = 0x10,
        CSRK = 0x20,
        MasterId = 0x40,
    }

    internal enum SmpIOCapability
    {
        DisplayOnly,
        DisplayYesNo,
        KeyboardOnly,
        NoInputNoOutput,
        KeyboardDisplay,
    }

    #endregion

    #region System

    internal enum Endpoint
    {
        API,
        Test,
        Script,
        USB,
        UART0,
        UART1,
    }

    #endregion

    #region Testing

    #endregion

    #region DeviceFirmwareUpgrade

    #endregion

    #region Error Codes

    internal enum ErrorCode : ushort
    {
        // BGAPI
        InvalidParameter = 0x0180,
        DeviceInWrongState = 0x0181,
        OutOfMemory = 0x0182,
        FeatureNotImplemented = 0x0183,
        CommandNotRecognized = 0x0184,
        Timeout = 0x0185,
        NotConnected = 0x0186,
        Flow = 0x0187,
        UserAttribute = 0x0188,
        InvalidLicenseKey = 0x0189,
        CommandTooLong = 0x018A,
        OutOfBonds = 0x018B,
        ScriptOverflow = 0x018C,
        // Bluetooth errors
        AuthenticationFailure = 0x0205,
        PinOrKeyMissing = 0x0206,
        MemoryCapacityExceeded = 0x0207,
        ConnectionTimeout = 0x0208,
        ConnectionLimitExceeded = 0x0209,
        CommandDisallowed = 0x020C,
        InvalidCommandParameters = 0x0212,
        RemoteUserTerminatedConnection = 0x0213,
        ConnectionTerminatedByLocalHost = 0x0216,
        LLResponseTimeout = 0x0222,
        LLInstantPassed = 0x0228,
        ControllerBusy = 0x023A,
        UnacceptableConnectionInterval = 0x023B,
        DirectedAdvertisingTimeout = 0x023C,
        MicFailure = 0x023D,
        ConnectionFailedToBeEstablished = 0x023E,
        // Security manager protocol errors
        PasskeyEntryFailed = 0x0301,
        OobDataIsNotAvailable = 0x0302,
        AuthenticationRequirements = 0x0303,
        ConfirmValueFailed = 0x0304,
        PairingNotSupported = 0x0305,
        EncryptionKeySize = 0x0306,
        CommandNotSupported = 0x0307,
        UnspecifiedReason = 0x0308,
        RepeatedAttempts = 0x0309,
        InvalidParamters = 0x030A,
        // Attribute protocol errors
        InvalidHandle = 0x0401,
        ReadNotPermitted = 0x0402,
        WriteNotPermitted = 0x0403,
        InvalidPDU = 0x0404,
        InsufficientAuthentication = 0x0405,
        RequestNotSupported = 0x0406,
        InvalidOffset = 0x0407,
        InsufficientAuthorization = 0x0408,
        PrepareQueueFull = 0x0409,
        AttributeNotFound = 0x040A,
        AttributeNotLong = 0x040B,
        InsufficientEncryptionKeySize = 0x040C,
        InvalidAttributeValueLength = 0x040D,
        UnlikelyError = 0x040E,
        InsufficientEncryption = 0x040F,
        UnsupportedGroupType = 0x0410,
        InsufficientResources = 0x0411,
        ApplicationErrorCodes = 0x0480,
    }

    #endregion
}
