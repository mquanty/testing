CREATE PROCEDURE [dbo].[PR_OPF_DERLevelData_InsertUpdate]
	@OptionID			INT = NULL,
	@ExecutionTypeID	INT = NULL
AS

BEGIN
	DECLARE @AllPhaseID INT, @PVSystemObjectID INT, @StorageObjectID INT
	SELECT @ExecutionTypeID		= ExecutionTypeID FROM DPF_Options WHERE OptionID = @OptionID
	SELECT @AllPhaseID			= ID FROM DGM_PhaseType WITH (NOLOCK) WHERE Name= 'ABC'
	SELECT @PVSystemObjectID	= ObjectTypeID FROM DPF_ObjectType WITH (NOLOCK) WHERE UPPER(ObjectType) ='PVSYSTEM'
	SELECT @StorageObjectID		= ObjectTypeID FROM DPF_ObjectType WITH (NOLOCK) WHERE UPPER(ObjectType) ='STORAGE'
	
	DELETE OPF_Output_DER WHERE OptionID = @OptionID AND ExecutionTypeID = @ExecutionTypeID 
	DECLARE @Products TAble 
	(
		[OptionID]					[INT]			NOT NULL,
		[SubstationID]				[INT]			NOT NULL,
		[FeederID]					[INT]			NOT NULL,
		[ExecutionTypeID]			[INT]			NOT NULL,
		[IntervalID]				[INT]			NOT NULL,
		[Interval]					[INT]			NOT NULL,
		[NodeName]					[VARCHAR](200)	NOT NULL,
		[MeterName]					[VARCHAR](200)	NOT NULL,
		[PhaseID]					[INT]			NOT NULL,
		[ElementTypeID]				[VARCHAR](200)	NOT NULL,
		[DERName]					[VARCHAR](200)	NOT NULL,
		[DERRating]					[DECIMAL](19,6) NULL,
		[ActivePowerPU]				[DECIMAL](19,6) NULL,
		[ReactivePowerPU]			[DECIMAL](19,6) NULL,
		[ActivePowerkw]				[DECIMAL](19,6) NULL,
		[PVGenerationDERWise]		[DECIMAL](19,6) NULL,
		[TotalPVGeneration]			[DECIMAL](19,6) NULL,
		[BatterykWhrated]			[DECIMAL](19,6) NULL,
		[WeightageFactor]			[DECIMAL](19,6) NULL,
		BatteryDisaggregationKW		[DECIMAL](19,6) NULL,
		SOCDIS						[DECIMAL](19,6) NULL
	);

	INSERT @Products
	SELECT  X.OptionID
			,DTR.SubstationID
			,DTR.FeederID
			,DTR.ExecutionTypeID
			,DTR.TimeResolutionID
			,DTR.Interval
			,Transformer = X.TransformerName 
			,MeterName = Child.Name 
			,DTR.PhaseID
			,elementType = CASE WHEN UPPER(M.ObjectType) ='UTILITY GRADE PV'  THEN @PVSystemObjectID 
								WHEN UPPER(M.ObjectType) ='UTILITY GRADE BATTERY' THEN @StorageObjectID 
								ELSE OBJT.ObjectTypeID 
								END 
			,DERName = Child.FullName 
			,DERRAting = CASE	WHEN DTR.PhaseID = @AllPhaseID
					THEN (PV.PowerRatingkVA*PV.Pf)/3 
					ELSE (PV.PowerRatingkVA *PV.Pf)/
					COUNT(*) OVER (PARTITION BY DTR.OptionID,DTR.Interval,X.TransformerName,Child.FullName) 
					END 
			,DTR.ActivePowerPU
			,DTR.ReactivePowerPU
			,DTR.ActivePowerkW
			,CASE	WHEN (UPPER(OBJT.ObjectType) = 'PVSYSTEM' OR UPPER(M.ObjectType) = 'UTILITY GRADE PV')
					THEN Gen.ActivePowerGenerationkW 
					ELSE NULL END  -- This  data later provided
			,CASE	WHEN (UPPER(OBJT.ObjectType) = 'PVSYSTEM' OR UPPER(M.ObjectType) = 'UTILITY GRADE PV')
					THEN SUM(CASE WHEN (UPPER(OBJT.ObjectType) = 'PVSYSTEM' OR UPPER(M.ObjectType) = 'UTILITY GRADE PV') 
									THEN Gen.ActivePowerGenerationkW 
									ELSE NULL 
									END) 
					OVER (PARTITION BY DTR.OptionID,DTR.Interval,X.TransformerName) 
					ELSE NULL END
			,CASE	WHEN (UPPER(OBJT.ObjectType) = 'STORAGE' OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY') THEN ST.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'ELECTRICVEHICLE' THEN EV.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'MICROGRID' THEN MG.RatedkWh 
					ELSE NULL END
			,CASE	WHEN (UPPER(OBJT.ObjectType) = 'STORAGE' OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY') THEN ST.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'ELECTRICVEHICLE' THEN EV.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'MICROGRID' THEN MG.RatedkWh 
					ELSE NULL END/(CASE WHEN UPPER(OBJT.ObjectType) IN  ('STORAGE','ELECTRICVEHICLE','MICROGRID') OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY'
										THEN SUM(CASE WHEN (UPPER(OBJT.ObjectType) = 'STORAGE' OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY') THEN ST.RatedkWh 
													  WHEN UPPER(OBJT.ObjectType) = 'ELECTRICVEHICLE' THEN EV.RatedkWh 
													  WHEN UPPER(OBJT.ObjectType) = 'MICROGRID' THEN MG.RatedkWh 
													  ELSE NULL END) 
										OVER (PARTITION BY DTR.OptionID,DTR.Interval,X.TransformerName ) ELSE NULL END)
			,(CASE	WHEN (UPPER(OBJT.ObjectType) = 'STORAGE' OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY') THEN ST.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'ELECTRICVEHICLE' THEN EV.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'MICROGRID' THEN MG.RatedkWh 
					ELSE NULL END/(CASE WHEN UPPER(OBJT.ObjectType) IN  ('STORAGE','ELECTRICVEHICLE','MICROGRID') OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY'
										THEN SUM(CASE	WHEN (UPPER(OBJT.ObjectType) = 'STORAGE' OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY') THEN ST.RatedkWh 
														WHEN UPPER(OBJT.ObjectType) = 'ELECTRICVEHICLE' THEN EV.RatedkWh 
														WHEN UPPER(OBJT.ObjectType) = 'MICROGRID' THEN MG.RatedkWh 
														ELSE NULL END) 
										OVER (PARTITION BY DTR.OptionID,DTR.Interval,X.TransformerName ) ELSE NULL END))*DTR.ActivePowerkW
			,(CASE	WHEN (UPPER(OBJT.ObjectType) = 'STORAGE' OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY') THEN ST.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'ELECTRICVEHICLE' THEN EV.RatedkWh 
					WHEN UPPER(OBJT.ObjectType) = 'MICROGRID' THEN MG.RatedkWh 
					ELSE NULL END/(CASE WHEN UPPER(OBJT.ObjectType) IN  ('STORAGE','ELECTRICVEHICLE','MICROGRID') OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY'
										THEN SUM(CASE	WHEN (UPPER(OBJT.ObjectType) = 'STORAGE' OR UPPER(M.ObjectType) = 'UTILITY GRADE BATTERY') THEN ST.RatedkWh 
														WHEN UPPER(OBJT.ObjectType) = 'ELECTRICVEHICLE' THEN EV.RatedkWh 
														WHEN UPPER(OBJT.ObjectType) = 'MICROGRID' THEN MG.RatedkWh ELSE NULL END) 
										OVER (PARTITION BY DTR.OptionID,DTR.Interval,X.TransformerName ) ELSE NULL END))*DTR.StateOfEnergyKWh
		FROM DPF_Transformer X WITH (NOLOCK) 
		INNER JOIN DPF_Options O WITH (NOLOCK) ON
			O.OptionID = X.OptionID
		INNER JOIN WS_GEO_DataPoint Parent WITH (NOLOCK) 
				ON X.TransformerName = Parent.Name 
		INNER JOIN WS_GEO_Marker P WITH (NOLOCK) ON 
				P.Id = Parent.MarkerId AND UPPER(P.ObjectType) = 'TRANSFORMER' 
		INNER JOIN WS_GEO_DataPoint Child WITH (NOLOCK)
				 ON Child.ParentId = Parent.Id 
		INNER JOIN WS_GEO_Marker M WITH (NOLOCK) 
				ON M.Id = Child.MarkerId 
				AND UPPER(M.ObjectType) IN ('SOLAR', 'EV', 'BATTERY','MICROGRID','UTILITY GRADE BATTERY','UTILITY GRADE PV')  
		LEFT JOIN DPF_ObjectType OBJT with (NOLOCK) ON
				 UPPER(M.ObjectType) = CASE WHEN UPPER(OBJT.ObjectType) = 'PVSYSTEM' THEN 'SOLAR'
											WHEN  UPPER(OBJT.ObjectType) ='STORAGE' THEN 'BATTERY' 
											WHEN  UPPER(OBJT.ObjectType) ='ELECTRICVEHICLE' THEN 'EV' 
											WHEN  UPPER(OBJT.ObjectType) ='MICROGRID' THEN 'MICROGRID'
											END
		INNER JOIN [OPF_Output_Transformer] DTR with (nolock) ON 
				DTR.NodeName = X.TransformerName 
				AND DTR.ExecutionTypeID= @ExecutionTypeID
				AND DTR.OptionID = @OptionID  
				AND DTR.ObjectTypeID = CASE WHEN UPPER(M.ObjectType) ='UTILITY GRADE PV'  THEN @PVSystemObjectID  WHEN UPPER(M.ObjectType) ='UTILITY GRADE BATTERY'  THEN @StorageObjectID ELSE OBJT.ObjectTypeID END 
		INNER JOIN OPF_Objective OOT WITH (NOLOCK) ON
				OOT.ID = O.OPFObjectiveID AND
				(UPPER(OOT.ShortName) = 'DOE_UB' OR UPPER(OOT.ShortName) = 'DOE_LB')
		INNER JOIN DPF_ExecutionType ET WITH (NOLOCK) ON
				ET.ID= DTR.ExecutionTypeID AND
				UPPER(ET.Name) = 'OPTIMAL POWER FLOW'
		LEFT JOIN DPF_PVSystem PV WITH (NOLOCK) ON  
				PV.PVSystemName = Child.FullName 
				AND PV.OptionID = @OptionID
		LEFT JOIN [OPF_DOE_DER_Generation] Gen ON
				Gen.NodeName = DTR.NodeName AND
				Gen.Interval = DTR.Interval AND
				Gen.TimeResolutionID = DTR.TimeResolutionID AND 
				Gen.ExecutionTypeID = @ExecutionTypeID AND
				Gen.OptionID = @OptionID and 
				Gen.DerName = PV.PVSystemName
		LEFT JOIN DPF_Storage ST WITH (NOLOCK) ON  
				ST.StorageName = Child.FullName 
				AND ST.OptionID = @OptionID
		LEFT JOIN DPF_ElectricVehicle EV WITH (NOLOCK) ON  
				EV.Name = Child.FullName 
				AND EV.OptionID = @OptionID
		LEFT JOIN DPF_Microgrid MG WITH (NOLOCK) ON  
				MG.Name = Child.FullName 
				AND MG.OptionID = @OptionID
		WHERE X.OptionID = @OptionID  AND DTR.ExecutionTypeID = @ExecutionTypeID AND (UPPER(OOT.ShortName) = 'DOE_UB' OR UPPER(OOT.ShortName) = 'DOE_LB')
	INSERT OPF_Output_DER
		(
		[OptionID] ,
		[SubstationID],
		[FeederID],
		[ExecutionTypeID],
		[TimeResolutionID],
		[Interval],
		[NodeName],
		[MeterName],
		[PhaseID],
		[ObjectTypeID],
		[DERName],
		[DERRating],
		[DERCurtailmentCoefficient],
		[PVForecast],
		[DERPVCurtailmentkW],
		[DERDOECoefficient],
		[kW],
		[kVAR],
		[DOEPowerFactor],
		[StateOfEnergyKWh]
		)
	SELECT DISTINCT 
		[OptionID],
		[SubstationID],
		[FeederID],
		[ExecutionTypeID],
		[IntervalID],
		[Interval],
		[NodeName],
		[MeterName],
		[PhaseID],
		[ElementTypeID],
		[DERName],
		[DERRating],
		CurtailmentCoefficent =[DERRating]/NULLIF(SUM(DERRating)OVER (PARTITION BY [NodeName],Interval,ElementTypeID),0),
		PVGenerationDERWise,
		PVCurtailmentkW = (
							(
								([DERRating] / NULLIF(SUM(DERRating) OVER (PARTITION BY [NodeName],Interval,ElementTypeID),0)) * TotalPVGeneration
							) * (1-ActivePowerPU)
						) ,
		IndividualPControl = (1-
								(
									[DERRating]/SUM(DERRating)
									OVER (PARTITION BY [NodeName],Interval,ElementTypeID)*TotalPVGeneration *(1-ActivePowerPU)
								)/NULLIF(PVGenerationDERWise,0)
							),
		DOEKw = COALESCE(((1-
					(
						[DERRating]/NULLIF(SUM(DERRating)
						OVER (PARTITION BY [NodeName],Interval,ElementTypeID),0) * TotalPVGeneration *(1-ActivePowerPU)
					)/NULLIF(PVGenerationDERWise,0)
				) * PVGenerationDERWise),BatteryDisaggregationKW),
		ReactivePowerkVar = [DERRating]/NULLIF(SUM(DERRating)
							OVER (PARTITION BY [NodeName],Interval,ElementTypeID),0) * 
							(TAN(ACOS(ReactivePowerPU) * ActivePowerkw)),
		ReactivePowerPU = COS(
								ATAN(
										(
											[DERRating]/NULLIF(SUM(DERRating) 
											OVER (PARTITION BY [NodeName],Interval,ElementTypeID),0) * 
											(
												TAN(ACOS(ReactivePowerPU) * ActivePowerkw)
											)
										)/
										(
											NULLIF(
												1-(
													[DERRating]/NULLIF(SUM(DERRating)
													OVER (PARTITION BY [NodeName],Interval,ElementTypeID),0) * TotalPVGeneration * (1-ActivePowerPU)
												  )/NULLIF(PVGenerationDERWise,0),0
											  ) * PVGenerationDERWise)
									)
							),
		StateofenergyinkWh = SOCDIS 
	FROM @Products
END
