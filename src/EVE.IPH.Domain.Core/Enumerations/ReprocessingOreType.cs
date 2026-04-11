namespace EVE.IPH.Domain.Core.Enumerations;

/// <summary>The form in which ore appears, which affects reprocessing yield and pricing.</summary>
public enum ReprocessingOreType
{
    /// <summary>Standard uncompressed belt or moon ore.</summary>
    Raw,

    /// <summary>Compressed ore (same yield per unit of raw ore, but smaller volume).</summary>
    Compressed,

    /// <summary>Moon ore variant mined from Upwell moon-drill operations.</summary>
    MoonOre,

    /// <summary>Ice products harvested from ice belts.</summary>
    Ice,
}
