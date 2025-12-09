namespace server.Models;

public record CharacterSkillsResponse(
    long TotalSp,
    int UnallocatedSp,
    IReadOnlyList<SkillGroup> SkillGroups
);

public record SkillGroup(
    long GroupId,
    string GroupName,
    IReadOnlyList<CharacterSkill> Skills
);

public record CharacterSkill(
    long SkillId,
    string SkillName,
    int TrainedSkillLevel,
    long SkillPointsInSkill,
    int ActiveSkillLevel
);
