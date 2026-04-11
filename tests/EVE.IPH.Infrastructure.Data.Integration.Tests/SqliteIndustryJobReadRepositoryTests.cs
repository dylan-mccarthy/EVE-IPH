using Dapper;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Infrastructure.Data.Repositories.App;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

public sealed class SqliteIndustryJobReadRepositoryTests : IDisposable
{
    private readonly InMemoryDbFixture _fixture = new();
    private readonly IIndustryJobReadRepository _sut;

    public SqliteIndustryJobReadRepositoryTests()
    {
        _sut = new SqliteIndustryJobReadRepository(_fixture.ConnectionFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetViewRecordsAsync_ReturnsHydratedIndustryJobRows()
    {
        using System.Data.IDbConnection connection = _fixture.ConnectionFactory.CreateConnection();

        await connection.ExecuteAsync("INSERT INTO ESI_CHARACTER_DATA (CHARACTER_ID, CHARACTER_NAME, CORPORATION_ID, ALLIANCE_ID, IS_DEFAULT) VALUES (@CHARACTER_ID, @CHARACTER_NAME, @CORPORATION_ID, @ALLIANCE_ID, @IS_DEFAULT)",
            new { CHARACTER_ID = 90000001L, CHARACTER_NAME = "Kara Maken", CORPORATION_ID = 98000001L, ALLIANCE_ID = (long?)null, IS_DEFAULT = 1 });
        await connection.ExecuteAsync("INSERT INTO INDUSTRY_ACTIVITIES (activityID, activityName) VALUES (@activityID, @activityName)",
            new { activityID = 1, activityName = "Manufacturing" });
        await connection.ExecuteAsync("INSERT INTO REGIONS (regionID, regionName) VALUES (@regionID, @regionName)",
            new { regionID = 10000002L, regionName = "The Forge" });
        await connection.ExecuteAsync("INSERT INTO SOLAR_SYSTEMS (solarSystemID, solarSystemName, regionID, SECURITY) VALUES (@solarSystemID, @solarSystemName, @regionID, @SECURITY)",
            new { solarSystemID = 30000142L, solarSystemName = "Jita", regionID = 10000002L, SECURITY = 0.9 });
        await connection.ExecuteAsync("INSERT INTO STATIONS (STATION_ID, STATION_NAME, SOLAR_SYSTEM_ID, regionID) VALUES (@STATION_ID, @STATION_NAME, @SOLAR_SYSTEM_ID, @regionID)",
            new { STATION_ID = 60003760L, STATION_NAME = "Jita IV - Moon 4 - Caldari Navy Assembly Plant", SOLAR_SYSTEM_ID = 30000142L, regionID = 10000002L });
        await connection.ExecuteAsync("INSERT INTO ITEM_LOOKUP (typeID, typeName, groupName, categoryName) VALUES (@typeID, @typeName, @groupName, @categoryName)", new[]
        {
            new { typeID = 681L, typeName = "Rifter Blueprint", groupName = "Blueprint", categoryName = "Blueprint" },
            new { typeID = 587L, typeName = "Rifter", groupName = "Frigate", categoryName = "Ship" },
            new { typeID = 648L, typeName = "Badger", groupName = "Industrial", categoryName = "Ship" },
        });
        await connection.ExecuteAsync("INSERT INTO ASSETS (ID, ItemID, LocationID, TypeID, Quantity, Flag, IsSingleton, IsBPCopy, ItemName) VALUES (@ID, @ItemID, @LocationID, @TypeID, @Quantity, @Flag, @IsSingleton, @IsBPCopy, @ItemName)", new[]
        {
            new { ID = 1L, ItemID = 70000001L, LocationID = 60003760L, TypeID = 681L, Quantity = 1L, Flag = 4, IsSingleton = 1, IsBPCopy = 0, ItemName = "Named Rifter Blueprint" },
            new { ID = 1L, ItemID = 70000002L, LocationID = 60003760L, TypeID = 648L, Quantity = 1L, Flag = 4, IsSingleton = 1, IsBPCopy = 0, ItemName = "Output Container" },
        });
        await connection.ExecuteAsync(
            "INSERT INTO INDUSTRY_JOBS (jobID, installerID, facilityID, locationID, activityID, blueprintID, blueprintTypeID, blueprintLocationID, outputLocationID, runs, cost, licensedRuns, probability, productTypeID, status, duration, startDate, endDate, pauseDate, completedDate, completedCharacterID, successfulRuns, JobType) VALUES (@jobID, @installerID, @facilityID, @locationID, @activityID, @blueprintID, @blueprintTypeID, @blueprintLocationID, @outputLocationID, @runs, @cost, @licensedRuns, @probability, @productTypeID, @status, @duration, @startDate, @endDate, @pauseDate, @completedDate, @completedCharacterID, @successfulRuns, @JobType)",
            new
            {
                jobID = 42L,
                installerID = 90000001L,
                facilityID = 60003760L,
                locationID = 60003760L,
                activityID = 1,
                blueprintID = 80000001L,
                blueprintTypeID = 681L,
                blueprintLocationID = 70000001L,
                outputLocationID = 70000002L,
                runs = 2L,
                cost = 1550000.5,
                licensedRuns = 10,
                probability = 1.0,
                productTypeID = 587L,
                status = "active",
                duration = 3600,
                startDate = "2026-04-10T10:00:00Z",
                endDate = "2026-04-10T11:00:00Z",
                pauseDate = (string?)null,
                completedDate = (string?)null,
                completedCharacterID = (long?)null,
                successfulRuns = 0,
                JobType = (int)IndustryJobScope.Personal,
            });

        var result = await _sut.GetViewRecordsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].InstallerName.Should().Be("Kara Maken");
        result.Value[0].ActivityName.Should().Be("Manufacturing");
        result.Value[0].BlueprintName.Should().Be("Rifter Blueprint");
        result.Value[0].OutputItemName.Should().Be("Rifter");
        result.Value[0].InstallSystem.Should().Be("Jita");
        result.Value[0].InstallRegion.Should().Be("The Forge");
        result.Value[0].BlueprintLocation.Should().Be("Named Rifter Blueprint");
        result.Value[0].OutputLocation.Should().Be("Output Container");
    }
}