using Dapper;
using EVE.IPH.Infrastructure.Data.Connections;
using Microsoft.Data.Sqlite;

namespace EVE.IPH.Infrastructure.Data.Integration.Tests;

/// <summary>
/// Creates a uniquely-named shared in-memory SQLite database and seeds the schemas
/// required by all repository integration tests. Keeps a persistent connection open
/// so the in-memory database survives for the lifetime of the fixture.
/// </summary>
public sealed class InMemoryDbFixture : IDisposable
{
    private readonly SqliteConnection _keeper;

    public IDbConnectionFactory ConnectionFactory { get; }

    public InMemoryDbFixture()
    {
        string dbName = $"eveiph_test_{Guid.NewGuid():N}";
        string connectionString = $"Data Source=file:{dbName}?mode=memory&cache=shared";

        ConnectionFactory = new SqliteConnectionFactory(connectionString);

        // Keep one connection open so the in-memory database is not destroyed.
        _keeper = new SqliteConnection(connectionString);
        _keeper.Open();

        CreateSchemas();
    }

    private void CreateSchemas()
    {
        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS ESI_CHARACTER_DATA (
                CHARACTER_ID   INTEGER NOT NULL,
                CHARACTER_NAME TEXT    NOT NULL,
                CORPORATION_ID INTEGER NOT NULL DEFAULT 0,
                ALLIANCE_ID    INTEGER NULL,
                IS_DEFAULT     INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (CHARACTER_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS ESI_CORPORATION_CONNECTIONS (
                CORPORATION_ID          INTEGER NOT NULL,
                CORPORATION_NAME        TEXT    NOT NULL,
                AUTHORIZED_CHARACTER_ID INTEGER NOT NULL,
                HAS_ASSET_ACCESS        INTEGER NOT NULL DEFAULT 0,
                HAS_INDUSTRY_JOB_ACCESS INTEGER NOT NULL DEFAULT 0,
                HAS_BLUEPRINT_ACCESS    INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (CORPORATION_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS CHARACTER_SKILLS (
                CHARACTER_ID         INTEGER NOT NULL,
                SKILL_TYPE_ID        INTEGER NOT NULL,
                SKILL_NAME           TEXT    NOT NULL,
                SKILL_POINTS         INTEGER NOT NULL DEFAULT 0,
                TRAINED_SKILL_LEVEL  INTEGER NOT NULL DEFAULT 0,
                ACTIVE_SKILL_LEVEL   INTEGER NOT NULL DEFAULT 0,
                OVERRIDE_SKILL       INTEGER NOT NULL DEFAULT 0,
                OVERRIDE_LEVEL       INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (CHARACTER_ID, SKILL_TYPE_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS CHARACTER_STANDINGS (
                CHARACTER_ID INTEGER NOT NULL,
                NPC_TYPE_ID  INTEGER NOT NULL,
                NPC_TYPE     TEXT    NOT NULL,
                NPC_NAME     TEXT    NOT NULL,
                STANDING     REAL    NOT NULL DEFAULT 0,
                PRIMARY KEY (CHARACTER_ID, NPC_TYPE_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS CURRENT_RESEARCH_AGENTS (
                AGENT_ID            INTEGER NOT NULL,
                SKILL_TYPE_ID       INTEGER NOT NULL,
                RP_PER_DAY          REAL    NOT NULL DEFAULT 0,
                RESEARCH_START_DATE TEXT    NOT NULL,
                REMAINDER_POINTS    REAL    NOT NULL DEFAULT 0,
                CHARACTER_ID        INTEGER NOT NULL,
                PRIMARY KEY (AGENT_ID, CHARACTER_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS RESEARCH_AGENTS (
                AGENT_ID   INTEGER NOT NULL,
                AGENT_NAME TEXT    NOT NULL,
                RP_PER_DAY REAL    NOT NULL DEFAULT 0,
                LEVEL      INTEGER NOT NULL DEFAULT 0,
                STATION    TEXT    NOT NULL,
                PRIMARY KEY (AGENT_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS INDUSTRY_JOBS (
                jobID                 INTEGER NOT NULL,
                installerID           INTEGER NOT NULL,
                facilityID            INTEGER NOT NULL DEFAULT 0,
                locationID            INTEGER NOT NULL DEFAULT 0,
                activityID            INTEGER NOT NULL DEFAULT 0,
                blueprintID           INTEGER NOT NULL DEFAULT 0,
                blueprintTypeID       INTEGER NOT NULL DEFAULT 0,
                blueprintLocationID   INTEGER NOT NULL DEFAULT 0,
                outputLocationID      INTEGER NOT NULL DEFAULT 0,
                runs                  INTEGER NOT NULL DEFAULT 0,
                cost                  REAL    NOT NULL DEFAULT 0,
                licensedRuns          INTEGER NOT NULL DEFAULT 0,
                probability           REAL    NOT NULL DEFAULT 0,
                productTypeID         INTEGER NULL,
                status                TEXT    NOT NULL DEFAULT '',
                duration              INTEGER NOT NULL DEFAULT 0,
                startDate             TEXT    NULL,
                endDate               TEXT    NULL,
                pauseDate             TEXT    NULL,
                completedDate         TEXT    NULL,
                completedCharacterID  INTEGER NULL,
                successfulRuns        INTEGER NOT NULL DEFAULT 0,
                JobType               INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (jobID, installerID, JobType)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS ASSETS (
                ID          INTEGER NOT NULL,
                ItemID      INTEGER NOT NULL,
                LocationID  INTEGER NOT NULL DEFAULT 0,
                TypeID      INTEGER NOT NULL,
                Quantity    INTEGER NOT NULL DEFAULT 0,
                Flag        INTEGER NOT NULL DEFAULT 0,
                IsSingleton INTEGER NOT NULL DEFAULT 0,
                IsBPCopy    INTEGER NOT NULL DEFAULT 0,
                ItemName    TEXT    NOT NULL DEFAULT '',
                PRIMARY KEY (ID, ItemID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS INVENTORY_FLAGS (
                FlagID     INTEGER NOT NULL,
                FlagText   TEXT    NOT NULL,
                container  INTEGER NOT NULL DEFAULT 0,
                sort_order INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (FlagID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS OWNED_BLUEPRINTS (
                USER_ID        INTEGER NOT NULL,
                ITEM_ID        INTEGER NOT NULL,
                LOCATION_ID    INTEGER NOT NULL DEFAULT 0,
                BLUEPRINT_ID   INTEGER NOT NULL,
                BLUEPRINT_NAME TEXT    NOT NULL,
                QUANTITY       INTEGER NOT NULL DEFAULT 1,
                ME             INTEGER NOT NULL DEFAULT 0,
                TE             INTEGER NOT NULL DEFAULT 0,
                RUNS           INTEGER NOT NULL DEFAULT -1,
                BP_TYPE        INTEGER NOT NULL DEFAULT 1,
                OWNED          INTEGER NOT NULL DEFAULT 1,
                SCANNED        INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (USER_ID, BLUEPRINT_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS ITEM_PRICES_CACHE (
                typeID          INTEGER NOT NULL,
                buyVolume       REAL    NOT NULL DEFAULT 0,
                buyAvg          REAL    NOT NULL DEFAULT 0,
                buyweightedAvg  REAL    NOT NULL DEFAULT 0,
                buyMax          REAL    NOT NULL DEFAULT 0,
                buyMin          REAL    NOT NULL DEFAULT 0,
                sellVolume      REAL    NOT NULL DEFAULT 0,
                sellAvg         REAL    NOT NULL DEFAULT 0,
                sellweightedAvg REAL    NOT NULL DEFAULT 0,
                sellMax         REAL    NOT NULL DEFAULT 0,
                sellMin         REAL    NOT NULL DEFAULT 0,
                RegionOrSystem  INTEGER NOT NULL DEFAULT 0,
                UpdateDate      TEXT    NOT NULL,
                PRICE_SOURCE    INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (typeID, RegionOrSystem, PRICE_SOURCE)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS SHOPPING_LIST_ITEMS (
                TYPE_ID   INTEGER NOT NULL,
                ITEM_NAME TEXT    NOT NULL,
                QUANTITY  INTEGER NOT NULL DEFAULT 1,
                PRICE     REAL    NOT NULL DEFAULT 0,
                PRIMARY KEY (TYPE_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS INVENTORY_TYPES (
                typeID      INTEGER NOT NULL,
                typeName    TEXT    NOT NULL,
                groupID     INTEGER NOT NULL,
                volume      REAL    NOT NULL DEFAULT 0,
                portionSize INTEGER NOT NULL DEFAULT 1,
                PRIMARY KEY (typeID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS ITEM_LOOKUP (
                typeID       INTEGER NOT NULL,
                typeName     TEXT    NOT NULL,
                groupName    TEXT    NOT NULL,
                categoryName TEXT    NOT NULL,
                PRIMARY KEY (typeID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS REGIONS (
                regionID   INTEGER NOT NULL,
                regionName TEXT    NOT NULL,
                PRIMARY KEY (regionID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS SOLAR_SYSTEMS (
                solarSystemID   INTEGER NOT NULL,
                solarSystemName TEXT    NOT NULL,
                regionID        INTEGER NOT NULL,
                SECURITY        REAL    NOT NULL DEFAULT 0,
                PRIMARY KEY (solarSystemID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS STATIONS (
                STATION_ID      INTEGER NOT NULL,
                STATION_NAME    TEXT    NOT NULL,
                SOLAR_SYSTEM_ID INTEGER NOT NULL,
                regionID        INTEGER NOT NULL,
                PRIMARY KEY (STATION_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS INDUSTRY_ACTIVITIES (
                activityID   INTEGER NOT NULL,
                activityName TEXT    NOT NULL,
                PRIMARY KEY (activityID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS INVENTORY_CATEGORIES (
                groupID      INTEGER NOT NULL,
                groupName    TEXT    NOT NULL,
                categoryID   INTEGER NOT NULL,
                PRIMARY KEY (groupID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS ALL_BLUEPRINTS_FACT (
                BLUEPRINT_ID          INTEGER NOT NULL,
                ITEM_ID               INTEGER NOT NULL,
                TECH_LEVEL            INTEGER NOT NULL DEFAULT 1,
                MAX_PRODUCTION_LIMIT  INTEGER NOT NULL DEFAULT 300,
                BASE_PRODUCTION_TIME  INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (BLUEPRINT_ID)
            )
            """);

        _keeper.Execute("""
            CREATE TABLE IF NOT EXISTS ALL_BLUEPRINT_MATERIALS_FACT (
                BLUEPRINT_ID  INTEGER NOT NULL,
                MATERIAL_ID   INTEGER NOT NULL,
                QUANTITY      INTEGER NOT NULL DEFAULT 1,
                ACTIVITY      INTEGER NOT NULL DEFAULT 1
            )
            """);
    }

    public void Dispose()
    {
        _keeper.Close();
        _keeper.Dispose();
    }
}
