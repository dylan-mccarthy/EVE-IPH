namespace EVE.IPH.Domain.Core.Enumerations;

/// <summary>
/// Industry blueprint activity types as defined by the EVE SDE INDUSTRY_ACTIVITIES table.
/// Integer values match the SDE activityID column.
/// </summary>
public enum ActivityType
{
    /// <summary>Build an item from a blueprint (activityID = 1).</summary>
    Manufacturing = 1,

    /// <summary>Research a blueprint for time efficiency (activityID = 3).</summary>
    ResearchingTimeEfficiency = 3,

    /// <summary>Research a blueprint for material efficiency (activityID = 4).</summary>
    ResearchingMaterialEfficiency = 4,

    /// <summary>Copy a blueprint original (activityID = 5).</summary>
    Copying = 5,

    /// <summary>Invent a T2/T3 blueprint copy from a T1 or relic (activityID = 8).</summary>
    Invention = 8,

    /// <summary>React raw or intermediate materials into composites (activityID = 11).</summary>
    Reactions = 11,
}
