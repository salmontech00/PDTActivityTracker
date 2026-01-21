IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') 
    AND name = 'ImageData'
)
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [ImageData] varbinary(MAX) NULL;
END
GO

IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') 
    AND name = 'ImageMimeType'
)
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [ImageMimeType] nvarchar(100) NULL;
END
GO
