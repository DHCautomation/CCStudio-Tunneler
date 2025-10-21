using Opc.Ua;
using CCStudio.Tunneler.Core.Models;

namespace CCStudio.Tunneler.Service.OPC;

/// <summary>
/// Maps OPC DA quality codes to OPC UA StatusCodes
/// OPC DA quality is a 16-bit value where:
/// - Bits 7-6: Quality (00=Bad, 01=Uncertain, 11=Good)
/// - Bits 5-0: Substatus
/// </summary>
public static class QualityMapper
{
    // OPC DA Quality masks
    private const ushort OPC_QUALITY_MASK = 0xC0;           // Bits 7-6
    private const ushort OPC_SUBSTATUS_MASK = 0x3F;         // Bits 5-0

    // OPC DA Quality values
    private const ushort OPC_QUALITY_BAD = 0x00;            // 00......
    private const ushort OPC_QUALITY_UNCERTAIN = 0x40;      // 01......
    private const ushort OPC_QUALITY_GOOD = 0xC0;           // 11......

    // OPC DA Substatus values (partial list - most common in BAS)
    private const ushort OPC_QUALITY_COMM_FAILURE = 0x18;   // Communication failure
    private const ushort OPC_QUALITY_LAST_KNOWN = 0x19;     // Last known value
    private const ushort OPC_QUALITY_NOT_CONNECTED = 0x08;  // Not connected
    private const ushort OPC_QUALITY_DEVICE_FAILURE = 0x0C; // Device failure
    private const ushort OPC_QUALITY_SENSOR_FAILURE = 0x10; // Sensor failure
    private const ushort OPC_QUALITY_CONFIG_ERROR = 0x04;   // Configuration error
    private const ushort OPC_QUALITY_NOT_ACTIVE = 0x14;     // Not active
    private const ushort OPC_QUALITY_OUT_OF_SERVICE = 0x1C; // Out of service

    /// <summary>
    /// Maps OPC DA quality code to OPC UA StatusCode
    /// </summary>
    /// <param name="daQuality">OPC DA quality value (0-255)</param>
    /// <returns>Corresponding OPC UA StatusCode</returns>
    public static StatusCode MapDaQualityToUa(ushort daQuality)
    {
        var quality = (ushort)(daQuality & OPC_QUALITY_MASK);
        var substatus = (ushort)(daQuality & OPC_SUBSTATUS_MASK);

        return quality switch
        {
            OPC_QUALITY_GOOD => MapGoodQuality(substatus),
            OPC_QUALITY_UNCERTAIN => MapUncertainQuality(substatus),
            OPC_QUALITY_BAD => MapBadQuality(substatus),
            _ => StatusCodes.Bad
        };
    }

    /// <summary>
    /// Maps DataQuality enum to OPC UA StatusCode (simple mapping)
    /// </summary>
    public static StatusCode MapDataQualityToUa(DataQuality quality)
    {
        return quality switch
        {
            DataQuality.Good => StatusCodes.Good,
            DataQuality.Uncertain => StatusCodes.Uncertain,
            DataQuality.Bad => StatusCodes.Bad,
            _ => StatusCodes.Bad
        };
    }

    /// <summary>
    /// Maps OPC UA StatusCode back to DataQuality enum
    /// </summary>
    public static DataQuality MapUaStatusToDataQuality(StatusCode statusCode)
    {
        if (StatusCode.IsGood(statusCode))
            return DataQuality.Good;
        else if (StatusCode.IsUncertain(statusCode))
            return DataQuality.Uncertain;
        else
            return DataQuality.Bad;
    }

    /// <summary>
    /// Maps OPC DA quality to simple DataQuality enum
    /// </summary>
    public static DataQuality MapDaQualityToDataQuality(ushort daQuality)
    {
        var quality = (ushort)(daQuality & OPC_QUALITY_MASK);

        return quality switch
        {
            OPC_QUALITY_GOOD => DataQuality.Good,
            OPC_QUALITY_UNCERTAIN => DataQuality.Uncertain,
            OPC_QUALITY_BAD => DataQuality.Bad,
            _ => DataQuality.Bad
        };
    }

    /// <summary>
    /// Gets a human-readable description of the quality code
    /// </summary>
    public static string GetQualityDescription(ushort daQuality)
    {
        var quality = (ushort)(daQuality & OPC_QUALITY_MASK);
        var substatus = (ushort)(daQuality & OPC_SUBSTATUS_MASK);

        var qualityStr = quality switch
        {
            OPC_QUALITY_GOOD => "Good",
            OPC_QUALITY_UNCERTAIN => "Uncertain",
            OPC_QUALITY_BAD => "Bad",
            _ => "Unknown"
        };

        var substatusStr = substatus switch
        {
            OPC_QUALITY_NOT_CONNECTED => "Not Connected",
            OPC_QUALITY_CONFIG_ERROR => "Configuration Error",
            OPC_QUALITY_DEVICE_FAILURE => "Device Failure",
            OPC_QUALITY_SENSOR_FAILURE => "Sensor Failure",
            OPC_QUALITY_NOT_ACTIVE => "Not Active",
            OPC_QUALITY_COMM_FAILURE => "Communication Failure",
            OPC_QUALITY_LAST_KNOWN => "Last Known Value",
            OPC_QUALITY_OUT_OF_SERVICE => "Out of Service",
            0 => "",
            _ => $"Substatus {substatus:X2}"
        };

        return string.IsNullOrEmpty(substatusStr) ? qualityStr : $"{qualityStr} ({substatusStr})";
    }

    private static StatusCode MapGoodQuality(ushort substatus)
    {
        return substatus switch
        {
            0 => StatusCodes.Good,
            OPC_QUALITY_LAST_KNOWN => StatusCodes.GoodClamped,
            _ => StatusCodes.Good
        };
    }

    private static StatusCode MapUncertainQuality(ushort substatus)
    {
        return substatus switch
        {
            OPC_QUALITY_LAST_KNOWN => StatusCodes.UncertainLastUsableValue,
            OPC_QUALITY_SENSOR_FAILURE => StatusCodes.UncertainSensorNotAccurate,
            OPC_QUALITY_CONFIG_ERROR => StatusCodes.UncertainSensorCalibration,
            OPC_QUALITY_NOT_ACTIVE => StatusCodes.UncertainInitialValue,
            _ => StatusCodes.Uncertain
        };
    }

    private static StatusCode MapBadQuality(ushort substatus)
    {
        return substatus switch
        {
            OPC_QUALITY_NOT_CONNECTED => StatusCodes.BadNoCommunication,
            OPC_QUALITY_DEVICE_FAILURE => StatusCodes.BadDeviceFailure,
            OPC_QUALITY_SENSOR_FAILURE => StatusCodes.BadSensorFailure,
            OPC_QUALITY_COMM_FAILURE => StatusCodes.BadCommunicationError,
            OPC_QUALITY_CONFIG_ERROR => StatusCodes.BadConfigurationError,
            OPC_QUALITY_NOT_ACTIVE => StatusCodes.BadNotConnected,
            OPC_QUALITY_OUT_OF_SERVICE => StatusCodes.BadOutOfService,
            _ => StatusCodes.Bad
        };
    }
}
