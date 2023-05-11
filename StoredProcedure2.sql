USE [cim_import_db]
GO
/****** Object:  StoredProcedure [dbo].[PR_CIM_WebGeo_Import]    Script Date: 10-May-23 8:53:08 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[PR_CIM_WebGeo_Import]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

DECLARE @Loop INT = 1
DECLARE  @ids TABLE (id INT)
DECLARE @ModelID INT =(SELECT MAX(ID) FROM CIM_Model)

CREATE TABLE #Parents
(
	MarkerId INT,
	Id INT IDENTITY(1,1) NOT NULL,
	Name VARCHAR(50),
	ParentId VARCHAR(50),
	ParentName VARCHAR(50),
	Latitude [decimal](8, 5),
	Longitude [decimal](8, 5),
	StatusId INT,
	Level INT,
	LineMarkerId INT,
	PhaseId INT,
	SequenceNumber INT,
	DisToParent FLOAT,
	Front INT
)
--Insert objects and their positions into WS_GEO_DataPoint
INSERT INTO #Parents
(	[MarkerId]
    ,[Latitude]
    ,[Longitude]
    ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId]
	,SequenceNumber
	,ParentName
)
SELECT
	25, -- Found in WS_GEO_Marker
	CIM_PositionPoint.PositionPoint_Latitude,
	CIM_PositionPoint.PositionPoint_Longitude,
	CIM_Switch.IdentifiedObject_name,
	0, --Default ParentId to 0
	CASE When CIM_Switch.Switch_normalOpen = 'false' THEN 6 --Closed. Found in WS_GEO_MarkerStatus
		ELSE 7 END,-- Open
	1, --Default Level as 1
	24,--ThreePhaseLine. Found in WS_GEO_Marker
	7, --PhaseId for ABC. Found in WS_GEO_Phase
	CIM_PositionPoint.PositionPoint_sequenceNumber,
	CIM_Switch.ParentName
FROM
	CIM_Switch INNER JOIN CIM_PositionPoint
	ON CIM_Switch.RDF_ID = CIM_PositionPoint.PosistionPoint_Object_RDF_ID
WHERE
	CIM_Switch.ModelID = @ModelID
	AND CIM_PositionPoint.ModelID = @ModelID

INSERT INTO #Parents
(	[MarkerId]
    ,[Latitude]
    ,[Longitude]
    ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId]
	,SequenceNumber
	,ParentName
)
SELECT
	25, -- Found in WS_GEO_Marker
	CIM_PositionPoint.PositionPoint_Latitude,
	CIM_PositionPoint.PositionPoint_Longitude,
	CIM_Disconnector.IdentifiedObject_name,
	0, --Default ParentId to 0
	CASE When CIM_Disconnector.Switch_normalOpen = 'false' THEN 6 --Closed. Found in WS_GEO_MarkerStatus
		ELSE 7 END,-- Open
	1, --Default Level as 1
	24,--ThreePhaseLine. Found in WS_GEO_Marker
	7, --PhaseId for ABC. Found in WS_GEO_Phase
	CIM_PositionPoint.PositionPoint_sequenceNumber,
	CIM_Disconnector.ParentName
FROM
	CIM_Disconnector INNER JOIN CIM_PositionPoint
	ON CIM_Disconnector.RDF_ID = CIM_PositionPoint.PosistionPoint_Object_RDF_ID
WHERE
	CIM_Disconnector.ModelID = @ModelID
	AND CIM_PositionPoint.ModelID = @ModelID

INSERT INTO #Parents
(	[MarkerId]
    ,[Latitude]
    ,[Longitude]
    ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId]
	,SequenceNumber
	,ParentName
)
SELECT
	25, -- Found in WS_GEO_Marker
	CIM_PositionPoint.PositionPoint_Latitude,
	CIM_PositionPoint.PositionPoint_Longitude,
	CIM_Breaker.IdentifiedObject_name,
	0, --Default ParentId to 0
	CASE When CIM_Breaker.Switch_normalOpen = 'false' THEN 6 --Closed. Found in WS_GEO_MarkerStatus
		ELSE 7 END,-- Open
	1, --Default Level as 1
	24,--ThreePhaseLine. Found in WS_GEO_Marker
	7, --PhaseId for ABC. Found in WS_GEO_Phase
	CIM_PositionPoint.PositionPoint_sequenceNumber,
	CIM_Breaker.ParentName
FROM
	CIM_Breaker INNER JOIN CIM_PositionPoint
	ON CIM_Breaker.RDF_ID = CIM_PositionPoint.PosistionPoint_Object_RDF_ID
WHERE
	CIM_Breaker.ModelID = @ModelID
	AND CIM_PositionPoint.ModelID = @ModelID

INSERT INTO #Parents
(	[MarkerId]
    ,[Latitude]
    ,[Longitude]
    ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId]
	,SequenceNumber
	,ParentName
)
SELECT
	12, -- Found in WS_GEO_Marker
	CIM_PositionPoint.PositionPoint_Latitude,
	CIM_PositionPoint.PositionPoint_Longitude,
	CIM_ACLineSegment.IdentifiedObject_name,
	0, --Default ParentId to 0
	1,-- Open
	1, --Default Level as 1
	24,--ThreePhaseLine. Found in WS_GEO_Marker
	7, --PhaseId for ABC. Found in WS_GEO_Phase
	CIM_PositionPoint.PositionPoint_sequenceNumber,
	CIM_ACLineSegment.ParentName
FROM
	CIM_ACLineSegment INNER JOIN CIM_PositionPoint
	ON CIM_ACLineSegment.RDF_ID = CIM_PositionPoint.PosistionPoint_Object_RDF_ID
WHERE
	CIM_ACLineSegment.ModelID = @ModelID
	AND CIM_PositionPoint.ModelID = @ModelID

INSERT INTO #Parents
(	[MarkerId]
    ,[Latitude]
    ,[Longitude]
    ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId]
	,SequenceNumber
	,ParentName
)
SELECT
	25, -- Found in WS_GEO_Marker
	CIM_PositionPoint.PositionPoint_Latitude,
	CIM_PositionPoint.PositionPoint_Longitude,
	CIM_LoadBreakSwitch.IdentifiedObject_name,
	0, --Default ParentId to 0
	CASE When CIM_LoadBreakSwitch.Switch_normalOpen = 'false' THEN 6 --Closed. Found in WS_GEO_MarkerStatus
		ELSE 7 END,-- Open
	1, --Default Level as 1
	24,--ThreePhaseLine. Found in WS_GEO_Marker
	7, --PhaseId for ABC. Found in WS_GEO_Phase
	CIM_PositionPoint.PositionPoint_sequenceNumber,
	CIM_LoadBreakSwitch.ParentName
FROM
	CIM_LoadBreakSwitch INNER JOIN CIM_PositionPoint
	ON CIM_LoadBreakSwitch.RDF_ID = CIM_PositionPoint.PosistionPoint_Object_RDF_ID
WHERE
	CIM_LoadBreakSwitch.ModelID = @ModelID
	AND CIM_PositionPoint.ModelID = @ModelID

INSERT INTO #Parents
(	[MarkerId]
    ,[Latitude]
    ,[Longitude]
    ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId]
	,SequenceNumber
)
SELECT
	1, -- Found in WS_GEO_Marker
	CIM_PositionPoint.PositionPoint_Latitude,
	CIM_PositionPoint.PositionPoint_Longitude,
	CIM_Substation.IdentifiedObject_name,
	0, --Default ParentId to 0
	1,-- Open
	1, --Default Level as 1
	24,--ThreePhaseLine. Found in WS_GEO_Marker
	7, --PhaseId for ABC. Found in WS_GEO_Phase
	CIM_PositionPoint.PositionPoint_sequenceNumber
FROM
	CIM_Substation
	INNER JOIN CIM_PositionPoint
	ON CIM_Substation.RDF_ID = CIM_PositionPoint.PosistionPoint_Object_RDF_ID
	INNER JOIN CIM_VoltageLevel
	ON CIM_Substation.RDF_ID=CIM_VoltageLevel.VoltageLevel_MemberOf_Substation
	AND CIM_VoltageLevel.VoltageLevel_highVoltageLimit>30
	AND CIM_PositionPoint.PositionPoint_sequenceNumber=1
WHERE
	CIM_Substation.ModelID = @ModelID
	AND CIM_PositionPoint.ModelID = @ModelID
	AND CIM_VoltageLevel.ModelID = @ModelID

INSERT INTO #Parents
(	[MarkerId]
    ,[Latitude]
    ,[Longitude]
    ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId]
	,SequenceNumber
	,ParentName
)
SELECT
	14, -- Found in WS_GEO_Marker
	CIM_PositionPoint.PositionPoint_Latitude,
	CIM_PositionPoint.PositionPoint_Longitude,
	CIM_PowerTransformer.IdentifiedObject_name,
	0, --Default ParentId to 0
	1,-- Open
	1, --Default Level as 1
	24,--ThreePhaseLine. Found in WS_GEO_Marker
	7, --PhaseId for ABC. Found in WS_GEO_Phase
	CIM_PositionPoint.PositionPoint_sequenceNumber,
	CIM_PowerTransformer.ParentName
FROM
	CIM_PowerTransformer INNER JOIN CIM_PositionPoint
	ON CIM_PowerTransformer.RDF_ID = CIM_PositionPoint.PosistionPoint_Object_RDF_ID
WHERE
	CIM_PowerTransformer.ModelID = @ModelID
	AND CIM_PositionPoint.ModelID = @ModelID

CREATE TABLE #MissingOutput --Used to output the missing objects
(
	Name VARCHAR (50),
	ParentName VARCHAR(50),
	ObjectType VARCHAR (50),
)

--If the object isn't refrenced by a PositionPoint, add it to #MissingOutput
INSERT INTO #MissingOutput
SELECT 
	CIM_ACLineSegment.IdentifiedObject_name,
	CIM_ACLineSegment.ParentName,
	'Line Segment'
FROM
	CIM_ACLineSegment
WHERE
	CIM_ACLineSegment.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

INSERT INTO #MissingOutput
SELECT 
	CIM_PowerTransformer.IdentifiedObject_name,
	CIM_PowerTransformer.ParentName,
	'PowerTransformer'
FROM
	CIM_PowerTransformer
WHERE
	CIM_PowerTransformer.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

INSERT INTO #MissingOutput
SELECT 
	CIM_Switch.IdentifiedObject_name,
	CIM_Switch.ParentName,
	'Switch'
FROM
	CIM_Switch
WHERE
	CIM_Switch.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

	INSERT INTO #MissingOutput
SELECT 
	CIM_Breaker.IdentifiedObject_name,
	CIM_Breaker.ParentName,
	'Breaker'
FROM
	CIM_Breaker
WHERE
	CIM_Breaker.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

INSERT INTO #MissingOutput
SELECT 
	CIM_LoadBreakSwitch.IdentifiedObject_name,
	CIM_LoadBreakSwitch.ParentName,
	'LoadBreakSwitch'
FROM
	CIM_LoadBreakSwitch
WHERE
	CIM_LoadBreakSwitch.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

	INSERT INTO #MissingOutput
SELECT 
	CIM_Disconnector.IdentifiedObject_name,
	CIM_Disconnector.ParentName,
	'Disconnector'
FROM
	CIM_Disconnector
WHERE
	CIM_Disconnector.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

SELECT * FROM #MissingOutput

CREATE TABLE #MissingPoints --Used to fill in the holes from missing points
(
	Name VARCHAR (50),
	ParentName VARCHAR (50)
)

WHILE (@Loop != 0) --fill holes in mapping where an object doesn't have a Lat/Long Position. Loop while there are still holes
BEGIN
	--If the object isn't refrenced by a PositionPoint, add it to #MissingPoints
	INSERT INTO #MissingPoints
	SELECT 
		CIM_ACLineSegment.IdentifiedObject_name,
		CIM_ACLineSegment.ParentName
	FROM
		CIM_ACLineSegment
	WHERE
		CIM_ACLineSegment.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

	INSERT INTO #MissingPoints
	SELECT 
		CIM_PowerTransformer.IdentifiedObject_name,
		CIM_PowerTransformer.ParentName
	FROM
		CIM_PowerTransformer
	WHERE
		CIM_PowerTransformer.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

		INSERT INTO #MissingPoints
	SELECT 
		CIM_Switch.IdentifiedObject_name,
		CIM_Switch.ParentName
	FROM
		CIM_Switch
	WHERE
		CIM_Switch.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

		INSERT INTO #MissingPoints
	SELECT 
		CIM_Breaker.IdentifiedObject_name,
		CIM_Breaker.ParentName
	FROM
		CIM_Breaker
	WHERE
		CIM_Breaker.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

		INSERT INTO #MissingPoints
	SELECT 
		CIM_LoadBreakSwitch.IdentifiedObject_name,
		CIM_LoadBreakSwitch.ParentName
	FROM
		CIM_LoadBreakSwitch
	WHERE
		CIM_LoadBreakSwitch.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

		INSERT INTO #MissingPoints
	SELECT 
		CIM_Disconnector.IdentifiedObject_name,
		CIM_Disconnector.ParentName
	FROM
		CIM_Disconnector
	WHERE
		CIM_Disconnector.RDF_ID NOT IN (SELECT PosistionPoint_Object_RDF_ID FROM CIM_PositionPoint)

	--If a ParentName is that of a missing point, set the ParentName to the parent's parent
	UPDATE #Parents
	SET
		#Parents.ParentName = #MissingPoints.ParentName
		OUTPUT INSERTED.Id INTO @ids --Fill @ids with the Id's of updated rows
	FROM
		#Parents INNER JOIN #MissingPoints
		ON #Parents.ParentName = #MissingPoints.Name

	--Set @Loop to how many rows are in @ids. @ids contains the ID's of Updated rows. Loop while rows are still being updated
	SET @Loop = (SELECT COUNT(*) FROM @ids)
	DELETE FROM @ids 
	--Clear all rows from #MissingPoints to repeat process until all parents have a PositionPoint
	TRUNCATE TABLE #MissingPoints
END

--Update the Parent Id of Non-Line objects who's Parents is not a line
UPDATE Base
SET
	Base.ParentId =	Parent.Id
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.ParentName = Parent.Name
	And Parent.MarkerId !=12
	AND Base.MarkerId !=12


--Set the DisToParent for Lines who's parent is not a line using the distance formula
UPDATE Base
SET
	Base.DisToParent = ABS(SQRT(SQUARE(Base.Latitude - Parent.Latitude)+SQUARE(Base.Longitude - Parent.Longitude)))
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.ParentName = Parent.Name
	AND Base.ParentId =0
	AND Parent.MarkerId !=12
	AND Base.MarkerId = 12

--If the DisToParent of the Line point with SequenceNumber 1 is 0, then set Front to 1 to indicate that it is the front
--end of the Line
Update #Parents
Set
	Front= 1
FROM
	#Parents
	WHERE DisToParent=0
	AND SequenceNumber=1

--If the DisToParent of the Line point with SequenceNumber 1 is the smallest (non 0) distance, set Front to 1, otherwise
--set Front to 2. This indicates that the Line point is at the back of the line. Only Sequence numbers of 1 or the 
--max sequence number can be at the ends.
Update Base
Set
	Front= Case WHEN (Parent.DisToParent < Base.DisToParent AND Base.Front IS NULL) THEN 2 
	WHEN (Parent.DisToParent > Base.DisToParent AND Base.Front IS NULL) THEN 1 
	END
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.Name = Parent.Name
	AND Base.SequenceNumber != Parent.SequenceNumber
	AND Parent.SequenceNumber = (SELECT MAX(PositionPoint_sequenceNumber) FROM CIM_PositionPoint P, CIM_ACLineSegment AC Where AC.RDF_ID=P.PosistionPoint_Object_RDF_ID AND AC.IdentifiedObject_name = Parent.Name)
	AND Base.SequenceNumber=1
	AND Base.Front IS NULL

--If the Line point with SequenceNumber 1 is the front, set the ParentIds of all other points to the Id of the point with
--a SequenceNumber that is one less than the current SequenceNumber
Update Base
Set
	ParentId = Parent.Id
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.Name = Parent.Name
	INNER JOIN #Parents Front
	ON Base.Name = Front.Name
	AND Front.Front = 1
	AND Base.SequenceNumber = Parent.SequenceNumber+1
	AND Base.MarkerId=12

--If the Point with SequenceNumber 1 is the back, set the ParentIds of all other to the Id of the point with
--a SequenceNumber that is one more than the base SequenceNumber
Update Base
Set
	ParentId = Parent.Id
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.Name = Parent.Name
	INNER JOIN #Parents Front
	ON Base.Name = Front.Name
	AND Front.Front = 2
	AND Base.SequenceNumber = Parent.SequenceNumber-1
	AND Base.MarkerId=12

--Loop through all Lines that do not have a DisToParent set yet. These are lines who's Parents are also Lines
Declare @SetLoop INT = 1
While (@SetLoop >0)
Begin

--Set the DisToParent for Lines who's parent is a line. Case for when the Line is to the point of the parent with
--SequenceNumber 1
UPDATE Base
SET
	Base.DisToParent = ABS(SQRT(SQUARE(Base.Latitude - Parent.Latitude)+SQUARE(Base.Longitude - Parent.Longitude)))
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.ParentName = Parent.Name
	AND Base.ParentId =0
	AND Parent.MarkerId=12
	AND Base.MarkerId=12
	AND Parent.Front = 2
	AND Parent.SequenceNumber =1

--Set the DisToParent for Lines who's parent is a line. Case for when the Line is to the point of the parent with
-- the max SequenceNumber
UPDATE Base
SET
	Base.DisToParent = ABS(SQRT(SQUARE(Base.Latitude - Parent.Latitude)+SQUARE(Base.Longitude - Parent.Longitude)))
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.ParentName = Parent.Name
	AND Base.DisToParent IS NULL
	AND Base.ParentId =0
	AND Parent.MarkerId=12
	AND Base.MarkerId=12
	AND Parent.ParentId != 0
	AND Parent.SequenceNumber = (SELECT MAX(PositionPoint_sequenceNumber) FROM CIM_PositionPoint P, CIM_ACLineSegment AC Where AC.RDF_ID=P.PosistionPoint_Object_RDF_ID AND AC.IdentifiedObject_name = Parent.Name)

--If the DisToParent of the Line point with SequenceNumber 1 is 0, then set Front to 1 to indicate that it is the front
--end of the Line
Update #Parents
Set
	Front= 1
FROM
	#Parents
	WHERE DisToParent=0
	AND SequenceNumber=1

--If the DisToParent of the Line point with SequenceNumber 1 is the smallest (non 0) distance, set Front to 1, otherwise
--set Front to 2. This indicates that the Line point is at the back of the line. Only Sequence numbers of 1 or the 
--max sequence number can be at the ends.
Update Base
Set
	Front= Case WHEN (Parent.DisToParent < Base.DisToParent AND Base.Front IS NULL) THEN 2 
	WHEN (Parent.DisToParent > Base.DisToParent AND Base.Front IS NULL) THEN 1 
	END
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.Name = Parent.Name
	AND Base.SequenceNumber != Parent.SequenceNumber
	AND Parent.SequenceNumber = (SELECT MAX(PositionPoint_sequenceNumber) FROM CIM_PositionPoint P, CIM_ACLineSegment AC Where AC.RDF_ID=P.PosistionPoint_Object_RDF_ID AND AC.IdentifiedObject_name = Parent.Name)
	AND Base.SequenceNumber=1
	AND Base.Front IS NULL

--If the Line point with SequenceNumber 1 is the front, set the ParentIds of all other points to the Id of the point with
--a SequenceNumber that is one less than the current SequenceNumber
Update Base
Set
	ParentId = Parent.Id
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.Name = Parent.Name
	INNER JOIN #Parents Front
	ON Base.Name = Front.Name
	AND Front.Front = 1
	AND Base.SequenceNumber = Parent.SequenceNumber+1
	AND Base.MarkerId=12

--If the Point with SequenceNumber 1 is the back, set the ParentIds of all other to the Id of the point with
--a SequenceNumber that is one more than the base SequenceNumber
Update Base
Set
	ParentId = Parent.Id
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.Name = Parent.Name
	INNER JOIN #Parents Front
	ON Base.Name = Front.Name
	AND Front.Front = 2
	AND Base.SequenceNumber = Parent.SequenceNumber-1
	AND Base.MarkerId=12

	--Set Loop to the number of rows that still need to be updated
	SET @SetLoop= (SELECT count(*) From #Parents WHERE DisToParent IS NULL AND MarkerId=12)
END

--Update the ParentId's of objects who's parent is a Line where the closest point is the point with SequenceNumber 1
UPDATE Base
SET
	Base.ParentId =	Parent.Id
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.ParentName = Parent.Name
	AND Base.ParentId=0 
	AND Parent.Front = 2

--Update the ParentId's of objects who's parent is a Line where the closest point is the point witht he largest SequenceNumber
UPDATE Base
SET
	Base.ParentId =	Parent.Id
FROM
	#Parents Base INNER JOIN #Parents Parent
	ON Base.ParentName = Parent.Name
	AND Base.ParentId=0 
	AND Parent.SequenceNumber = (SELECT MAX(SequenceNumber) FROM #Parents WHERE #Parents.Name = Parent.Name)

--If any ParentIds are Null, set them to 0
UPDATE #Parents
SET
	ParentId=0
WHERE ParentName is NULL

DECLARE @MaxDataPointId INT
--Variable used for the offset of Ids in WS_GEO_DataPoint
SET @MaxDataPointId = (SELECT count(*) From WS_GEO_DataPoint)

INSERT INTO [dbo].[WS_GEO_DataPoint]
	([MarkerId]
  ,[Latitude]
  ,[Longitude]
  ,[Name]
	,[ParentId]
	,[StatusId]
	,[Level]
	,[LineMarkerId]
	,[PhaseId])
SELECT
	#Parents.MarkerId,
	#Parents.Latitude,
	#Parents.Longitude,
	#Parents.Name,
	(#Parents.ParentId+@MaxDataPointId),--If there are any rows in WS_GEO_DataPoint, ParentId's need to be shifted by the number of rows already in the table
	#Parents.StatusId,
	#Parents.Level,
	#Parents.LineMarkerId,
	#Parents.PhaseId
FROM
	#Parents

--Drop temporary tables
drop table #MissingOutput
drop table #Parents
drop table #MissingPoints
END
