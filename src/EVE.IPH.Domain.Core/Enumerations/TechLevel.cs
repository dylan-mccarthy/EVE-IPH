namespace EVE.IPH.Domain.Core.Enumerations;

/// <summary>
/// Technology level of an EVE item or blueprint.
/// Integer values match the SDE techLevel attribute (dgmTypeAttributes, attributeID = 422).
/// </summary>
public enum TechLevel
{
    /// <summary>Standard Tech 1 item.</summary>
    T1 = 1,

    /// <summary>Advanced Tech 2 item (invented or manufactured from T2 blueprint).</summary>
    T2 = 2,

    /// <summary>Strategic Tech 3 cruiser or component (uses relics from exploration).</summary>
    T3 = 3,

    /// <summary>Faction / storyline item (not researchable or inventable).</summary>
    Faction = 4,

    /// <summary>Structure / Upwell structure blueprint.</summary>
    Structure = 5,
}
