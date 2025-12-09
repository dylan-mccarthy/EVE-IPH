namespace EveIph.Server.Models;

public record ItemGroup(
    int GroupId,
    string GroupName,
    int CategoryId,
    int ItemCount
);

public record ItemGroupsResponse(
    List<ItemGroup> Groups
);

public record ItemsByGroupRequest(
    int[] GroupIds,
    int RegionId = 10000002
);

public record ItemsByGroupResponse(
    int TotalItems,
    List<int> TypeIds
);
