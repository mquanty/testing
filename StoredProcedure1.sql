
DECLARE @LCV INT=1
DECLARE  @ids TABLE (id INT)
DECLARE @ModelID INT =(SELECT MAX(ID) FROM CIM_Model)

CREATE TABLE #Links --Temporary table to set the ParentName of CIM objects
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
	RDF_ID VARCHAR(250),
	Name VARCHAR(250),
	ParentRDF_ID VARCHAR(250),
	ParentName VARCHAR(250),
	ConnectivityNodeA_Id VARCHAR(250),
	ConnectivityNodeB_Id VARCHAR(250)
)

INSERT INTO #Links
(
	RDF_ID,
	Name,
	ConnectivityNodeA_Id,
	ConnectivityNodeB_Id
)
SELECT 
	CIM_ACLineSegment.RDF_ID,
	CIM_ACLineSegment.IdentifiedObject_name,
	CIM_ACLineSegment.ConnectivityNode_A_ID,
	CIM_ACLineSegment.ConnectivityNode_B_ID
FROM CIM_ACLineSegment
WHERE CIM_ACLineSegment.ModelID = @ModelID
--SELECT * FROM #Links
INSERT INTO #Links
(
	RDF_ID,
	Name,
	ConnectivityNodeA_Id,
	ConnectivityNodeB_Id
)
SELECT 
	CIM_LoadBreakSwitch.RDF_ID,
	CIM_LoadBreakSwitch.IdentifiedObject_name,
	CIM_LoadBreakSwitch.ConnectivityNode_A_ID,
	CIM_LoadBreakSwitch.ConnectivityNode_B_ID
FROM CIM_LoadBreakSwitch
WHERE CIM_LoadBreakSwitch.ModelID = @ModelID

INSERT INTO #Links
(
	RDF_ID,
	Name,
	ConnectivityNodeA_Id,
	ConnectivityNodeB_Id
)
SELECT 
	CIM_Breaker.RDF_ID,
	CIM_Breaker.IdentifiedObject_name,
	CIM_Breaker.ConnectivityNode_A_ID,
	CIM_Breaker.ConnectivityNode_B_ID
FROM CIM_Breaker
WHERE CIM_Breaker.ModelID = @ModelID

INSERT INTO #Links
(
	RDF_ID,
	Name,
	ConnectivityNodeA_Id,
	ConnectivityNodeB_Id
)
SELECT 
	CIM_Disconnector.RDF_ID,
	CIM_Disconnector.IdentifiedObject_name,
	CIM_Disconnector.ConnectivityNode_A_ID,
	CIM_Disconnector.ConnectivityNode_B_ID
FROM CIM_Disconnector
WHERE CIM_Disconnector.ModelID = @ModelID

INSERT INTO #Links
(
	RDF_ID,
	Name,
	ConnectivityNodeA_Id,
	ConnectivityNodeB_Id
)
SELECT 
	CIM_Switch.RDF_ID,
	CIM_Switch.IdentifiedObject_name,
	CIM_Switch.ConnectivityNode_A_ID,
	CIM_Switch.ConnectivityNode_B_ID
FROM CIM_Switch
WHERE CIM_Switch.ModelID = @ModelID

INSERT INTO #Links
(
	RDF_ID,
	Name,
	ConnectivityNodeA_Id,
	ConnectivityNodeB_Id
)
SELECT 
	CIM_PowerTransformer.RDF_ID,
	CIM_PowerTransformer.IdentifiedObject_name,
	CIM_PowerTransformer.ConnectivityNode_A_ID,
	CIM_PowerTransformer.ConnectivityNode_B_ID
FROM CIM_PowerTransformer
WHERE CIM_PowerTransformer.ModelID = @ModelID

INSERT INTO #Links
(
	RDF_ID,
	Name,
	ConnectivityNodeA_Id
)
SELECT 
	CIM_EnergyConsumer.RDF_ID,
	CIM_EnergyConsumer.IdentifiedObject_name,
	CIM_EnergyConsumer.ConnectivityNode_ID
FROM CIM_EnergyConsumer
WHERE CIM_EnergyConsumer.ModelID = @ModelID

INSERT INTO #Links
(
	RDF_ID,
	Name
)
SELECT 
	CIM_Substation.RDF_ID,
	CIM_Substation.IdentifiedObject_name
FROM 
	CIM_Substation INNER JOIN CIM_VoltageLevel
	ON CIM_Substation.RDF_ID = CIM_VoltageLevel.VoltageLevel_MemberOf_Substation 
	AND CIM_VoltageLevel.VoltageLevel_highVoltageLimit>30
WHERE
	CIM_Substation.ModelID = @ModelID AND
	CIM_VoltageLevel.ModelID = @ModelID

UPDATE #Links	--Set ParentId and ParentRDF_ID of Transformers that are connected to Supply. Starting point for setting
SET				--ParentName and ParentRDF_ID for other rows.
	ParentName = Parent.Name,
	ParentRDF_ID = Parent.RDF_ID
FROM
	#Links INNER JOIN
		(CIM_PowerTransformer INNER JOIN #Links Parent
		ON CIM_PowerTransformer.Equipment_MemberOf_EquipmentContainer = Parent.RDF_ID)
	ON CIM_PowerTransformer.RDF_ID = #Links.RDF_ID
	AND #Links.ParentRDF_ID IS NULL

WHILE (@LCV !=0)--Loop through to set ParentName and ParentRDF_ID while there are rows to update. Branches out starting with
BEGIN			--PowerTransformers connected to supply
	UPDATE L
	SET
		ParentName = Parent.Name,
		ParentRDF_ID = Parent.RDF_ID--,
		OUTPUT INSERTED.Id INTO @ids
	FROM
		#Links L INNER JOIN #Links Parent
		ON (L.ConnectivityNodeA_Id = Parent.ConnectivityNodeB_Id OR
		L.ConnectivityNodeA_Id = Parent.ConnectivityNodeA_Id OR
		L.ConnectivityNodeB_Id = Parent.ConnectivityNodeB_Id OR
		L.ConnectivityNodeB_Id = Parent.ConnectivityNodeA_Id)
		AND L.ParentRDF_ID IS NULL
		AND Parent.RDF_ID!= L.RDF_ID
		AND Parent.ParentRDF_ID IS NOT NULL
		--Update the ParentName and ParentRDF_ID of rows that share a ConnectivityNode with a row where ParentRDF_ID is not NULL
	SET @LCV = (SELECT COUNT(*) From @ids) --Sets @LCV to the number of rows updated. If no rows were updated, end while
	DELETE FROM @ids
END 

--Update CIM tables with their ParentName
UPDATE CIM_ACLineSegment
SET ParentName = #Links.ParentName
FROM #Links
WHERE 
	CIM_ACLineSegment.RDF_ID = #Links.RDF_ID
	AND CIM_ACLineSegment.ModelID = @ModelID

UPDATE CIM_Breaker
SET ParentName = #Links.ParentName
FROM #Links
WHERE 
	CIM_Breaker.RDF_ID = #Links.RDF_ID
	AND CIM_Breaker.ModelID = @ModelID

UPDATE CIM_LoadBreakSwitch
SET ParentName = #Links.ParentName
FROM #Links
WHERE 
	CIM_LoadBreakSwitch.RDF_ID = #Links.RDF_ID
	AND CIM_LoadBreakSwitch.ModelID = @ModelID

UPDATE CIM_Disconnector
SET ParentName = #Links.ParentName
FROM #Links
WHERE 
	CIM_Disconnector.RDF_ID = #Links.RDF_ID
	AND CIM_Disconnector.ModelID = @ModelID

UPDATE CIM_Switch
SET ParentName = #Links.ParentName
FROM #Links
WHERE 
	CIM_Switch.RDF_ID = #Links.RDF_ID
	AND CIM_Switch.ModelID = @ModelID

UPDATE CIM_PowerTransformer
SET ParentName = #Links.ParentName
FROM #Links
WHERE 
	CIM_PowerTransformer.RDF_ID = #Links.RDF_ID
	AND CIM_PowerTransformer.ModelID = @ModelID

UPDATE CIM_EnergyConsumer
SET ParentName = #Links.ParentName
FROM #Links
WHERE 
	CIM_EnergyConsumer.RDF_ID = #Links.RDF_ID
	AND CIM_EnergyConsumer.ModelID = @ModelID

DROP TABLE #Links
