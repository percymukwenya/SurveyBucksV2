CREATE TYPE SurveyBucks.UserIdTableType AS TABLE (
    UserId NVARCHAR(255) NOT NULL
);
GO

-- Survey Status table
CREATE TABLE SurveyBucks.SurveyStatus (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

-- Insert initial Survey Status values
INSERT INTO SurveyBucks.SurveyStatus (Name, [Description], DisplayOrder)
VALUES 
('Draft', 'Survey is in draft state and not visible to participants', 10),
('Scheduled', 'Survey is scheduled to open in the future', 20),
('Active', 'Survey is active and accepting responses', 30),
('Paused', 'Survey is temporarily paused from accepting responses', 40),
('Closed', 'Survey has closed and no longer accepts responses', 50),
('Analyzing', 'Survey is closed and results are being analyzed', 60),
('Archived', 'Survey is completed and archived', 70);
GO

-- Survey table
CREATE TABLE [SurveyBucks].[Survey](
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(250) NOT NULL,
    [OpeningDateTime] DATETIME2(7) NOT NULL,
    [ClosingDateTime] DATETIME2(7) NOT NULL,
    [DurationInSeconds] INT NOT NULL,
    [IsPublished] BIT NOT NULL,
    [IsActive] BIT NOT NULL,
    [StatusId] INT NOT NULL DEFAULT 1, -- Reference to SurveyStatus
    -- Company and industry details
    [CompanyName] NVARCHAR(150) NULL,
    [CompanyDescription] NVARCHAR(250) NULL,
    [Industry] NVARCHAR(150) NULL,
    -- Survey completion criteria
    [MinQuestions] INT NOT NULL,
    [MaxTimeInMins] INT NOT NULL,
    [RequireAllQuestions] BIT NOT NULL,
    -- Survey versioning
    [Version] INT NOT NULL DEFAULT 1,
    [IsLatestVersion] BIT NOT NULL DEFAULT 1,
    [PreviousVersionId] INT NULL,
    [VersionNotes] NVARCHAR(MAX) NULL,
    -- Template reference
    [TemplateId] INT NULL,
    -- Audit fields
    [CreatedDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_Survey_CreatedDate DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NOT NULL CONSTRAINT DF_Survey_CreatedBy DEFAULT ('system'),
    [ModifiedDate] DATETIMEOFFSET(7) NULL,
    [ModifiedBy] NVARCHAR(255) NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT DF_Survey_IsDeleted DEFAULT (0),
    [TimeStamp] ROWVERSION NOT NULL,
    CONSTRAINT PK_Survey PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_Survey_SurveyStatus FOREIGN KEY (StatusId) REFERENCES SurveyBucks.SurveyStatus(Id),
    CONSTRAINT FK_Survey_PreviousVersion FOREIGN KEY (PreviousVersionId) REFERENCES SurveyBucks.Survey(Id),
    CONSTRAINT CK_Survey_Status_Consistency CHECK (
        (StatusId = 1 AND IsPublished = 0) OR -- Draft is not published
        (StatusId IN (2, 3, 4, 5, 6, 7)) -- Other statuses can be published
    )
);
GO

-- SurveySection table
CREATE TABLE [SurveyBucks].[SurveySection](
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SurveyId] INT NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(250) NOT NULL,
    [Order] INT NOT NULL,
    [CreatedDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_SurveySection_CreatedDate DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NOT NULL CONSTRAINT DF_SurveySection_CreatedBy DEFAULT ('system'),
    [ModifiedDate] DATETIMEOFFSET(7) NULL,
    [ModifiedBy] NVARCHAR(255) NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT DF_SurveySection_IsDeleted DEFAULT (0),
    [TimeStamp] ROWVERSION NOT NULL,
    CONSTRAINT PK_SurveySection PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_SurveySection_Survey FOREIGN KEY ([SurveyId]) REFERENCES [SurveyBucks].[Survey]([Id]) ON DELETE CASCADE
);
GO

-- QuestionType table
CREATE TABLE SurveyBucks.QuestionType (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    HasChoices BIT NOT NULL DEFAULT 0,
    HasMinMaxValues BIT NOT NULL DEFAULT 0,
    HasFreeText BIT NOT NULL DEFAULT 0,
    HasMedia BIT NOT NULL DEFAULT 0,
    HasMatrix BIT NOT NULL DEFAULT 0,
    ValidationRegex NVARCHAR(500) NULL,
    DefaultMinValue INT NULL,
    DefaultMaxValue INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0
);
GO

-- Insert initial QuestionType values
INSERT INTO SurveyBucks.QuestionType 
(Name, Description, HasChoices, HasMinMaxValues, HasFreeText, HasMedia, HasMatrix, DisplayOrder)
VALUES 
('ShortText', 'Short text answer', 0, 0, 1, 0, 0, 10),
('LongText', 'Long text/essay answer', 0, 0, 1, 0, 0, 20),
('SingleChoice', 'Single choice from options', 1, 0, 0, 0, 0, 30),
('MultipleChoice', 'Multiple choices from options', 1, 0, 0, 0, 0, 40),
('Rating', 'Rating scale', 0, 1, 0, 0, 0, 50),
('Slider', 'Slider selection', 0, 1, 0, 0, 0, 60),
('Ranking', 'Rank items in order', 1, 0, 0, 0, 0, 70),
('Matrix', 'Matrix of questions', 1, 0, 0, 0, 1, 80),
('Date', 'Date picker', 0, 0, 0, 0, 0, 90),
('FileUpload', 'File upload', 0, 0, 0, 1, 0, 100),
('Image', 'Image selection', 1, 0, 0, 1, 0, 110),
('Dropdown', 'Dropdown selection', 1, 0, 0, 0, 0, 120),
('NumberInput', 'Numeric input', 0, 1, 0, 0, 0, 130),
('Email', 'Email address input', 0, 0, 1, 0, 0, 140),
('Phone', 'Phone number input', 0, 0, 1, 0, 0, 150),
('Address', 'Address input', 0, 0, 1, 0, 0, 160),
('YesNo', 'Yes/No question', 1, 0, 0, 0, 0, 170),
('LikertScale', 'Likert scale question', 1, 0, 0, 0, 0, 180);
GO

-- Question table
CREATE TABLE [SurveyBucks].[Question](
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SurveySectionId] INT NOT NULL,
    [Text] NVARCHAR(500) NOT NULL,
    [IsMandatory] BIT NOT NULL CONSTRAINT DF_Question_IsMandatory DEFAULT (0),
    [Order] INT NOT NULL,
    [QuestionTypeId] INT NOT NULL,
    [MinValue] INT NULL,
    [MaxValue] INT NULL,
    [ValidationMessage] NVARCHAR(255) NULL,
    [HelpText] NVARCHAR(500) NULL,
    [IsScreeningQuestion] BIT NOT NULL DEFAULT 0,
    [ScreeningLogic] NVARCHAR(MAX) NULL,
    [TimeoutInSeconds] INT NULL,
    [RandomizeChoices] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_Question_CreatedDate DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NOT NULL CONSTRAINT DF_Question_CreatedBy DEFAULT ('system'),
    [ModifiedDate] DATETIMEOFFSET(7) NULL,
    [ModifiedBy] NVARCHAR(255) NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT DF_Question_IsDeleted DEFAULT (0),
    [TimeStamp] ROWVERSION NOT NULL,
    CONSTRAINT PK_Question PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_Question_SurveySection FOREIGN KEY ([SurveySectionId]) REFERENCES [SurveyBucks].[SurveySection]([Id]) ON DELETE CASCADE,
    CONSTRAINT FK_Question_QuestionType FOREIGN KEY ([QuestionTypeId]) REFERENCES [SurveyBucks].[QuestionType]([Id])
);
GO

-- QuestionResponseChoice table
CREATE TABLE [SurveyBucks].[QuestionResponseChoice](
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QuestionId] INT NOT NULL,
    [Text] NVARCHAR(200) NOT NULL,
    [Value] NVARCHAR(100) NULL,
    [Order] INT NOT NULL,
    [IsExclusiveOption] BIT NOT NULL DEFAULT 0, -- For "None of the above" type options
    [CreatedDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_QRC_CreatedDate DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NOT NULL CONSTRAINT DF_QRC_CreatedBy DEFAULT ('system'),
    [ModifiedDate] DATETIMEOFFSET(7) NULL,
    [ModifiedBy] NVARCHAR(255) NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT DF_QRC_IsDeleted DEFAULT (0),
    [TimeStamp] ROWVERSION NOT NULL,
    CONSTRAINT PK_QuestionResponseChoice PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_QuestionResponseChoice_Question FOREIGN KEY ([QuestionId]) REFERENCES [SurveyBucks].[Question]([Id]) ON DELETE CASCADE
);
GO

-- Matrix questions support
CREATE TABLE SurveyBucks.MatrixRows (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    Text NVARCHAR(200) NOT NULL,
    [Order] INT NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_MatrixRows_Question FOREIGN KEY (QuestionId) 
    REFERENCES SurveyBucks.Question(Id) ON DELETE CASCADE
);
GO

CREATE TABLE SurveyBucks.MatrixColumns (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    Text NVARCHAR(200) NOT NULL,
    Value NVARCHAR(50) NOT NULL,
    [Order] INT NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_MatrixColumns_Question FOREIGN KEY (QuestionId) 
    REFERENCES SurveyBucks.Question(Id) ON DELETE CASCADE
);
GO

-- Question Logic and Branching
CREATE TABLE SurveyBucks.QuestionLogic (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    LogicType NVARCHAR(50) NOT NULL, -- 'Skip', 'Show', 'End', 'Disqualify'
    ConditionType NVARCHAR(50) NOT NULL, -- 'Equals', 'NotEquals', 'Contains', 'GreaterThan', etc.
    ConditionValue NVARCHAR(MAX) NOT NULL, -- Value to compare against
    TargetQuestionId INT NULL, -- Question to skip to (if applicable)
    TargetSectionId INT NULL, -- Section to skip to (if applicable)
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_QuestionLogic_Question FOREIGN KEY (QuestionId) 
    REFERENCES SurveyBucks.Question(Id) ON DELETE CASCADE,
    CONSTRAINT FK_QuestionLogic_TargetQuestion FOREIGN KEY (TargetQuestionId) 
    REFERENCES SurveyBucks.Question(Id),
    CONSTRAINT FK_QuestionLogic_TargetSection FOREIGN KEY (TargetSectionId) 
    REFERENCES SurveyBucks.SurveySection(Id)
);
GO

-- Response Validation Rules
CREATE TABLE SurveyBucks.ValidationRule (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    ValidationRegex NVARCHAR(500) NULL,
    ValidationScript NVARCHAR(MAX) NULL,
    ErrorMessage NVARCHAR(255) NOT NULL,
    QuestionTypeId INT NULL, -- If specific to a question type
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ValidationRule_QuestionType FOREIGN KEY (QuestionTypeId) 
    REFERENCES SurveyBucks.QuestionType(Id)
);
GO

-- Question-Validation mapping
CREATE TABLE SurveyBucks.QuestionValidation (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    ValidationRuleId INT NOT NULL,
    CustomErrorMessage NVARCHAR(255) NULL, -- Override default error message
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_QuestionValidation_Question FOREIGN KEY (QuestionId) 
    REFERENCES SurveyBucks.Question(Id) ON DELETE CASCADE,
    CONSTRAINT FK_QuestionValidation_ValidationRule FOREIGN KEY (ValidationRuleId) 
    REFERENCES SurveyBucks.ValidationRule(Id)
);
GO

-- Media support
CREATE TABLE SurveyBucks.MediaType (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    MimeTypes NVARCHAR(255) NOT NULL, -- Comma-separated list of allowed MIME types
    MaxSizeKB INT NOT NULL DEFAULT 5120, -- Default 5MB
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Insert media types
INSERT INTO SurveyBucks.MediaType (Name, MimeTypes)
VALUES 
('Image', 'image/jpeg,image/png,image/gif'),
('Document', 'application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document'),
('Audio', 'audio/mpeg,audio/wav'),
('Video', 'video/mp4,video/mpeg');
GO

-- Question Media attachments
CREATE TABLE SurveyBucks.QuestionMedia (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    MediaTypeId INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FileSize INT NOT NULL,
    StoragePath NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    AltText NVARCHAR(255) NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_QuestionMedia_Question FOREIGN KEY (QuestionId) 
    REFERENCES SurveyBucks.Question(Id) ON DELETE CASCADE,
    CONSTRAINT FK_QuestionMedia_MediaType FOREIGN KEY (MediaTypeId) 
    REFERENCES SurveyBucks.MediaType(Id)
);
GO

-- Survey Template for reusable surveys
CREATE TABLE SurveyBucks.SurveyTemplate (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(100) NULL,
    IsPublic BIT NOT NULL DEFAULT 0, -- Available to all creators
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

-----------------------------
-- PARTICIPATION TABLES
-----------------------------

-- ParticipationStatus table
CREATE TABLE SurveyBucks.ParticipationStatus (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0
);
GO

-- Insert initial ParticipationStatus values
INSERT INTO SurveyBucks.ParticipationStatus (Name, Description, DisplayOrder)
VALUES 
('Enrolled', 'User has enrolled but not started', 10),
('InProgress', 'User has started but not completed', 20),
('Completed', 'User has completed the survey', 30),
('Abandoned', 'User started but abandoned the survey', 40),
('Disqualified', 'User was disqualified during screening', 50),
('Expired', 'Survey participation window expired', 60),
('Rewarded', 'User completed and received rewards', 70);
GO

-- SurveyParticipation table
CREATE TABLE [SurveyBucks].[SurveyParticipation](
    [Id] INT IDENTITY(1,1) NOT NULL,
    [EnrolmentDateTime] DATETIME2(7) NOT NULL,
    [StartedAtDateTime] DATETIME2(7) NULL,
    [CompletedAtDateTime] DATETIME2(7) NULL,
    [UserId] NVARCHAR(255) NOT NULL, -- References Microsoft Identity Users
    [SurveyId] INT NOT NULL,
    [StatusId] INT NOT NULL DEFAULT 1, -- Reference to ParticipationStatus
    [ProgressPercentage] INT NOT NULL DEFAULT 0,
    [CurrentSectionId] INT NULL,
    [CurrentQuestionId] INT NULL,
    [LastQuestionAnsweredId] INT NULL,
    [TimeSpentInSeconds] INT NOT NULL DEFAULT 0,
    [DisqualificationReason] NVARCHAR(255) NULL,
    [CompletionCode] NVARCHAR(50) NULL,
    [CreatedDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_SurveyParticipation_CreatedDate DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NOT NULL CONSTRAINT DF_SurveyParticipation_CreatedBy DEFAULT ('system'),
    [ModifiedDate] DATETIMEOFFSET(7) NULL,
    [ModifiedBy] NVARCHAR(255) NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT DF_SurveyParticipation_IsDeleted DEFAULT (0),
    [TimeStamp] ROWVERSION NOT NULL,
    CONSTRAINT PK_SurveyParticipation PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_SurveyParticipation_Survey FOREIGN KEY ([SurveyId]) REFERENCES [SurveyBucks].[Survey]([Id]),
    CONSTRAINT FK_SurveyParticipation_Status FOREIGN KEY (StatusId) REFERENCES SurveyBucks.ParticipationStatus(Id),
    CONSTRAINT FK_SurveyParticipation_CurrentSection FOREIGN KEY (CurrentSectionId) REFERENCES SurveyBucks.SurveySection(Id),
    CONSTRAINT FK_SurveyParticipation_CurrentQuestion FOREIGN KEY (CurrentQuestionId) REFERENCES SurveyBucks.Question(Id),
    CONSTRAINT FK_SurveyParticipation_LastAnsweredQuestion FOREIGN KEY (LastQuestionAnsweredId) REFERENCES SurveyBucks.Question(Id)
    -- FK to AspNetUsers will be added after the Identity tables are created
);
GO

-- SurveyResponse table
CREATE TABLE [SurveyBucks].[SurveyResponse](
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Answer] NVARCHAR(MAX) NOT NULL,
    [ResponseDateTime] DATETIME2(7) NOT NULL,
    [SurveyParticipationId] INT NOT NULL,
    [QuestionId] INT NOT NULL,
    [MatrixRowId] INT NULL,
    [CreatedDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_SurveyResponse_CreatedDate DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NOT NULL CONSTRAINT DF_SurveyResponse_CreatedBy DEFAULT ('system'),
    [ModifiedDate] DATETIMEOFFSET(7) NULL,
    [ModifiedBy] NVARCHAR(255) NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT DF_SurveyResponse_IsDeleted DEFAULT (0),
    [TimeStamp] ROWVERSION NOT NULL,
    CONSTRAINT PK_SurveyResponse PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_SurveyResponse_SurveyParticipation FOREIGN KEY ([SurveyParticipationId]) REFERENCES [SurveyBucks].[SurveyParticipation]([Id]) ON DELETE CASCADE,
    CONSTRAINT FK_SurveyResponse_Question FOREIGN KEY ([QuestionId]) REFERENCES [SurveyBucks].[Question]([Id]),
    CONSTRAINT FK_SurveyResponse_MatrixRow FOREIGN KEY (MatrixRowId) REFERENCES SurveyBucks.MatrixRows(Id)
);
GO

-- Session state for saving partial responses
CREATE TABLE SurveyBucks.SurveySessionState (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyParticipationId INT NOT NULL,
    SessionData NVARCHAR(MAX) NOT NULL, -- JSON representation of session state
    SavedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ExpiryDate DATETIMEOFFSET(7) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SurveySessionState_SurveyParticipation FOREIGN KEY (SurveyParticipationId) 
    REFERENCES SurveyBucks.SurveyParticipation(Id) ON DELETE CASCADE
);
GO

-- Response media uploads
CREATE TABLE SurveyBucks.ResponseMedia (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyResponseId INT NOT NULL,
    MediaTypeId INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FileSize INT NOT NULL,
    StoragePath NVARCHAR(MAX) NOT NULL,
    UploadDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ResponseMedia_SurveyResponse FOREIGN KEY (SurveyResponseId) 
    REFERENCES SurveyBucks.SurveyResponse(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ResponseMedia_MediaType FOREIGN KEY (MediaTypeId) 
    REFERENCES SurveyBucks.MediaType(Id)
);
GO

-- Response time tracking
CREATE TABLE SurveyBucks.ResponseTimeTracking (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyParticipationId INT NOT NULL,
    QuestionId INT NOT NULL,
    ViewStartTime DATETIMEOFFSET(7) NOT NULL,
    ViewEndTime DATETIMEOFFSET(7) NULL,
    TimeSpentInSeconds INT NULL,
    IsAnswered BIT NOT NULL DEFAULT 0,
    DeviceInfo NVARCHAR(255) NULL, -- Optional device information
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ResponseTimeTracking_SurveyParticipation FOREIGN KEY (SurveyParticipationId) 
    REFERENCES SurveyBucks.SurveyParticipation(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ResponseTimeTracking_Question FOREIGN KEY (QuestionId) 
    REFERENCES SurveyBucks.Question(Id)
);
GO

-- Survey Feedback
CREATE TABLE SurveyBucks.SurveyFeedback (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyParticipationId INT NOT NULL,
    Rating INT NOT NULL, -- 1-5 star rating
    FeedbackText NVARCHAR(1000) NULL,
    FeedbackCategory NVARCHAR(50) NULL, -- 'ContentQuality', 'UX', 'Technical', etc.
    SubmittedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SurveyFeedback_SurveyParticipation FOREIGN KEY (SurveyParticipationId) 
    REFERENCES SurveyBucks.SurveyParticipation(Id) ON DELETE CASCADE
);
GO

-- Quality issues reporting
CREATE TABLE SurveyBucks.QualityIssueReport (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    ReportType NVARCHAR(50) NOT NULL, -- 'TechnicalIssue', 'ContentProblem', 'OffensiveContent', etc.
    ReportedBy NVARCHAR(255) NOT NULL, -- User ID
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Investigating', 'Resolved', 'Rejected'
    Description NVARCHAR(1000) NOT NULL,
    Resolution NVARCHAR(1000) NULL,
    ReportedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ResolvedDate DATETIMEOFFSET(7) NULL,
    ResolvedBy NVARCHAR(255) NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_QualityIssueReport_Survey FOREIGN KEY (SurveyId) 
    REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Survey invitation management
CREATE TABLE SurveyBucks.SurveyInvitation (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    UserId NVARCHAR(255) NOT NULL,
    InvitationCode NVARCHAR(50) NOT NULL,
    SentDate DATETIMEOFFSET(7) NULL,
    OpenedDate DATETIMEOFFSET(7) NULL,
    ExpiryDate DATETIMEOFFSET(7) NULL,
    InvitationStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Sent', 'Opened', 'Accepted', 'Declined', 'Expired'
    InvitationMethod NVARCHAR(50) NOT NULL DEFAULT 'Email', -- 'Email', 'SMS', 'InApp', etc.
    InvitationMessage NVARCHAR(MAX) NULL,
    ReminderCount INT NOT NULL DEFAULT 0,
    LastReminderDate DATETIMEOFFSET(7) NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SurveyInvitation_Survey FOREIGN KEY (SurveyId) 
    REFERENCES SurveyBucks.Survey(Id)
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_SurveyInvitation_Code 
ON SurveyBucks.SurveyInvitation (InvitationCode);
GO

CREATE NONCLUSTERED INDEX IX_SurveyInvitation_UserId 
ON SurveyBucks.SurveyInvitation (UserId);
GO

CREATE NONCLUSTERED INDEX IX_SurveyInvitation_SurveyId 
ON SurveyBucks.SurveyInvitation (SurveyId);
GO

-----------------------------
-- SURVEY ANALYTICS TABLES
-----------------------------

-- Survey-level analytics
CREATE TABLE SurveyBucks.SurveyAnalytics (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    TotalViews INT NOT NULL DEFAULT 0,
    TotalStarts INT NOT NULL DEFAULT 0,
    TotalCompletions INT NOT NULL DEFAULT 0,
    CompletionRate AS (CASE WHEN TotalStarts = 0 THEN 0 ELSE (TotalCompletions * 100.0 / TotalStarts) END) PERSISTED,
    AverageCompletionTimeSeconds INT NULL,
    DropOffRate DECIMAL(5,2) NULL,
    TotalDisqualifications INT NOT NULL DEFAULT 0,
    LastUpdated DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT FK_SurveyAnalytics_Survey FOREIGN KEY (SurveyId) 
    REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Question-level analytics
CREATE TABLE SurveyBucks.QuestionAnalytics (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    TotalResponses INT NOT NULL DEFAULT 0,
    AverageTimeToAnswerSeconds INT NULL,
    SkipRate DECIMAL(5,2) NULL,
    LastUpdated DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT FK_QuestionAnalytics_Question FOREIGN KEY (QuestionId) 
    REFERENCES SurveyBucks.Question(Id)
);
GO

-- Section-level analytics
CREATE TABLE SurveyBucks.SectionAnalytics (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveySectionId INT NOT NULL,
    TotalEntered INT NOT NULL DEFAULT 0,
    TotalCompleted INT NOT NULL DEFAULT 0,
    CompletionRate AS (CASE WHEN TotalEntered = 0 THEN 0 ELSE (TotalCompleted * 100.0 / TotalEntered) END) PERSISTED,
    AverageTimeInSectionSeconds INT NULL,
    DropOffRate DECIMAL(5,2) NULL,
    LastUpdated DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT FK_SectionAnalytics_SurveySection FOREIGN KEY (SurveySectionId) 
    REFERENCES SurveyBucks.SurveySection(Id)
);
GO

-----------------------------
-- SCREENING QUESTIONS TABLES
-----------------------------

-- Pre-survey screening questions
CREATE TABLE SurveyBucks.SurveyScreeningQuestions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    QuestionText NVARCHAR(500) NOT NULL,
    QuestionTypeId INT NOT NULL,
    [Order] INT NOT NULL,
    IsDisqualifying BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SurveyScreeningQuestions_Survey FOREIGN KEY (SurveyId) 
    REFERENCES SurveyBucks.Survey(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SurveyScreeningQuestions_QuestionType FOREIGN KEY (QuestionTypeId) 
    REFERENCES SurveyBucks.QuestionType(Id)
);
GO

-- Screening question options
CREATE TABLE SurveyBucks.ScreeningQuestionOptions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ScreeningQuestionId INT NOT NULL,
    OptionText NVARCHAR(200) NOT NULL,
    IsQualifying BIT NOT NULL DEFAULT 0, -- Does this answer qualify the user?
    [Order] INT NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ScreeningQuestionOptions_ScreeningQuestion FOREIGN KEY (ScreeningQuestionId) 
    REFERENCES SurveyBucks.SurveyScreeningQuestions(Id) ON DELETE CASCADE
);
GO

-- Screening responses
CREATE TABLE SurveyBucks.ScreeningResponses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyParticipationId INT NOT NULL,
    ScreeningQuestionId INT NOT NULL,
    Response NVARCHAR(MAX) NOT NULL,
    IsQualified BIT NOT NULL,
    ResponseDateTime DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ScreeningResponses_SurveyParticipation FOREIGN KEY (SurveyParticipationId) 
    REFERENCES SurveyBucks.SurveyParticipation(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ScreeningResponses_ScreeningQuestion FOREIGN KEY (ScreeningQuestionId) 
    REFERENCES SurveyBucks.SurveyScreeningQuestions(Id)
);
GO

-----------------------------
-- DEMOGRAPHIC TABLES
-----------------------------

-- Demographics table
CREATE TABLE [SurveyBucks].[Demographics](
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(255) NOT NULL,
    [Gender] NVARCHAR(50) NOT NULL,
    [Age] INT NOT NULL,
    [HighestEducation] NVARCHAR(255) NULL,
    [Income] DECIMAL(14, 2) NOT NULL,
    [Location] NVARCHAR(255) NOT NULL,
    [Occupation] NVARCHAR(255) NOT NULL,
    -- Enhanced personal details
    [MaritalStatus] NVARCHAR(50) NULL,
    [HouseholdSize] INT NULL,
    [HasChildren] BIT NULL,
    [NumberOfChildren] INT NULL,
    -- Enhanced location data
    [Country] NVARCHAR(100) NULL,
    [State] NVARCHAR(100) NULL,
    [City] NVARCHAR(100) NULL,
    [ZipCode] NVARCHAR(20) NULL,
    [UrbanRural] NVARCHAR(50) NULL, -- 'Urban', 'Suburban', 'Rural'
    -- Enhanced professional data
    [Industry] NVARCHAR(150) NULL,
    [JobTitle] NVARCHAR(150) NULL,
    [YearsOfExperience] INT NULL,
    [EmploymentStatus] NVARCHAR(50) NULL, -- 'Full-time', 'Part-time', 'Self-employed', etc.
    [CompanySize] NVARCHAR(50) NULL, -- 'Small', 'Medium', 'Large', 'Enterprise'
    -- Enhanced education details
    [FieldOfStudy] NVARCHAR(150) NULL,
    [YearOfGraduation] INT NULL,
    -- Technology usage
    [DeviceTypes] NVARCHAR(255) NULL, -- 'Mobile', 'Desktop', 'Tablet', etc.
    [InternetUsageHoursPerWeek] INT NULL,
    -- Financial aspects
    [IncomeCurrency] NVARCHAR(10) NULL DEFAULT 'USD',
    [CreatedDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_Demographics_CreatedDate DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NOT NULL CONSTRAINT DF_Demographics_CreatedBy DEFAULT ('system'),
    [ModifiedDate] DATETIMEOFFSET(7) NULL,
    [ModifiedBy] NVARCHAR(255) NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT DF_Demographics_IsDeleted DEFAULT (0),
    CONSTRAINT PK_Demographics PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT UK_Demographics_UserId UNIQUE (UserId)
    -- FK to AspNetUsers will be added after the Identity tables are created
);
GO

-- Profile completion tracking
CREATE TABLE SurveyBucks.DemographicProfileStatus (
    UserId NVARCHAR(255) PRIMARY KEY,
    CompletionPercentage INT NOT NULL DEFAULT 0,
    LastUpdated DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    RequiredFieldsCompleted BIT NOT NULL DEFAULT 0,
    OptionalFieldsCompleted BIT NOT NULL DEFAULT 0,
    InterestsAdded BIT NOT NULL DEFAULT 0
    -- FK to AspNetUsers will be added after the Identity tables are created
);
GO

-- Demographic history tracking
CREATE TABLE SurveyBucks.DemographicHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    ChangeDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ChangedBy NVARCHAR(255) NOT NULL
    -- FK to AspNetUsers will be added after the Identity tables are created
);
GO

-- User interests
CREATE TABLE SurveyBucks.UserInterests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    Interest NVARCHAR(100) NOT NULL,
    InterestLevel INT NULL -- Optional: 1-5 scale of interest
    -- FK to AspNetUsers will be added after the Identity tables are created
);
GO

CREATE NONCLUSTERED INDEX IX_UserInterests_UserId_Interest 
ON SurveyBucks.UserInterests (UserId, Interest);
GO

-----------------------------
-- DEMOGRAPHIC TARGETING TABLES
-----------------------------

-- Age range targeting
CREATE TABLE SurveyBucks.AgeRangeTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    MinAge INT NOT NULL,
    MaxAge INT NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_AgeRangeTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Gender targeting
CREATE TABLE SurveyBucks.GenderTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    Gender NVARCHAR(50) NOT NULL, -- 'Male', 'Female', 'Non-binary', etc.
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_GenderTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Location targeting
CREATE TABLE SurveyBucks.LocationTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    Location NVARCHAR(255) NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_LocationTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Income range targeting
CREATE TABLE SurveyBucks.IncomeRangeTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    MinIncome DECIMAL(14,2) NOT NULL,
    MaxIncome DECIMAL(14,2) NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_IncomeRangeTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Education targeting
CREATE TABLE SurveyBucks.EducationTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    Education NVARCHAR(255) NOT NULL, -- 'High School', 'Bachelor', 'Master', etc.
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_EducationTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Occupation targeting
CREATE TABLE SurveyBucks.OccupationTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    Occupation NVARCHAR(255) NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_OccupationTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Industry targeting
CREATE TABLE SurveyBucks.IndustryTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    Industry NVARCHAR(150) NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_IndustryTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Marital status targeting
CREATE TABLE SurveyBucks.MaritalStatusTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    MaritalStatus NVARCHAR(50) NOT NULL,  -- 'Single', 'Married', 'Divorced', etc.
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_MaritalStatusTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Household size targeting
CREATE TABLE SurveyBucks.HouseholdSizeTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    MinSize INT NOT NULL,
    MaxSize INT NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_HouseholdSizeTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Parental status targeting
CREATE TABLE SurveyBucks.ParentalStatusTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    HasChildren BIT NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ParentalStatusTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- Interest targeting
CREATE TABLE SurveyBucks.InterestTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    Interest NVARCHAR(100) NOT NULL,
    MinInterestLevel INT NULL, -- Optional: minimum interest level required
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_InterestTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

CREATE NONCLUSTERED INDEX IX_InterestTargets_Interest 
ON SurveyBucks.InterestTargets (Interest);
GO

-- Country targeting
CREATE TABLE SurveyBucks.CountryTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    Country NVARCHAR(100) NOT NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_CountryTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- State targeting
CREATE TABLE SurveyBucks.StateTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    State NVARCHAR(100) NOT NULL,
    CountryId INT NULL, -- Optional link to CountryTargets
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_StateTargets_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-----------------------------
-- REWARDS AND GAMIFICATION TABLES
-----------------------------

-- Rewards table
CREATE TABLE SurveyBucks.Rewards (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SurveyId INT NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(250) NULL,
    Amount DECIMAL(14,2) NULL,
    RewardType NVARCHAR(50) NOT NULL, -- 'Cash', 'Gift', 'Points'
    RewardCategory NVARCHAR(50) NOT NULL DEFAULT 'General', -- 'Gift Card', 'Cash', 'Merchandise', 'Donation', etc.
    PointsCost INT NULL, -- Point cost if redeemable with points
    MonetaryValue DECIMAL(14,2) NULL, -- Actual monetary value
    ImageUrl NVARCHAR(255) NULL, -- URL to reward image
    RedemptionInstructions NVARCHAR(MAX) NULL, -- How to redeem
    TermsAndConditions NVARCHAR(MAX) NULL, -- Legal terms
    AvailableQuantity INT NULL, -- Limited quantity (NULL = unlimited)
    MinimumUserLevel INT NULL DEFAULT 1, -- Minimum user level required
    StartDate DATETIMEOFFSET(7) NULL, -- When reward becomes available
    EndDate DATETIMEOFFSET(7) NULL, -- When reward expires
    RedemptionUrl NVARCHAR(255) NULL, -- URL for digital redemption
    IsExternallyFulfilled BIT NOT NULL DEFAULT 0, -- Fulfilled by external system
    ExternalReferenceId NVARCHAR(100) NULL, -- Reference ID in external system
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Rewards_Survey FOREIGN KEY (SurveyId) REFERENCES SurveyBucks.Survey(Id)
);
GO

-- User Rewards table
CREATE TABLE SurveyBucks.UserRewards (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    RewardId INT NOT NULL,
    SurveyParticipationId INT NOT NULL,
    EarnedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    RedemptionStatus NVARCHAR(50) NOT NULL DEFAULT 'Unclaimed', -- 'Unclaimed', 'Claimed'
    ClaimedDate DATETIMEOFFSET(7) NULL,
    RedemptionCode NVARCHAR(100) NULL, -- Code needed for redemption
    RedemptionMethod NVARCHAR(50) NULL, -- 'Email', 'SMS', 'InApp', 'Manual', etc.
    DeliveryStatus NVARCHAR(50) NULL, -- 'Pending', 'Sent', 'Failed', 'Cancelled'
    DeliveryDate DATETIMEOFFSET(7) NULL, -- When reward was actually delivered
    PointsUsed INT NULL, -- Points spent on this reward
    MonetaryValueRedeemed DECIMAL(14,2) NULL, -- Actual monetary value redeemed
    FulfillmentProvider NVARCHAR(100) NULL, -- Entity fulfilling the reward
    FulfillmentReferenceId NVARCHAR(100) NULL, -- Reference ID in fulfillment system
    UserFeedback NVARCHAR(500) NULL, -- User feedback on the reward
    UserRating INT NULL, -- User rating (1-5)
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_UserRewards_Reward FOREIGN KEY (RewardId) REFERENCES SurveyBucks.Rewards(Id),
    CONSTRAINT FK_UserRewards_Participation FOREIGN KEY (SurveyParticipationId) REFERENCES SurveyBucks.SurveyParticipation(Id)
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

-- Points system
CREATE TABLE SurveyBucks.UserPoints (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    TotalPoints INT NOT NULL DEFAULT 0,
    AvailablePoints INT NOT NULL DEFAULT 0,
    RedeemedPoints INT NOT NULL DEFAULT 0,
    ExpiredPoints INT NOT NULL DEFAULT 0,
    PointsLevel INT NOT NULL DEFAULT 1, -- User level based on points
    LastPointEarnedDate DATETIMEOFFSET(7) NULL,
    LastPointRedeemedDate DATETIMEOFFSET(7) NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UK_UserPoints_UserId UNIQUE (UserId)
    -- FK to AspNetUsers will be added after Identity tables are created
);

-- Point transactions
CREATE TABLE SurveyBucks.PointTransactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    Amount INT NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL, -- 'Earned', 'Redeemed', 'Expired', 'Bonus', 'Referral'
    ActionType NVARCHAR(50) NOT NULL, -- 'SurveyCompletion', 'ProfileUpdate', 'DailyLogin', 'RewardRedemption', etc.
    Description NVARCHAR(255) NULL,
    ReferenceId NVARCHAR(50) NULL, -- Generic ID for the source of points (survey ID, reward ID, etc.)
    ReferenceType NVARCHAR(50) NULL, -- 'Survey', 'Reward', 'Referral', etc.
    ExpiryDate DATETIMEOFFSET(7) NULL, -- When these points expire (if applicable)
    TransactionDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

CREATE NONCLUSTERED INDEX IX_PointTransactions_UserId ON SurveyBucks.PointTransactions (UserId, TransactionDate DESC);
GO

CREATE NONCLUSTERED INDEX IX_PointTransactions_TransactionType ON SurveyBucks.PointTransactions (TransactionType);
GO

CREATE NONCLUSTERED INDEX IX_PointTransactions_ExpiryDate ON SurveyBucks.PointTransactions (ExpiryDate) WHERE ExpiryDate IS NOT NULL;
GO

-- Achievement system
CREATE TABLE SurveyBucks.Achievements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Category NVARCHAR(50) NOT NULL, -- 'Survey', 'Engagement', 'Loyalty', etc.
    ImageUrl NVARCHAR(255) NULL,
    PointsAwarded INT NOT NULL DEFAULT 0,
    RequiredActionType NVARCHAR(50) NOT NULL, -- 'CompleteSurveys', 'ConsecutiveLogins', etc.
    RequiredActionCount INT NOT NULL DEFAULT 1, -- Number of actions required
    IsRepeatable BIT NOT NULL DEFAULT 0, -- Can be earned multiple times
    RepeatCooldownDays INT NULL, -- Days before achievement can be earned again
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE SurveyBucks.UserAchievements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    AchievementId INT NOT NULL,
    EarnedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    PointsAwarded INT NOT NULL DEFAULT 0,
    EarnedCount INT NOT NULL DEFAULT 1, -- How many times earned (for repeatable achievements)
    LastEarnedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    IsNotified BIT NOT NULL DEFAULT 0, -- User was notified
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_UserAchievements_Achievements FOREIGN KEY (AchievementId) REFERENCES SurveyBucks.Achievements(Id),
    CONSTRAINT UK_UserAchievements_UserId_AchievementId UNIQUE (UserId, AchievementId)
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

CREATE NONCLUSTERED INDEX IX_UserAchievements_UserId ON SurveyBucks.UserAchievements (UserId);
GO

-- User engagement tracking
CREATE TABLE SurveyBucks.UserEngagement (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    LastLoginDate DATETIMEOFFSET(7) NULL,
    CurrentLoginStreak INT NOT NULL DEFAULT 0,
    MaxLoginStreak INT NOT NULL DEFAULT 0,
    TotalLogins INT NOT NULL DEFAULT 0,
    TotalSurveysCompleted INT NOT NULL DEFAULT 0,
    TotalSurveysStarted INT NOT NULL DEFAULT 0,
    CompletionRate AS (CASE WHEN TotalSurveysStarted = 0 THEN 0 ELSE (TotalSurveysCompleted * 100.0 / TotalSurveysStarted) END) PERSISTED,
    TotalPointsEarned INT NOT NULL DEFAULT 0,
    TotalRewardsRedeemed INT NOT NULL DEFAULT 0,
    LastActivityDate DATETIMEOFFSET(7) NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UK_UserEngagement_UserId UNIQUE (UserId)
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

-- Leaderboards
CREATE TABLE SurveyBucks.Leaderboards (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    LeaderboardType NVARCHAR(50) NOT NULL, -- 'Points', 'Surveys', 'Streak', etc.
    TimePeriod NVARCHAR(50) NOT NULL, -- 'Daily', 'Weekly', 'Monthly', 'AllTime'
    StartDate DATETIMEOFFSET(7) NULL,
    EndDate DATETIMEOFFSET(7) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    RewardPoints INT NOT NULL DEFAULT 0, -- Points awarded to top performers
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE SurveyBucks.LeaderboardEntries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LeaderboardId INT NOT NULL,
    UserId NVARCHAR(255) NOT NULL,
    Score INT NOT NULL,
    Rank INT NOT NULL,
    PreviousRank INT NULL,
    IsRewarded BIT NOT NULL DEFAULT 0,
    SnapshotDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_LeaderboardEntries_Leaderboards FOREIGN KEY (LeaderboardId) REFERENCES SurveyBucks.Leaderboards(Id),
    CONSTRAINT UK_LeaderboardEntries_LeaderboardId_UserId UNIQUE (LeaderboardId, UserId)
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

CREATE NONCLUSTERED INDEX IX_LeaderboardEntries_LeaderboardId_Score ON SurveyBucks.LeaderboardEntries (LeaderboardId, Score DESC);
GO

-- User leveling system
CREATE TABLE SurveyBucks.UserLevels (
    Level INT PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    PointsRequired INT NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    PointsMultiplier DECIMAL(5,2) NOT NULL DEFAULT 1.0, -- Point boost for this level
    UnlocksRewardCategories NVARCHAR(255) NULL, -- Comma-separated list of categories
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

-- Insert initial user levels
INSERT INTO SurveyBucks.UserLevels (Level, Name, Description, PointsRequired, PointsMultiplier)
VALUES 
(1, 'Beginner', 'Just starting out', 0, 1.0),
(2, 'Bronze', 'Building up experience', 500, 1.1),
(3, 'Silver', 'Regular participant', 2000, 1.2),
(4, 'Gold', 'Experienced surveyor', 5000, 1.3),
(5, 'Platinum', 'Survey expert', 10000, 1.5),
(6, 'Diamond', 'Survey master', 25000, 1.75),
(7, 'Elite', 'Elite surveyor status', 50000, 2.0);
GO

-- Challenge system
CREATE TABLE SurveyBucks.Challenges (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    StartDate DATETIMEOFFSET(7) NOT NULL,
    EndDate DATETIMEOFFSET(7) NOT NULL,
    RequiredActionType NVARCHAR(50) NOT NULL, -- 'CompleteSurveys', 'EarnPoints', etc.
    RequiredActionCount INT NOT NULL DEFAULT 1, -- Number of actions required
    PointsAwarded INT NOT NULL DEFAULT 0,
    RewardId INT NULL, -- Optional additional reward
    ImageUrl NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Challenges_Rewards FOREIGN KEY (RewardId) REFERENCES SurveyBucks.Rewards(Id)
);
GO

CREATE TABLE SurveyBucks.UserChallenges (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    ChallengeId INT NOT NULL,
    Progress INT NOT NULL DEFAULT 0, -- Current progress towards goal
    IsCompleted BIT NOT NULL DEFAULT 0,
    CompletedDate DATETIMEOFFSET(7) NULL,
    PointsAwarded INT NULL,
    IsRewarded BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_UserChallenges_Challenges FOREIGN KEY (ChallengeId) REFERENCES SurveyBucks.Challenges(Id),
    CONSTRAINT UK_UserChallenges_UserId_ChallengeId UNIQUE (UserId, ChallengeId)
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

CREATE NONCLUSTERED INDEX IX_UserChallenges_UserId ON SurveyBucks.UserChallenges (UserId);
GO

CREATE NONCLUSTERED INDEX IX_UserChallenges_ChallengeId ON SurveyBucks.UserChallenges (ChallengeId);
GO

-- Point earning rules
CREATE TABLE SurveyBucks.PointEarningRules (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ActionType NVARCHAR(50) NOT NULL, -- 'SurveyCompletion', 'ProfileUpdate', 'DailyLogin', etc.
    PointsAwarded INT NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    IsPercentageOfBase BIT NOT NULL DEFAULT 0, -- Is this a percentage of a base value?
    BaseValueType NVARCHAR(50) NULL, -- If percentage, what is the base? 'SurveyLength', 'MonetaryValue', etc.
    DailyCap INT NULL, -- Maximum points per day from this action (NULL = unlimited)
    IsActive BIT NOT NULL DEFAULT 1,
    StartDate DATETIMEOFFSET(7) NULL,
    EndDate DATETIMEOFFSET(7) NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

-- Insert initial point earning rules
INSERT INTO SurveyBucks.PointEarningRules (ActionType, PointsAwarded, Description, IsActive)
VALUES 
('SurveyCompletion', 10, 'Complete any survey', 1),
('ProfileUpdate', 5, 'Update your profile information', 1),
('DailyLogin', 2, 'Log in to the platform daily', 1),
('ReferFriend', 25, 'Refer a friend who joins the platform', 1),
('ConsecutiveLogin', 5, 'Log in 5 days in a row', 1);
GO

-----------------------------
-- NOTIFICATION SYSTEM
-----------------------------

-- Notification types
CREATE TABLE SurveyBucks.NotificationType (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    Category NVARCHAR(50) NOT NULL, -- 'Survey', 'Reward', 'System', etc.
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Insert notification types
INSERT INTO SurveyBucks.NotificationType (Name, Description, Category)
VALUES 
('SurveyInvitation', 'Invitation to participate in a survey', 'Survey'),
('SurveyReminder', 'Reminder to complete an enrolled survey', 'Survey'),
('SurveyCompletion', 'Confirmation of survey completion', 'Survey'),
('RewardEarned', 'Notification of reward earned', 'Reward'),
('PointsAwarded', 'Points added to account', 'Reward'),
('AchievementEarned', 'New achievement unlocked', 'Achievement'),
('LevelUp', 'Advanced to a new user level', 'System');
GO

-- User notifications
CREATE TABLE SurveyBucks.UserNotification (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    NotificationTypeId INT NOT NULL,
    Title NVARCHAR(100) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    ReferenceId NVARCHAR(50) NULL, -- ID of the related entity (survey, reward, etc.)
    ReferenceType NVARCHAR(50) NULL, -- Type of entity referenced
    DeepLink NVARCHAR(255) NULL, -- Link to the relevant page
    IsRead BIT NOT NULL DEFAULT 0,
    IsSent BIT NOT NULL DEFAULT 0,
    DeliveryChannel NVARCHAR(50) NULL DEFAULT 'InApp', -- 'InApp', 'Email', 'Push', 'SMS'
    SentDate DATETIMEOFFSET(7) NULL,
    ReadDate DATETIMEOFFSET(7) NULL,
    ExpiryDate DATETIMEOFFSET(7) NULL,
    CreatedDate DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy NVARCHAR(255) NOT NULL DEFAULT ('system'),
    ModifiedDate DATETIMEOFFSET(7) NULL,
    ModifiedBy NVARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_UserNotification_NotificationType FOREIGN KEY (NotificationTypeId) 
    REFERENCES SurveyBucks.NotificationType(Id)
    -- FK to AspNetUsers will be added after Identity tables are created
);
GO

CREATE NONCLUSTERED INDEX IX_UserNotification_UserId 
ON SurveyBucks.UserNotification (UserId, IsRead, CreatedDate DESC);
GO

-----------------------------
-- CREATE REQUIRED INDEXES
-----------------------------

-- Survey indexes
CREATE NONCLUSTERED INDEX IX_Survey_IsActive_IsPublished 
ON SurveyBucks.Survey (IsActive, IsPublished);
GO

CREATE NONCLUSTERED INDEX IX_Survey_DemographicMatching 
ON SurveyBucks.Survey (IsActive, IsPublished, OpeningDateTime, ClosingDateTime);
GO

-- Question indexes
CREATE NONCLUSTERED INDEX IX_Question_SurveySectionId 
ON SurveyBucks.Question (SurveySectionId);
GO

-- Survey participation indexes
CREATE NONCLUSTERED INDEX IX_SurveyParticipation_UserId 
ON SurveyBucks.SurveyParticipation (UserId);
GO

CREATE NONCLUSTERED INDEX IX_SurveyParticipation_SurveyId 
ON SurveyBucks.SurveyParticipation (SurveyId);
GO

CREATE NONCLUSTERED INDEX IX_SurveyParticipation_StatusId 
ON SurveyBucks.SurveyParticipation (StatusId);
GO

-- Survey response indexes
CREATE NONCLUSTERED INDEX IX_SurveyResponse_SurveyParticipationId 
ON SurveyBucks.SurveyResponse (SurveyParticipationId);
GO

CREATE NONCLUSTERED INDEX IX_SurveyResponse_QuestionId 
ON SurveyBucks.SurveyResponse (QuestionId);
GO

-- Demographic targeting indexes
CREATE NONCLUSTERED INDEX IX_AgeRangeTargets_SurveyId 
ON SurveyBucks.AgeRangeTargets (SurveyId);
GO

CREATE NONCLUSTERED INDEX IX_AgeRangeTargets_MinMax 
ON SurveyBucks.AgeRangeTargets (MinAge, MaxAge);
GO

CREATE NONCLUSTERED INDEX IX_GenderTargets_SurveyId 
ON SurveyBucks.GenderTargets (SurveyId);
GO

CREATE NONCLUSTERED INDEX IX_GenderTargets_Gender 
ON SurveyBucks.GenderTargets (Gender);
GO

CREATE NONCLUSTERED INDEX IX_LocationTargets_SurveyId 
ON SurveyBucks.LocationTargets (SurveyId);
GO

CREATE NONCLUSTERED INDEX IX_LocationTargets_Location 
ON SurveyBucks.LocationTargets (Location);
GO

CREATE NONCLUSTERED INDEX IX_IncomeRangeTargets_SurveyId 
ON SurveyBucks.IncomeRangeTargets (SurveyId);
GO

CREATE NONCLUSTERED INDEX IX_EducationTargets_SurveyId 
ON SurveyBucks.EducationTargets (SurveyId);
GO

CREATE NONCLUSTERED INDEX IX_OccupationTargets_SurveyId 
ON SurveyBucks.OccupationTargets (SurveyId);
GO

-----------------------------
-- FOREIGN KEY CONSTRAINTS TO IDENTITY TABLES
-----------------------------

-- Note: These should be executed after the AspNetUsers table is created by the Identity framework

/*
-- Sample constraints to be added after Users is created:

ALTER TABLE SurveyBucks.Demographics
ADD CONSTRAINT FK_Demographics_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.DemographicProfileStatus
ADD CONSTRAINT FK_DemographicProfileStatus_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.DemographicHistory
ADD CONSTRAINT FK_DemographicHistory_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.UserInterests
ADD CONSTRAINT FK_UserInterests_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.UserRewards
ADD CONSTRAINT FK_UserRewards_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.UserPoints
ADD CONSTRAINT FK_UserPoints_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.UserAchievements
ADD CONSTRAINT FK_UserAchievements_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.UserEngagement
ADD CONSTRAINT FK_UserEngagement_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.UserChallenges
ADD CONSTRAINT FK_UserChallenges_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.UserNotification
ADD CONSTRAINT FK_UserNotification_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.SurveyParticipation
ADD CONSTRAINT FK_SurveyParticipation_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.SurveyInvitation
ADD CONSTRAINT FK_SurveyInvitation_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.LeaderboardEntries
ADD CONSTRAINT FK_LeaderboardEntries_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

ALTER TABLE SurveyBucks.PointTransactions
ADD CONSTRAINT FK_PointTransactions_Users FOREIGN KEY (UserId)
REFERENCES SurveyBucks.Users(Id);

*/