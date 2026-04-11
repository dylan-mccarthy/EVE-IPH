namespace EVE.IPH.Domain.Manufacturing.Models;

public sealed record InventionPlanInput(
    long UserRuns,
    int MaxProductionLimit,
    double BaseInventionChance,
    int EncryptionSkillLevel,
    IReadOnlyList<int> SupportingSkillLevels,
    InventionDecryptorModifier Decryptor,
    int NumberOfLaboratoryLines,
    double SingleInventionMaterialsCost,
    bool UseTypicalSkills = false);