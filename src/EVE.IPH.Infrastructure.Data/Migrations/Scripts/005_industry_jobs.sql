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
);

CREATE INDEX IF NOT EXISTS IX_INDUSTRY_JOBS_INSTALLER
    ON INDUSTRY_JOBS (installerID, JobType);