CREATE PROCEDURE [dbo].[PR_OPF_Output_DERSchedule_Adjustment_InsertUpdate]
	@OptionID			INT = NULL-- DPF_Option
	,@ExecutionSourceID INT = NULL-- DPF_ExecutionSource
	,@OPFObjectiveID	INT = NULL --OPF_Objective
AS

BEGIN
	DECLARE @PVObjectTypeID			INT, 
			@StorageObjectTypeID	INT,
			@ExecutionTypeID		INT,
			@DERScheduleMessageID	INT

	SELECT	@ExecutionTypeID	= ExecutionTypeID, 
			@ExecutionSourceID	= ExecutionSourceID,
			@OPFObjectiveID		= OPFObjectiveID 
	FROM DPF_Options WITH (NOLOCK) 
	WHERE OptionID = @OptionID

	SELECT TOP (1) @DERScheduleMessageID	= ID FROM  DPF_DERScheduleMessage WITH (NOLOCK) ORDER BY ID DESC
	SELECT @PVObjectTypeID					= ObjectTypeID FROM DPF_ObjectType WITH (NOLOCK) WHERE UPPER(ObjectType) ='PVSYSTEM'
	SELECT @StorageObjectTypeID				= ObjectTypeID FROM DPF_ObjectType WITH (NOLOCK) WHERE UPPER(ObjectType) ='STORAGE'

	DELETE OPF_Output_DER 
	WHERE OptionID = @OptionID 
			AND  ExecutionTypeID = @ExecutionTypeID 
	DECLARE @NodeLevelData TABLE 
	(
		[OptionID]					[INT]			NOT NULL,
		[SubstationID]				[INT]			NOT NULL,
		[FeederID]					[INT]			NOT NULL,
		[ExecutionTypeID]			[INT]			NOT NULL,
		TimeResolutionID			[INT]			NOT NULL,
		[Interval]					[INT]			NOT NULL,
		[NodeName]					[VARCHAR](200)	NOT NULL,
		[pAdjustAggregation]		[DECIMAL](19,6) NULL,
		[MeterName]					[VARCHAR](200)	NOT NULL,
		[PhaseID]					[INT]			NOT NULL,
		[ElementTypeID]				[VARCHAR](200)	NOT NULL,
		[ElementType]				[VARCHAR](200)	NULL,
		[AssociatedDERName]			[VARCHAR](200)	NOT NULL,
		[ScheduledDERName]			[VARCHAR](200)	NULL,
		[ScheduleDERValue]			[DECIMAL](19,6) NULL
	);

	INSERT @NodeLevelData
	SELECT DISTINCT 
			 X.OptionID
			,DTR.SubstationID
			,DTR.FeederID
			,DTR.ExecutionTypeID
			,DTR.TimeResolutionID
			,DTR.Interval
			,Transformer = X.TransformerName 
			,padjust = DTR.ActivePowerkW
			,MeterName =  Child.Name 
			,DTR.PhaseID
			,ElementTypeId = CASE WHEN UPPER(M.ObjectType) ='UTILITY GRADE PV'  THEN @PVObjectTypeID 
								WHEN UPPER(M.ObjectType) ='UTILITY GRADE BATTERY' THEN @StorageObjectTypeID 
								ELSE OBJT.ObjectTypeID 
								END
			,ElementType= COALESCE(DETPV.AssetType,DETSTORG.AssetType,DETEV.AssetType,DETMG.AssetType,DETGEN.AssetType,DETSuts.AssetType,DETGradePV.AssetType)
			,DERName = Child.FullName 
			,COALESCE(DETPV.AssetName,DETSTORG.AssetName,DETEV.AssetName,DETMG.AssetName,DETGEN.AssetName,DETSuts.AssetName,DETGradePV.AssetName)
			,COALESCE(DETPV.Value,DETSTORG.Value,DETEV.Value,DETMG.Value,DETGEN.Value,DETSuts.Value,DETGradePV.Value)
		FROM DPF_Transformer X WITH (NOLOCK) 
		INNER JOIN WS_GEO_DataPoint Parent WITH (NOLOCK) 
				ON X.TransformerName = Parent.Name 
		INNER JOIN WS_GEO_Marker P WITH (NOLOCK) ON 
				P.Id = Parent.MarkerId AND UPPER(P.ObjectType) = 'TRANSFORMER' 
		INNER JOIN WS_GEO_DataPoint Child WITH (NOLOCK)
				 ON Child.ParentId = Parent.Id 
		INNER JOIN WS_GEO_Marker M WITH (NOLOCK) 
				ON M.Id = Child.MarkerId 
				AND UPPER(M.ObjectType) IN ('SOLAR', 'EV', 'BATTERY','MICROGRID','UTILITY GRADE BATTERY','UTILITY GRADE PV','BACKUP GENERATOR')  
		LEFT JOIN DPF_ObjectType OBJT with (NOLOCK) ON
				 UPPER(M.ObjectType) = CASE WHEN UPPER(OBJT.ObjectType) = 'PVSYSTEM' THEN 'SOLAR'
											WHEN  UPPER(OBJT.ObjectType) ='STORAGE' THEN 'BATTERY' 
											WHEN  UPPER(OBJT.ObjectType) ='UTILITY GRADE BATTERY' THEN 'STORAGE' 
											WHEN  UPPER(OBJT.ObjectType) ='ELECTRICVEHICLE' THEN 'EV' 
											WHEN  UPPER(OBJT.ObjectType) ='MICROGRID' THEN 'MICROGRID'
											WHEN  UPPER(OBJT.ObjectType) ='GENERATOR' THEN 'BACKUP GENERATOR'
											END
		INNER JOIN [OPF_Output_Transformer] DTR with (nolock) ON 
				DTR.NodeName = X.TransformerName 
				AND DTR.OptionID = @OptionID  
				AND DTR.ObjectTypeID = CASE WHEN UPPER(M.ObjectType) ='UTILITY GRADE PV'  THEN @PVObjectTypeID  WHEN UPPER(M.ObjectType) ='UTILITY GRADE BATTERY'  THEN @StorageObjectTypeID ELSE OBJT.ObjectTypeID END 
				AND DTR.DERScheduleMessageID= @DERScheduleMessageID
				AND DTR.OPFObjectiveID = @OPFObjectiveID
		INNER JOIN DPF_ExecutionType ET WITH (NOLOCK) ON
				ET.ID= DTR.ExecutionTypeID AND
				UPPER(ET.Name) = 'OPTIMAL POWER FLOW'
		LEFT JOIN DPF_PVSystem PV WITH (NOLOCK) ON  
				PV.PVSystemName = Child.FullName 
				AND PV.OptionID = @OptionID
		LEFT JOIN DPF_DERScheduleMessageDetail DETPV WITH (NOLOCK) ON
				UPPER(DETPV.AssetName) = UPPER(PV.PVSystemName) AND
				UPPER(DETPV.AssetType) = UPPER(OBJT.ObjectType) AND
				DETPV.DERScheduleMessageID= @DERScheduleMessageID AND
				DATEPART(HH,DETPV.EndTime)= DTR.Interval AND
				DETPV.IsMappedToWGM =1
		LEFT JOIN DPF_DERScheduleMessageDetail DETGradePV WITH (NOLOCK) ON
				UPPER(DETGradePV.AssetName) = UPPER(PV.PVSystemName) AND
				UPPER(DETGradePV.AssetType) = 'PVSYSTEM' AND
				DETGradePV.DERScheduleMessageID= @DERScheduleMessageID AND
				DATEPART(HH,DETGradePV.EndTime)= DTR.Interval AND
				DETGradePV.IsMappedToWGM =1
		LEFT JOIN DPF_Storage ST WITH (NOLOCK) ON  
				ST.StorageName = Child.FullName 
				AND ST.OptionID = @OptionID
		LEFT JOIN DPF_DERScheduleMessageDetail DETSTORG WITH (NOLOCK) ON
				UPPER(DETSTORG.AssetName) = UPPER(ST.StorageName) AND
				UPPER(DETSTORG.AssetType) = UPPER(OBJT.ObjectType) AND
				DETSTORG.DERScheduleMessageID= @DERScheduleMessageID AND
				DATEPART(HH,DETSTORG.EndTime)= DTR.Interval AND
				DETSTORG.IsMappedToWGM =1
		LEFT JOIN DPF_DERScheduleMessageDetail DETSuts WITH (NOLOCK) ON
				UPPER(DETSuts.AssetName) = UPPER(ST.StorageName) AND
				UPPER(DETSuts.AssetType) = 'STORAGE' AND
				DETSuts.DERScheduleMessageID= @DERScheduleMessageID AND
				DATEPART(HH,DETSuts.EndTime)= DTR.Interval  AND
				DETSuts.IsMappedToWGM =1
		LEFT JOIN DPF_ElectricVehicle EV WITH (NOLOCK) ON  
				EV.Name = Child.FullName 
				AND EV.OptionID = @OptionID
		LEFT JOIN DPF_DERScheduleMessageDetail DETEV WITH (NOLOCK) ON
				UPPER(DETEV.AssetName) = UPPER(EV.Name) AND
				UPPER(DETEV.AssetType) = UPPER(OBJT.ObjectType) AND
				DETEV.DERScheduleMessageID= @DERScheduleMessageID AND
				DATEPART(HH,DETEV.EndTime)= DTR.Interval AND
				DETEV.IsMappedToWGM =1
		LEFT JOIN DPF_Microgrid MG WITH (NOLOCK) ON  
				MG.Name = Child.FullName 
				AND MG.OptionID = @OptionID
		LEFT JOIN DPF_DERScheduleMessageDetail DETMG WITH (NOLOCK) ON
				UPPER(DETMG.AssetName) = UPPER(MG.Name) AND
				UPPER(DETMG.AssetType) = UPPER(OBJT.ObjectType) AND
				DETMG.DERScheduleMessageID= @DERScheduleMessageID AND
				DATEPART(HH,DETMG.EndTime)= DTR.Interval AND
				DETMG.IsMappedToWGM =1
		LEFT JOIN DPF_Generator GEN WITH (NOLOCK) ON  
				GEN.GeneratorName = ISNULL(GEN.GeneratorName,Child.FullName )
				AND GEN.OptionID = @OptionID
		LEFT JOIN DPF_DERScheduleMessageDetail DETGEN WITH (NOLOCK) ON
				UPPER(DETGEN.AssetName) = UPPER(GEN.GeneratorName) AND
				UPPER(DETGEN.AssetType) = UPPER(OBJT.ObjectType) AND
				DETGEN.DERScheduleMessageID= @DERScheduleMessageID AND
				DATEPART(HH,DETGEN.EndTime)= DTR.Interval  AND
				DETGEN.IsMappedToWGM =1
		WHERE X.OptionID = @OptionID 
				AND DTR.DERScheduleMessageID = @DERScheduleMessageID AND DTR.ExecutionTypeID = @ExecutionTypeID
		
	-- Logic to Make the Absolute value for Charging
	SELECT *,
		ABSpAdjustAggregation	= CASE WHEN ElementTypeID = @StorageObjectTypeID AND ScheduleDERValue<0 THEN ABS(pAdjustAggregation) ELSE pAdjustAggregation END, 
		ABSScheduleDERValue		= CASE WHEN ElementTypeID = @StorageObjectTypeID AND ScheduleDERValue<0 THEN ABS(ScheduleDERValue) ELSE ScheduleDERValue END, 
		IsBatteryCharge			= CASE WHEN ElementTypeID = @StorageObjectTypeID AND ScheduleDERValue<0 THEN 1 ELSE 0 END 
	INTO #Result
	FROM @NodeLevelData 
	WHERE ScheduledDERName IS NOT NULL

	
	SELECT * 
		,AdjustedKWTransformer		= ABSpAdjustAggregation
		,TotalScheduleTransformer	= SUM (ABSScheduleDERValue) OVER (PARTITION BY OptionID,SubstationID,FeederID,Interval,NodeName,ElementTypeID,PhaseID)
	INTO #TotalScheduledTable
	FROM #Result


	SELECT *
		,AdjustmentCoefficient		= ABSScheduleDERValue/NULLIF(TotalScheduleTransformer,0)
	INTO #AdjustmentCoefficient
	FROM #TotalScheduledTable
	DROP TABLE #TotalScheduledTable


	SELECT *,
		AdjustedKWDER	= AdjustmentCoefficient * ABSpAdjustAggregation
	INTO #Final
	FROM #AdjustmentCoefficient
	DROP TABLE #AdjustmentCoefficient

	-- IF Charging then AdjustedKWDER make it negative 
	UPDATE #Final 
	SET AdjustedKWDER = CASE WHEN IsBatteryCharge = 1 
							 THEN AdjustedKWDER * -1
							 ELSE AdjustedKWDER 
							 END

	INSERT OPF_Output_DER(
		[OptionID]
		,[SubstationID]
		,[ExecutionSourceID]
		,[OPFObjectiveID]
		,[DERScheduleMessageID]
		,[FeederID]
		,[ExecutionTypeID]
		,[TimeResolutionID]
		,[Interval]
		,[NodeName]
		,[MeterName]
		,[PhaseID]
		,[ObjectTypeID]
		,[DERName]
		,[kW]
	)
	SELECT
		[OptionID]
		,[SubstationID]
		,@ExecutionSourceID
		,@OPFObjectiveID
		,@DERScheduleMessageID
		,[FeederID]
		,[ExecutionTypeID]
		,[TimeResolutionID]
		,[Interval]
		,[NodeName]
		,[MeterName]
		,[PhaseID]
		,ElementTypeID
		,ScheduledDERName
		,Kw = AdjustedKWDER
	FROM #Final
	DROP TABLE #Final
	DROP TABLE #Result
END	




from sqlalchemy import create_engine, MetaData, Table, Column, Integer, String, Float, select, delete, insert, update

# Connect to the SQL Server database
engine = create_engine('mssql+pyodbc://username:password@server/database')
conn = engine.connect()

# Define metadata
metadata = MetaData()

# Define tables
DPF_Options = Table('DPF_Options', metadata,
    Column('OptionID', Integer),
    Column('ExecutionTypeID', Integer),
    Column('ExecutionSourceID', Integer),
    Column('OPFObjectiveID', Integer)
)

DPF_DERScheduleMessage = Table('DPF_DERScheduleMessage', metadata,
    Column('ID', Integer)
)

DPF_ObjectType = Table('DPF_ObjectType', metadata,
    Column('ObjectTypeID', Integer),
    Column('ObjectType', String)
)

# Define other tables similarly

# Define derived table
NodeLevelData = Table('NodeLevelData', metadata,
    Column('OptionID', Integer),
    Column('SubstationID', Integer),
    Column('FeederID', Integer),
    Column('ExecutionTypeID', Integer),
    Column('TimeResolutionID', Integer),
    Column('Interval', Integer),
    Column('NodeName', String),
    Column('pAdjustAggregation', Float),
    Column('MeterName', String),
    Column('PhaseID', Integer),
    Column('ElementTypeID', Integer),
    Column('ElementType', String),
    Column('AssociatedDERName', String),
    Column('ScheduledDERName', String),
    Column('ScheduleDERValue', Float)
)

# Define temporary tables
Result = Table('Result', metadata,
    Column('OptionID', Integer),
    Column('SubstationID', Integer),
    Column('FeederID', Integer),
    Column('ExecutionTypeID', Integer),
    Column('TimeResolutionID', Integer),
    Column('Interval', Integer),
    Column('NodeName', String),
    Column('pAdjustAggregation', Float),
    Column('MeterName', String),
    Column('PhaseID', Integer),
    Column('ElementTypeID', Integer),
    Column('ElementType', String),
    Column('AssociatedDERName', String),
    Column('ScheduledDERName', String),
    Column('ScheduleDERValue', Float),
    Column('ABSpAdjustAggregation', Float),
    Column('ABSScheduleDERValue', Float),
    Column('IsBatteryCharge', Integer)
)

TotalScheduledTable = Table('TotalScheduledTable', metadata,
    Column('OptionID', Integer),
    Column('SubstationID', Integer),
    Column('FeederID', Integer),
    Column('ExecutionTypeID', Integer),
    Column('TimeResolutionID', Integer),
    Column('Interval', Integer),
    Column('NodeName', String),
    Column('AdjustedKWTransformer', Float),
    Column('TotalScheduleTransformer', Float)
)

AdjustmentCoefficient = Table('AdjustmentCoefficient', metadata,
    Column('OptionID', Integer),
    Column('SubstationID', Integer),
    Column('FeederID', Integer),
    Column('ExecutionTypeID', Integer),
    Column('TimeResolutionID', Integer),
    Column('Interval', Integer),
    Column('NodeName', String),
    Column('AdjustmentCoefficient', Float)
)

Final = Table('Final', metadata,
    Column('OptionID', Integer),
    Column('SubstationID', Integer),
    Column('FeederID', Integer),
    Column('ExecutionTypeID', Integer),
    Column('TimeResolutionID', Integer),
    Column('Interval', Integer),
    Column('NodeName', String),
    Column('AdjustedKWDER', Float)
)

# Select data from DPF_Options
query = select([DPF_Options.c.OptionID, DPF_Options.c.ExecutionTypeID,
                DPF_Options.c.ExecutionSourceID, DPF_Options.c.OPFObjectiveID])\
            .where(DPF_Options.c.OptionID == 1)  # Assuming OptionID = 1

result = conn.execute(query)
option_data = result.fetchone()

# Retrieve values from the result
option_id = option_data['OptionID']
execution_type_id = option_data['ExecutionTypeID']
execution_source_id = option_data['ExecutionSourceID']
opf_objective_id = option_data['OPFObjectiveID']

# Select data from DPF_DERScheduleMessage
query = select([DPF_DERScheduleMessage.c.ID])\
            .order_by(DPF_DERScheduleMessage.c.ID.desc())\
            .limit(1)

result = conn.execute(query)
derschedule_message_id = result.scalar()  # Get the ID value directly

# Select data from DPF_ObjectType
query = select([DPF_ObjectType.c.ObjectTypeID])\
            .where(DPF_ObjectType.c.ObjectType.ilike('PVSYSTEM'))

result = conn.execute(query)
pv_object_type_id = result.scalar()  # Get the ObjectTypeID value directly

query = select([DPF_ObjectType.c.ObjectTypeID])\
            .where(DPF_ObjectType.c.ObjectType.ilike('STORAGE'))

result = conn.execute(query)
storage_object_type_id = result.scalar()  # Get the ObjectTypeID value directly

# Delete records from OPF_Output_DER
delete_query = delete(OPF_Output_DER).where(OPF_Output_DER.c.OptionID == option_id)\
                                      .where(OPF_Output_DER.c.ExecutionTypeID == execution_type_id)

conn.execute(delete_query)

# Perform other operations (inserts, updates, etc.) using SQLAlchemy

# Close the connection
conn.close()

							 THEN AdjustedKWDER * -1
							 ELSE AdjustedKWDER 
							 END

	
Input Tables and Columns:
DPF_Options: Columns: OptionID, ExecutionTypeID, ExecutionSourceID, OPFObjectiveID
DPF_DERScheduleMessage: Columns: ID
DPF_ObjectType: Columns: ObjectTypeID, ObjectType
WS_GEO_DataPoint: Columns: Id, ParentId, Name, MarkerId
WS_GEO_Marker: Columns: Id, ObjectType
OPF_Output_Transformer: Columns: OptionID, SubstationID, FeederID, ExecutionTypeID, TimeResolutionID, Interval, NodeName, ActivePowerkW, PhaseID, ObjectTypeID, DERScheduleMessageID, OPFObjectiveID
DPF_ExecutionType: Columns: ID, Name
DPF_PVSystem: Columns: PVSystemName, OptionID
DPF_Storage: Columns: StorageName, OptionID
DPF_ElectricVehicle: Columns: Name, OptionID
DPF_Microgrid: Columns: Name, OptionID
DPF_Generator: Columns: GeneratorName, OptionID
Output Tables and Columns:
#Result: Columns: OptionID, SubstationID, FeederID, ExecutionTypeID, TimeResolutionID, Interval, NodeName, pAdjustAggregation, MeterName, PhaseID, ElementTypeID, ElementType, AssociatedDERName, ScheduledDERName, ScheduleDERValue, ABSpAdjustAggregation, ABSScheduleDERValue, IsBatteryCharge
#TotalScheduledTable: Columns: OptionID, SubstationID, FeederID, ExecutionTypeID, TimeResolutionID, Interval, NodeName, pAdjustAggregation, MeterName, PhaseID, ElementTypeID, ElementType, AssociatedDERName, ScheduledDERName, ScheduleDERValue, ABSpAdjustAggregation, ABSScheduleDERValue, IsBatteryCharge, AdjustedKWTransformer, TotalScheduleTransformer
#AdjustmentCoefficient: Columns: OptionID, SubstationID, FeederID, ExecutionTypeID, TimeResolutionID, Interval, NodeName, pAdjustAggregation, MeterName, PhaseID, ElementTypeID, ElementType, AssociatedDERName, ScheduledDERName, ScheduleDERValue, ABSpAdjustAggregation, ABSScheduleDERValue, IsBatteryCharge, AdjustedKWTransformer, TotalScheduleTransformer, AdjustmentCoefficient
#Final: Columns: OptionID, SubstationID, FeederID, ExecutionTypeID, TimeResolutionID, Interval, NodeName, pAdjustAggregation, MeterName, PhaseID, ElementTypeID, ElementType, AssociatedDERName, ScheduledDERName, ScheduleDERValue, ABSpAdjustAggregation, ABSScheduleDERValue, IsBatteryCharge, AdjustedKWTransformer, TotalScheduleTransformer, AdjustmentCoefficient, AdjustedKWDER
OPF_Output_DER: Columns: OptionID, SubstationID, ExecutionSourceID, OPFObjectiveID, DERScheduleMessageID, FeederID, ExecutionTypeID, TimeResolutionID, Interval, NodeName, MeterName, PhaseID, ObjectTypeID, DERName, kW
