using J = Newtonsoft.Json.JsonPropertyAttribute;
using Newtonsoft.Json;
using static GisImporter2.Model.ElectricGridModel;
using System.Xml.Linq;
using System.Diagnostics;
using System.Linq;

namespace GisImporter2.Model
{
    public partial class ElectricGridModel
    {
        [J("additional_info")] public ElectricGridModelAdditionalInfo AdditionalInfo { get; set; }
        [J("asset_service_states")] public List<AssetServiceState> AssetServiceStates { get; set; }
        [J("asset_specs")] public AssetSpecs AssetSpecs { get; set; }
        [J("assets")] public Assets Assets { get; set; }
        [J("base_kvs")] public List<double> BaseKvs { get; set; }
        [J("faults")] public List<object> Faults { get; set; }
        [J("geo_coordinates")] public List<GeoCoordinate> GeoCoordinates { get; set; }
        [J("groups")] public List<Group> Groups { get; set; }
        [J("model_id")] public string ModelId { get; set; }
        [J("model_timestamp")] public DateTimeOffset ModelTimestamp { get; set; }
        [J("source_nodes")] public Dictionary<string, string> SourceNodes { get; set; }
        [J("state_variables")] public StateVariables StateVariables { get; set; }
        [J("ts_state_variables")] public TsStateVariables TsStateVariables { get; set; }

        private Dictionary<string, GeoJson> geoJsonDict;
        public Dictionary<Node, List<Asset>> nodeAssetsMap;

        public ElectricGridModel()
        {
        }

        public void AssociateGeoJsonWithAssets()
        {
            geoJsonDict = GeoCoordinates?.ToDictionary(gc => gc.ObjectId, gc => gc.Geojson);
            foreach (var asset in allAssets)
            {
                if (geoJsonDict.TryGetValue(asset.AssetId, out var geoJson))
                {
                    asset.GeoJson = geoJson;
                }
            }
        }

        public void AssociateAssetsWithNodes()
        {
            nodeAssetsMap = Asset.BuildNodeAssetsMap(allAssets);
            Asset.AssignNodeReferences(nodeAssetsMap, allAssets);
        }

        //should it be hash or just list
        public HashSet<Asset> allAssets => Assets.GetAllAssets().ToHashSet();
        public Dictionary<string, Asset> allAssetsDict => allAssets.ToDictionary(a => a.AssetId);

        //clears all parent relations, feedernames, and fromside node and to side node information
        public void ResetNetwork()
        {
            //AssociateAssetsWithNodes();
            foreach (var asset in allAssets)
            {
                asset.Parent = null;
                asset.FeederName = null;

                asset.NodeSideFrom = null;
                asset.NodeSideTo = null;

                asset.IsFeederHead = false;
            }
        }

        public List<Asset> TraceNetworkComplete(bool writetofile = false)
        {
            var allTracedAssets = new List<Asset>();
            Stopwatch sss = Stopwatch.StartNew();
            this.AllocateStates();
            //this.AssociateAssetsWithNodes();
            this.ResetNetwork();
            //LOGIC TO FIND FEEDER HEAD ASSETS
            //start from source and trace all the way down until we find any asset where the cicruitID is not null. i.e., the feeder Head Breakers.
            //var sourceAsset = allAssets.FirstOrDefault(x => x.AssetType == AssetType.VOLTAGE_SOURCE);
            var sourceAsset = Assets.Sources.First();
            var allAssetsFromSource = this.TraceFeeder(sourceAsset).ToList();
            var allFeederHeads = allAssetsFromSource.Where(x => x.IsFeederHead).ToList();
            //LOGIC TO FIND ALL FEEDER ASSETS
            //start from each feederhead and the node towards the network, normally we can find it as tonode and trace all the way down.
            var allFeederAssets = new List<List<Asset>>();
            foreach (var eachFeederHead in allFeederHeads)
            {
                eachFeederHead.FeederName = null; //reset the feedername from the previously assigned sourcename to null. it will get reassigned during tracing.
                //we may not necessarily return the list, as already inside egm.allAssets the changes are reflected, and we can get all the tracing details of all assets
                var currentTraceAssets = this.TraceFeeder(eachFeederHead, eachFeederHead.NodeSideTo);
                allFeederAssets.Add(currentTraceAssets);
            }
            sss.Stop();
            allTracedAssets = allAssetsFromSource.Except(allFeederHeads).Concat(allFeederAssets.SelectMany(x => x)).ToList();
            Console.WriteLine($"Tracing complete for {allTracedAssets.Count()} assets in {sss.ElapsedMilliseconds} ms.");
            if (writetofile)
            {
                using StreamWriter writer = new StreamWriter($"DUKE_Feeder_Tracing_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.txt");
                foreach (var asset in allTracedAssets)
                    writer.WriteLine($"Feeder:{asset?.FeederName ?? "null"} || Parent:{asset?.Parent?.AssetId ?? "null"} || FromNode:{asset?.NodeSideFrom?.Name ?? "null"} => {asset.AssetType}:{asset.AssetId} => ToNode:{asset?.NodeSideTo?.Name ?? "null"}");
            }
            return allTracedAssets;
        }

        //Breadth-First-Search Approach for tracing
        public List<Asset> TraceFeeder(Asset startingAsset, Node startingNode = null, string feederName = null)
        {
            Stopwatch st = new Stopwatch();
            st.Start();

            //if nodename not provided then take the first node as the starting node name
            if (string.IsNullOrEmpty(startingNode?.Name))
                startingNode = startingAsset?.NodeObject1 ?? startingAsset?.NodeObject2; //NodeList?.FirstOrDefault();

            bool IsSubstationTracing = false;

            //if no feedername is provided and the stataring asset's circuitid is null, most probably its a device in the substation, and we intend to find the feederheads only.
            if (string.IsNullOrEmpty(feederName))
                if (startingAsset.Groupings.CircuitId == null || startingAsset.AssetType == AssetType.VOLTAGE_SOURCE)
                {
                    //if the startingdevice's circuitid is null, find out the feederheads.
                    feederName = startingAsset.AssetId;
                    IsSubstationTracing = true;
                }
                else
                {
                    //if feedername not mentioned, take the one which is the circuitid of the starting device.
                    feederName = startingAsset.Groupings.CircuitId;
                }

            var feederAssets = new List<Asset>();
            //feederAssets.Add(startingAsset);   //add the first device to the list
            startingAsset.NodeSideTo = startingNode; //make the startingnode as to side node of the device

            Queue<(Asset asset, Node node, Asset parent)> queue = new Queue<(Asset, Node, Asset)>();
            HashSet<string> visitedNodes = new HashSet<string>();

            queue.Enqueue((startingAsset, startingNode, null));
            visitedNodes.Add(startingNode.Name);
            startingNode.FeederName = feederName;

            while (queue.Count > 0)
            {
                var (currentAsset, currentNode, parentAsset) = queue.Dequeue();

                if (string.IsNullOrEmpty(currentAsset.FeederName))
                {
                    currentAsset.FeederName = feederName;
                    currentAsset.Parent ??= parentAsset; //if the parent is null, set the parent, case when starting device has existing parent
                    feederAssets.Add(currentAsset);
                }
                // Continue to the next iteration if the FeederName is already set, never enters the else condition
                else continue;

                foreach (var connectedAsset in currentNode.ConnectedAssets)
                {
                    if (connectedAsset == currentAsset) continue;

                    if (connectedAsset.IsTraced)
                    {
                        if (connectedAsset.FeederName == feederName)
                            //the asset is already been traced but again appeared through a closed circuit or mesh circuit
                            continue;
                        else
                            //set from another feeder
                            continue;
                    }

                    var nextNode = connectedAsset.NodeList.First() == currentNode.Name ?
                        connectedAsset.NodeObject2 : connectedAsset.NodeObject1;

                    // Set from-side and to-side nodes
                    connectedAsset.NodeSideFrom = currentNode;
                    connectedAsset.NodeSideTo = nextNode;

                    //stop if the switch is open or
                    //stop if the IsSubstationTracing is true and the device's circuitid is not null, mostly for the breakers which acts as feederheads where the circuitid is not null will have this.
                    if (connectedAsset.IsOpenSwitch() ||
                        (IsSubstationTracing && connectedAsset.Groupings.CircuitId != null))
                    {
                        if (string.IsNullOrEmpty(connectedAsset.FeederName))
                        {
                            connectedAsset.FeederName = feederName;
                            connectedAsset.Parent = currentAsset;
                            feederAssets.Add(connectedAsset);
                        }

                        //if the tracing is for substation assets, and its circuitid is have some value, only occures for the feederhead breakers.
                        if (IsSubstationTracing && connectedAsset.Groupings.CircuitId != null)
                            connectedAsset.IsFeederHead = true;

                        currentNode.FeederName = feederName;
                        continue;
                    }

                    if (nextNode != null)
                    {
                        //if the node is visited for first time, just add it to the visited list and set feedername, and add the asset to queue
                        if (!visitedNodes.Contains(nextNode.Name))
                        {
                            visitedNodes.Add(nextNode.Name);
                            nextNode.FeederName = feederName;
                            queue.Enqueue((connectedAsset, nextNode, currentAsset));
                        }
                        //if node is already visited but the feedername is empty for the asset, the case when last asset for closed circuit or mesh connections.
                        else if (!connectedAsset.IsTraced) //string.IsNullOrEmpty(connectedAsset.FeederName))
                        {
                            connectedAsset.FeederName = feederName;
                            connectedAsset.Parent = currentAsset;
                            feederAssets.Add(connectedAsset);
                            //queue.Enqueue((connectedAsset, nextNode, currentAsset));
                        }
                    }
                    //if the next node is not there, and its a dead end, the case for single ended devices like load, capacitor etc.
                    else if (nextNode == null)
                    {
                        if (!feederAssets.Contains(connectedAsset))
                        {
                            connectedAsset.FeederName = feederName;
                            connectedAsset.Parent = currentAsset;
                            feederAssets.Add(connectedAsset);
                        }
                    }
                }
            }

            //UpdateAssets(feederAssets);

            st.Stop();
            Console.WriteLine($"Feeder tracing complete in total time {st.ElapsedMilliseconds} ms. " +
                              $"Asset:{startingAsset.AssetType}_{startingAsset.AssetId}, Node:{startingNode.Name}, Count:{feederAssets?.Count()} Feeder:{feederName}");
            return feederAssets;
        }

        // Method to trace up the hierarchy
        public List<Asset> TraceUp(string assetId, bool tillFeederHeadOnly = true)
        {
            var result = new List<Asset>();
            if (!allAssetsDict.ContainsKey(assetId)) return result;

            var currentAsset = allAssetsDict[assetId];
            result.Add(currentAsset);

            while (currentAsset.Parent != null)
            {
                if (tillFeederHeadOnly && currentAsset.IsFeederHead)
                    break;
                currentAsset = currentAsset.Parent;
                result.Add(currentAsset);
            }

            return result;
        }

        public List<Asset> TraceDn(string assetId)
        {
            var result = new List<Asset>();
            if (!allAssetsDict.ContainsKey(assetId)) return result;

            result.Add(allAssetsDict[assetId]); //asset in the argument

            var queue = new Queue<Asset>();
            var visitedAssets = new HashSet<string>();

            queue.Enqueue(allAssetsDict[assetId]);
            visitedAssets.Add(assetId);

            while (queue.Count > 0)
            {
                var currentAsset = queue.Dequeue();
                if (currentAsset.NodeSideTo != null)
                    foreach (var connectedAsset in currentAsset.NodeSideTo?.ConnectedAssets)
                    {
                        // Check if the connected asset is a child and has not been visited
                        if (
                            connectedAsset.Parent?.AssetId == currentAsset.AssetId
                            //connectedAsset.NodeSideFrom?.Name == currentAsset.NodeSideTo?.Name
                            && !visitedAssets.Contains(connectedAsset.AssetId))
                        {
                            result.Add(connectedAsset);
                            queue.Enqueue(connectedAsset);
                            visitedAssets.Add(connectedAsset.AssetId);
                        }
                    }
            }

            return result;
        }

        //private void UpdateAssets(List<Asset> tracedAssets)
        //{
        //    foreach (var tracedAsset in tracedAssets)
        //    {
        //        if (allAssetsDict.ContainsKey(tracedAsset.AssetId))
        //        {
        //            var existingAsset = allAssetsDict[tracedAsset.AssetId];
        //            existingAsset.FeederName = tracedAsset.FeederName;
        //            existingAsset.Parent = tracedAsset.Parent;
        //            existingAsset.NodeSideFrom = tracedAsset.NodeSideFrom;
        //            existingAsset.NodeSideTo = tracedAsset.NodeSideTo;
        //        }
        //    }
        //}

        public void AllocateStates()
        {
            Dictionary<string, bool?> allstatedict = new();

            var allStates = this.StateVariables.SwitchStates;
            foreach (var eachState in allStates)
            {
                var assettype = eachState.AssetType;
                var assetid = eachState.SwitchId;
                var assetunits = eachState.SwitchStatusByUnits;

                var numphases = assetunits.Count;
                //if all are OPEN either 1 or 3 units
                if (assetunits.All(x => x.Value == "OPEN"))
                    allstatedict[assetid] = false;
                //if all are CLSOE either 1 or 3 units
                else if (assetunits.All(x => x.Value == "CLOSED"))
                    allstatedict[assetid] = true;
                else
                    //mismatch condition, did not occured during test
                    allstatedict[assetid] = null;
            }

            foreach (var asset in allAssets)
            {
                if (allstatedict.TryGetValue(asset.AssetId, out var switchstate))
                {
                    asset.SwitchStatus = switchstate;
                }
            }
        }

        public void SwitchSimulateOpen(string switchID)
        {
            var myswitch = this.StateVariables.SwitchStates.FirstOrDefault(x => x.SwitchId == switchID);
            if (myswitch != null)
                foreach (var myswitchunitid in myswitch.SwitchStatusByUnits.Keys)
                    myswitch.SwitchStatusByUnits[myswitchunitid] = "OPEN";
        }

        public void SwitchSimulateClose(string switchID)
        {
            var myswitch = this.StateVariables.SwitchStates.FirstOrDefault(x => x.SwitchId == switchID);
            if (myswitch != null)
                foreach (var myswitchunitid in myswitch.SwitchStatusByUnits.Keys)
                    myswitch.SwitchStatusByUnits[myswitchunitid] = "CLOSED";
        }
    }

    public partial class ElectricGridModelAdditionalInfo
    {
        [J("grid_model_version")] public string GridModelVersion { get; set; }
        [J("planner_kva_ratings")] public List<PlannerKvaRating> PlannerKvaRatings { get; set; }
        [J("run_date")] public long RunDate { get; set; }
        [J("run_id")] public DateTimeOffset RunId { get; set; }
        [J("station_internals")] public StationInternals StationInternals { get; set; }
    }

    public partial class PlannerKvaRating
    {
        [J("entity_id")] public string EntityId { get; set; }
        [J("entity_type")] public string EntityType { get; set; }
        [J("summer")] public long Summer { get; set; }
        [J("winter")] public long Winter { get; set; }
    }

    public partial class StationInternals
    {
        [J("has_internal_data")] public bool HasInternalData { get; set; }
        [J("reason")] public string Reason { get; set; }
    }


    #region asset_service_states

    public partial class AssetServiceState
    {
        [J("asset_id")] public string AssetId { get; set; }
        [J("asset_type")] public AssetType AssetType { get; set; }
        [J("service_status")] public ServiceStatus ServiceStatus { get; set; }
    }

    #endregion

    #region asset_specs

    public partial class AssetSpec
    {
        /// <summary>
        /// Field used to extend electric grid model, can contain any data required by applications using the model such as user interfaces.
        /// </summary>
        [J("additional_info")]
        public AssetSpecAdditionalInfo AdditionalInfo { get; set; }
    }

    public partial class AssetSpecs
    {
        [JsonProperty("battery_specs")]
        public List<BatterySpec> BatterySpecs { get; set; }

        [JsonProperty("cable_specs")]
        public List<CableSpec> CableSpecs { get; set; }

        [JsonProperty("capacitor_specs")]
        public List<CapacitorSpec> CapacitorSpecs { get; set; }

        [JsonProperty("conductor_specs")]
        public List<ConductorSpec> ConductorSpecs { get; set; }

        [JsonProperty("conductor_impedance_specs")]
        public List<ConductorImpedanceSpec> ConductorImpedanceSpecs { get; set; }

        [JsonProperty("inverter_specs")]
        public List<object> InverterSpecs { get; set; }

        [JsonProperty("load_specs")]
        public List<LoadSpec> LoadSpecs { get; set; }

        [JsonProperty("ltc_specs")]
        public List<LtcSpec> LtcSpecs { get; set; }

        [JsonProperty("regulator_specs")]
        public List<RegulatorSpec> RegulatorSpecs { get; set; }

        [JsonProperty("solar_pv_specs")]
        public List<SolarPVSpec> SolarPVSpecs { get; set; }

        [JsonProperty("source_specs")]
        public List<SourceSpec> SourceSpecs { get; set; }

        [JsonProperty("switch_specs")]
        public List<SwitchSpec> SwitchSpecs { get; set; }

        [JsonProperty("transformer_specs")]
        public List<TransformerSpec> TransformerSpecs { get; set; }

        [JsonProperty("wire_specs")]
        public List<WireSpec> WireSpecs { get; set; }
    }

    public partial class AssetSpecAdditionalInfo
    {
        [J("summer_emergency_rating")] public double? SummerEmergencyRating { get; set; }
        [J("summer_rating")] public double? SummerRating { get; set; }
        [J("winter_emergency_rating")] public double? WinterEmergencyRating { get; set; }
        [J("winter_rating")] public double? WinterRating { get; set; }
        [J("off_setting")] public long? OffSetting { get; set; }
        [J("on_setting")] public long? OnSetting { get; set; }
        [J("continuous_amp_rating")] public double? ContinuousAmpRating { get; set; }
        [J("ct_primary_rating")] public long? CtPrimaryRating { get; set; }
        [J("ct_secondary_rating")] public double? CtSecondaryRating { get; set; }
    }

    public partial class Battery : Asset
    {
        [J("additional_info")] public BatteryAdditionalInfo AdditionalInfo { get; set; }
        [J("battery_spec_id")] public string BatterySpecId { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("phase_configuration")] public PhaseConfiguration PhaseConfiguration { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
    }

    public partial class BatteryAdditionalInfo
    {

    }

    public partial class BatterySpec : AssetSpec
    {
        [J("battery_spec_id")] public string BatterySpecId { get; set; }
        [J("charging_rate_kw_per_hour")] public double? ChargingRateKwPerHour { get; set; }
        [J("discharging_rate_kw_per_hour")] public double? DischargingRateKwPerHour { get; set; }
        [J("kva_rating")] public double? KvaRating { get; set; }
        [J("kwh_rating")] public double? KwhRating { get; set; }
        [J("voltage_rating_kvll")] public double? VoltageRatingKvll { get; set; }
    }

    public partial class CableSpec : AssetSpec
    {
        [J("cable_spec_id")] public string CableSpecId { get; set; }
        [J("cn_diameter_in")] public double? CnDiameterIn { get; set; }
        [J("cn_stranding")] public string CnStranding { get; set; }
        [J("continuous_amp_rating")] public double? ContinuousAmpRating { get; set; }
        [J("gmr_cn_or_ts_in")] public double? GmrCnOrTsIn { get; set; }
        [J("gmr_neutral_in")] public double? GmrNeutralIn { get; set; }
        [J("gmr_phase_in")] public double? GmrPhaseIn { get; set; }
        [J("insulation_thickness_mils")] public double? InsulationThicknessMils { get; set; }
        [J("material_cn_or_ts")] public string MaterialCnOrTs { get; set; }
        [J("material_neutral")] public string MaterialNeutral { get; set; }
        [J("material_phase")] public string MaterialPhase { get; set; }
        [J("outside_diameter_in")] public double? OutsideDiameterIn { get; set; }
        [J("rac_cn_or_ts_ohm_per_mile")] public double? RacCnOrTsOhmPerMile { get; set; }
        [J("rac_neutral_ohm_per_mile")] public double? RacNeutralOhmPerMile { get; set; }
        [J("rac_phase_ohm_per_mile")] public double? RacPhaseOhmPerMile { get; set; }
        [J("size_cn_or_ts")] public double? SizeCnOrTs { get; set; }
        [J("size_neutral")] public string SizeNeutral { get; set; }
        [J("size_phase")] public string SizePhase { get; set; }
        [J("stranding_cn_or_ts")] public double? StrandingCnOrTs { get; set; }
        [J("stranding_neutral")] public string StrandingNeutral { get; set; }
        [J("stranding_phase")] public double? StrandingPhase { get; set; }
    }

    public partial class CapacitorSpec : AssetSpec
    {
        [J("capacitor_spec_id")] public string CapacitorSpecId { get; set; }
        [J("control_type")] public CapacitorControlType ControlType { get; set; }
        [J("ct_ratio")] public double? CtRatio { get; set; }
        [J("dead_time_seconds")] public double? DeadTimeSeconds { get; set; }
        [J("kvar_rating")] public double? KvarRating { get; set; }
        [J("off_delay_seconds")] public double? OffDelaySeconds { get; set; }
        [J("off_setting")] public double? OffSetting { get; set; }
        [J("on_delay_seconds")] public double? OnDelaySeconds { get; set; }
        [J("on_setting")] public double? OnSetting { get; set; }
        [J("pt_ratio")] public double? PtRatio { get; set; }
        [J("vmax")] public double? Vmax { get; set; }
        [J("vmin")] public double? Vmin { get; set; }
        [J("voltage_override")] public bool? VoltageOverride { get; set; }
        [J("voltage_rating_kvll")] public double? VoltageRatingKvll { get; set; }
    }

    public partial class ConductorSpec : AssetSpec
    {
        [J("conductor_arrangement_entries")]
        public List<ConductorArrangementEntry> ConductorArrangementEntries { get; set; }

        [J("conductor_spec_id")] public string ConductorSpecId { get; set; }
        [J("neutral_conductor_present")] public bool? NeutralConductorPresent { get; set; }
        [J("wires_arrangement")] public string WiresArrangement { get; set; }
    }

    public partial class ConductorArrangementEntry
    {
        [J("conductor_index")] public long ConductorIndex { get; set; }
        [J("conductor_spec_type")] public ConductorSpecType ConductorSpecType { get; set; }
        [J("horizontal_spacing_ft")] public double HorizontalSpacingFt { get; set; }
        [J("vertical_spacing_ft")] public double VerticalSpacingFt { get; set; }
        [J("wire_or_cable_spec_id")] public string WireOrCableSpecId { get; set; }
    }

    public partial class ConductorImpedanceSpec : AssetSpec
    {
        [J("conductor_spec_id")] public string ConductorSpecId { get; set; }
        [J("conductor_material")] public string ConductorMaterial { get; set; }
        [J("conductor_size")] public string ConductorSize { get; set; }
        [J("conductor_type")] public ConductorTypeEnum? ConductorType { get; set; }
        [J("rmatrix_ohms_per_mile")] public List<double> RmatrixOhmsPerMile { get; set; }
        [J("xmatrix_ohms_per_mile")] public List<double> XmatrixOhmsPerMile { get; set; }
        [J("bmatrix_microsiemens_per_mile")] public List<double> BmatrixMicrosiemensPerMile { get; set; }
        [J("continuous_amp_rating")] public double? ContinuousAmpRating { get; set; }
    }

    public partial class LoadSpec : AssetSpec
    {
        [J("load_spec_id")] public string LoadSpecId { get; set; }
        [J("load_type")] public LoadType LoadType { get; set; }
        [J("p_constant_current_percent")] public double? PConstantCurrentPercent { get; set; }
        [J("p_constant_impedance_percent")] public double? PConstantImpedancePercent { get; set; }
        [J("p_constant_power_percent")] public double? PConstantPowerPercent { get; set; }
        [J("q_constant_current_percent")] public double? QConstantCurrentPercent { get; set; }
        [J("q_constant_impedance_percent")] public double? QConstantImpedancePercent { get; set; }
        [J("q_constant_power_percent")] public double? QConstantPowerPercent { get; set; }
        [J("vz_max_percent")] public double? VzMaxPercent { get; set; }
        [J("vz_min_percent")] public double? VzMinPercent { get; set; }
    }

    public partial class LtcSpec : AssetSpec
    {
        [J("ltc_spec_id")] public string LtcSpecId { get; set; }
        [J("ct_ratio")] public double? CtRatio { get; set; }
        [J("pt_ratio")] public double? PtRatio { get; set; }
        [J("ltc_lower_bandwidth")] public double? LtcLowerBandwidth { get; set; }
        [J("ltc_upper_bandwidth")] public double? LtcUpperBandwidth { get; set; }
        [J("ltc_max_limit")] public double? LtcMaxLimit { get; set; }
        [J("ltc_min_limit")] public double? LtcMinLimit { get; set; }
        [J("ltc_num_of_steps")] public int? LtcNumOfSteps { get; set; }
    }

    public partial class RegulatorSpec : AssetSpec
    {
        [J("bandwidth")] public double? Bandwidth { get; set; }
        [J("ct_ratio")] public double? CtRatio { get; set; }
        [J("pt_ratio")] public double? PtRatio { get; set; }
        [J("initial_delay_seconds")] public double? InitialDelaySeconds { get; set; }
        [J("kva_rating")] public double? KvaRating { get; set; }
        [J("line_drop_comp_r")] public double? LineDropCompR { get; set; }
        [J("line_drop_comp_x")] public double? LineDropCompX { get; set; }
        [J("max_boost")] public double? MaxBoost { get; set; }
        [J("max_buck")] public double? MaxBuck { get; set; }
        [J("num_of_taps")] public double? NumOfTaps { get; set; }
        [J("regulator_spec_id")] public string RegulatorSpecId { get; set; }
        [J("reversible")] public bool? Reversible { get; set; }
        [J("subsequent_delay_seconds")] public double? SubsequentDelaySeconds { get; set; }
        [J("voltage_rating_kvll")] public double? VoltageRatingKvll { get; set; }
        [J("first_house_high")] public double? FirstHouseHigh { get; set; }
        [J("first_house_low")] public double? FirstHouseLow { get; set; }
    }

    public partial class SolarPVSpec : AssetSpec
    {
        [J("cell_dimension")] public string CellDimension { get; set; }
        [J("max_power_output_kw")] public double? MaxPowerOutputKw { get; set; }
        [J("module_efficiency")] public double? ModuleEfficiency { get; set; }
        [J("num_of_cells")] public double? NumOfCells { get; set; }
        [J("panel_height_mtr")] public double? PanelHeightMtr { get; set; }
        [J("panel_length_mtr")] public double? PanelLengthMtr { get; set; }
        [J("panel_weight_kg")] public double? PanelWeightKg { get; set; }
        [J("panel_width_mtr")] public double? PanelWidthMtr { get; set; }
        [J("solar_pv_spec_id")] public string SolarPvSpecId { get; set; }
        [J("voltage_rating_kvll")] public double? VoltageRatingKvll { get; set; }
    }

    public partial class SourceSpec : AssetSpec
    {
        [J("single_phase_short_circuit_mva")] public double? SinglePhaseShortCircuitMva { get; set; }
        [J("source_spec_id")] public string SourceSpecId { get; set; }
        [J("three_phase_short_circuit_mva")] public double? ThreePhaseShortCircuitMva { get; set; }
        [J("x0r0")] public double? X0R0 { get; set; }
        [J("x1r1")] public double? X1R1 { get; set; }
        [J("r0_ohms")] public double? R0Ohms { get; set; }
        [J("r1_ohms")] public double? R1Ohms { get; set; }

        [J("x0_ohms")] public double? X0Ohms { get; set; }
        [J("x1_ohms")] public double? X1Ohms { get; set; }
    }

    public partial class SwitchSpec : AssetSpec
    {
        [J("asset_type")] public AssetType AssetType { get; set; }
        [J("continuous_amp_rating")] public double? ContinuousAmpRating { get; set; }
        [J("interrupting_amp_rating")] public double? InterruptingAmpRating { get; set; }
        [J("is_ganged")] public bool? IsGanged { get; set; }
        [J("operation_mode")] public OperationMode OperationMode { get; set; }
        [J("switching_device_spec_id")] public string SwitchingDeviceSpecId { get; set; }
    }

    public partial class TransformerSpec : AssetSpec
    {
        [J("first_loading_limit_kva")] public double? FirstLoadingLimitKva { get; set; }
        [J("full_load_losses_kw")] public double? FullLoadLossesKw { get; set; }
        [J("no_load_losses_kw")] public double? NoLoadLossesKw { get; set; }
        [J("nominal_rating_kva")] public double? NominalRatingKva { get; set; }
        [J("phase_configurations")] public List<PhaseConfiguration> PhaseConfigurations { get; set; }
        [J("phase_shift_degrees")] public double? PhaseShiftDegrees { get; set; }
        [J("r_ground")] public List<long> RGround { get; set; }
        [J("reversible")] public bool Reversible { get; set; }
        [J("second_loading_limit_kva")] public double? SecondLoadingLimitKva { get; set; }
        [J("short_circuit_reactance_percent")] public double? ShortCircuitReactancePercent { get; set; }

        [J("short_circuit_resistance_percent")]
        public double? ShortCircuitResistancePercent { get; set; }

        [J("transformer_spec_id")] public string TransformerSpecId { get; set; }
        [J("voltage_ratings_kvll")] public List<double> VoltageRatingsKvll { get; set; }
        [J("x_ground")] public List<long> XGround { get; set; }
    }

    public partial class WireSpec : AssetSpec
    {
        [J("continuous_amp_rating")] public double? ContinuousAmpRating { get; set; }
        [J("wire_diameter_in")] public double? WireDiameterIn { get; set; }
        [J("wire_gmr_in")] public double? WireGmrIn { get; set; }
        [J("wire_material")] public string WireMaterial { get; set; }
        [J("wire_rac_ohm_per_mile")] public double? WireRacOhmPerMile { get; set; }
        [J("wire_size")] public string WireSize { get; set; }
        [J("wire_spec_id")] public string WireSpecId { get; set; }
        [J("wire_stranding")] public string WireStranding { get; set; }
    }

    #endregion

    #region assets

    #endregion

    #region geo_coordinate

    #endregion

    #region asset_group

    #endregion

    #region source_node_entry

    #endregion

    #region state_variables

    #endregion

    public abstract class Asset
    {
        public string FeederName { get; set; }
        public Asset Parent { get; set; }
        public Node NodeObject1 { get; set; }
        public Node NodeObject2 { get; set; }
        public Node NodeSideFrom { get; set; }
        public Node NodeSideTo { get; set; }
        public bool? SwitchStatus { get; set; } // Assume this is properly defined somewhere in your class
        public bool IsOpenSwitch()
        {
            return (AssetType == AssetType.SWITCH || AssetType == AssetType.CIRCUIT_BREAKER ||
                    AssetType == AssetType.FUSE || AssetType == AssetType.ELBOW ||
                    AssetType == AssetType.RECLOSER || AssetType == AssetType.SECTIONALIZER) && SwitchStatus == false;
        }
        public bool IsFeederHead { get; set; }
        public bool IsSingleNode => NodeList.Count() == 1;
        public bool IsIsolated => ConnectedAssets.Count() == 0;
        public bool IsTraced => this.Parent != null || !string.IsNullOrEmpty(this.FeederName);
        //public bool IsTracedAbnormal => this.Parent?.NodeSideTo != this.NodeSideFrom;
        //public bool IsInSubstation { get; set; }
        // used for HashSet Optimization
        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            Asset other = (Asset)obj;
            return AssetType == other.AssetType && AssetId == other.AssetId;
        }
        // used for HashSet Optimization
        public override int GetHashCode() => HashCode.Combine(AssetType, AssetId);


        [J("asset_id")] public string AssetId { get; set; }
        [J("asset_type")] public AssetType AssetType { get; set; }
        [J("node_list")] public List<string> NodeList { get; set; } = new List<string>();
        //public HashSet<string> NodeList { get; set; }
        [J("phasing")] public Phasing Phasing { get; set; }
        [J("groupings")] public Groupings Groupings { get; set; }

        public HashSet<Asset> ConnectedAssets { get; set; } = new HashSet<Asset>();
        public GeoJson GeoJson { get; set; }

        public List<double?> GeoCoordinateNode1 { get; set; } //only for line type of assets
        public List<double?> GeoCoordinateNode2 { get; set; } //only for line type of assets

        protected Asset()
        {
            ConnectedAssets = new HashSet<Asset>();
        }

        public void ConnectAsset(Asset asset)
        {
            ConnectedAssets.Add(asset);
            asset.ConnectedAssets.Add(this);
        }

        public static Dictionary<Node, List<Asset>> BuildNodeAssetsMap(IEnumerable<Asset> allAssets)
        {
            var nodeAssetsMap = new Dictionary<Node, List<Asset>>();
            var nodeLookup = new Dictionary<string, Node>();

            foreach (var asset in allAssets)
            {
                foreach (var nodeName in asset.NodeList)
                {
                    if (nodeName == null) continue;

                    if (!nodeLookup.ContainsKey(nodeName))
                    {
                        var node = new Node(nodeName);
                        nodeLookup[nodeName] = node;
                        nodeAssetsMap[node] = new List<Asset>();
                    }

                    nodeAssetsMap[nodeLookup[nodeName]].Add(asset);
                }
            }

            return nodeAssetsMap;
        }

        public static void AssignNodeReferences(Dictionary<Node, List<Asset>> nodeAssetsMap, IEnumerable<Asset> assets)
        {
            var nodeLookup = nodeAssetsMap.Keys.ToDictionary(n => n.Name, n => n);

            foreach (var asset in assets)
            {
                if (asset.NodeList.Count > 0)
                    asset.NodeObject1 = nodeLookup.ContainsKey(asset.NodeList[0]) ? nodeLookup[asset.NodeList[0]] : null;
                if (asset.NodeList.Count > 1)
                    asset.NodeObject2 = nodeLookup.ContainsKey(asset.NodeList[1]) ? nodeLookup[asset.NodeList[1]] : null;
            }

            foreach (var kvp in nodeAssetsMap)
            {
                var node = kvp.Key;
                var connectedAssets = kvp.Value;

                foreach (var asset in connectedAssets)
                {
                    if (asset.NodeObject1 == node || asset.NodeObject2 == node)
                        node.ConnectedAssets.Add(asset);
                }
            }

            // Populate ConnectedAssets for each Asset
            foreach (var asset in assets)
            {
                asset.ConnectedAssets.Clear(); // Clear existing connected assets

                foreach (var connectedAsset in asset.NodeObject1?.ConnectedAssets ?? Enumerable.Empty<Asset>())
                    if (connectedAsset != asset)
                        asset.ConnectedAssets.Add(connectedAsset);

                foreach (var connectedAsset in asset.NodeObject2?.ConnectedAssets ?? Enumerable.Empty<Asset>())
                    if (connectedAsset != asset)
                        asset.ConnectedAssets.Add(connectedAsset);
            }
        }

        public GeoJson GetGeoJson(List<GeoCoordinate> geoCoordinates)
        {
            var geoCoordinate = geoCoordinates.FirstOrDefault(gc => gc.ObjectId == AssetId);
            return geoCoordinate?.Geojson;
        }

        public static IEnumerable<(Asset, Asset, string)> GetUniqueConnections(
            Dictionary<string, List<Asset>> nodeAssetsMap)
        {
            var connections = new HashSet<(Asset, Asset, string)>();

            foreach (var kvp in nodeAssetsMap)
            {
                var node = kvp.Key;
                var assets = kvp.Value;
                if (assets.Count > 1)
                {

                    for (int i = 0; i < assets.Count; i++)
                    {
                        for (int j = i + 1; j < assets.Count; j++)
                        {
                            var a1 = assets[i];
                            var a2 = assets[j];

                            if (a1.NodeList?.Count == 1 && a2.NodeList?.Count == 1)
                                continue;

                            var connection = (a1, a2, node);
                            if (string.CompareOrdinal(connection.Item1.AssetId, connection.Item2.AssetId) < 0)
                                connections.Add(connection);
                            else
                                connections.Add((connection.Item2, connection.Item1, node));
                        }
                    }

                }
            }

            return connections;
        }
    }

    public partial class Assets
    {
        [J("capacitors")] public List<Capacitor> Capacitors { get; set; }
        [J("circuit_breakers")] public List<CircuitBreaker> CircuitBreakers { get; set; }
        [J("distributed_generators")] public List<DistributedGenerator> DistributedGenerators { get; set; }
        [J("elbows")] public List<Elbow> Elbows { get; set; }
        [J("electric_batteries")] public List<Battery> ElectricBatteries { get; set; }
        [J("fuses")] public List<Fuse> Fuses { get; set; }
        [J("loads")] public List<Load> Loads { get; set; }
        [J("overhead_primary_conductors")] public List<OverheadPrimaryConductor> OverheadPrimaryConductors { get; set; }
        [J("overhead_secondary_conductors")] public List<object> OverheadSecondaryConductors { get; set; }
        [J("reclosers")] public List<Recloser> Reclosers { get; set; }
        [J("regulators")] public List<Regulator> Regulators { get; set; }
        [J("sectionalizers")] public List<Sectionalizer> Sectionalizers { get; set; }
        [J("sources")] public List<Source> Sources { get; set; }
        [J("switches")] public List<Switch> Switches { get; set; }
        [J("tie_switches")] public List<TieSwitch> TieSwitches { get; set; }
        [J("transformers")] public List<Transformer> Transformers { get; set; }

        [J("underground_primary_conductors")]
        public List<UndergroundPrimaryConductor> UndergroundPrimaryConductors { get; set; }

        [J("underground_secondary_conductors")]
        public List<object> UndergroundSecondaryConductors { get; set; }

        internal IEnumerable<Asset> GetAllAssets()
        {
            return new List<Asset>()
                    .Concat(Capacitors)
                    .Concat(CircuitBreakers)
                    .Concat(DistributedGenerators)
                    .Concat(Elbows)
                    .Concat(ElectricBatteries)
                    .Concat(Fuses)
                    .Concat(Loads)
                    .Concat(OverheadPrimaryConductors)
                    //.Concat(OverheadSecondaryConductors)
                    .Concat(Reclosers)
                    .Concat(Regulators)
                    .Concat(Sectionalizers)
                    .Concat(Sources)
                    .Concat(Switches)
                    .Concat(Transformers)
                    .Concat(UndergroundPrimaryConductors)
                    //.Concat(UndergroundSecondaryConductors)
                    ;
        }
    }

    public partial class Capacitor : Asset
    {
        [J("additional_info")] public CapacitorAdditionalInfo AdditionalInfo { get; set; }
        [J("capacitor_spec_id")] public string CapacitorSpecId { get; set; }
        [J("capacitor_unit_list")] public List<CapacitorUnitList> CapacitorUnitList { get; set; }
        [J("monitored_asset")] public string MonitoredAsset { get; set; }
        [J("monitored_phase")] public MonitoredPhase MonitoredPhase { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("phase_configuration")] public PhaseConfiguration PhaseConfiguration { get; set; }
        [J("total_kvar")] public long TotalKvar { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
    }

    public partial class CapacitorAdditionalInfo
    {
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }

        [JsonConverter(typeof(SubPlotConverter))]
        [J("sub_plot")]
        public dynamic SubPlot { get; set; }

        [J("subeditor_name")] public string SubeditorName { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
        [J("pi_enabled")] public bool? PiEnabled { get; set; }
        [J("sensing_phase")] public Phasing? SensingPhase { get; set; }
        [J("original_nodes")] public List<string> OriginalNodes { get; set; }
    }

    public partial class ProtectionDevice
    {
        [J("asset_id")] public string AssetId { get; set; }
        [J("asset_type")] public AssetType AssetType { get; set; }
        [J("phases_protected")] public List<Phasing> PhasesProtected { get; set; }
    }

    [JsonConverter(typeof(SubPlotConverter))]
    public partial class SubPlot
    {
        [J("x")] public long? X { get; set; }
        [J("y")] public long? Y { get; set; }
    }

    public partial class PurpleUnitsInfo
    {
        [J("gis_phase")] public GisPhase GisPhase { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
    }

    public partial class CapacitorUnitList
    {
        [J("capacitor_unit_id")] public string CapacitorUnitId { get; set; }
        [J("phasing")] public Phasing Phasing { get; set; }
        [J("unit_kvar")] public long UnitKvar { get; set; }
    }

    public partial class Groupings
    {
        [J("circuit_id")] public string CircuitId { get; set; }
        [J("county_name")] public string CountyName { get; set; }
        [J("distribution_op_center")] public string DistributionOpCenter { get; set; }
        [J("distribution_region")] public string DistributionRegion { get; set; }
        [J("distribution_zone")] public string DistributionZone { get; set; }
        [J("state_name")] public string StateName { get; set; }
        [J("substation_id")] public string SubstationId { get; set; }
        [J("substation_name")] public string SubstationName { get; set; }
        [J("circuit_name")] public string CircuitName { get; set; }
    }

    public partial class CircuitBreaker : Asset
    {
        [J("additional_info")] public CircuitBreakerAdditionalInfo AdditionalInfo { get; set; }
        [J("breaker_spec_id")] public string BreakerSpecId { get; set; }
        [J("breaker_unit_list")] public List<UnitList> BreakerUnitList { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
    }

    public partial class CircuitBreakerAdditionalInfo
    {
        [J("downstream_customers")] public long DownstreamCustomers { get; set; }
        [J("external_interface_member")] public bool ExternalInterfaceMember { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("normal_status")] public NormalStatus NormalStatus { get; set; }

        [JsonConverter(typeof(SubPlotConverter))]
        [J("sub_plot")]
        public dynamic SubPlot { get; set; }

        [J("subeditor_name")] public string SubeditorName { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
        [J("pi_enabled")] public bool? PiEnabled { get; set; }
    }

    public partial class UnitList
    {
        [J("breaker_unit_id")] public string BreakerUnitId { get; set; }
        [J("continuous_amp_rating")] public long ContinuousAmpRating { get; set; }
        [J("normal_status")] public NormalStatus NormalStatus { get; set; }
        [J("phasing")] public Phasing Phasing { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
        [J("elbow_unit_id")] public string ElbowUnitId { get; set; }
        [J("fuse_unit_id")] public string FuseUnitId { get; set; }
        [J("recloser_unit_id")] public string RecloserUnitId { get; set; }
        [J("sectionalizer_unit_id")] public string SectionalizerUnitId { get; set; }
        [J("switch_unit_id")] public string SwitchUnitId { get; set; }
    }

    public partial class DistributedGenerator : Asset
    {
        [J("additional_info")] public DistributedGeneratorAdditionalInfo AdditionalInfo { get; set; }
        [J("generator_spec_id")] public string GeneratorSpecId { get; set; }
        [J("generator_type")] public GeneratorType GeneratorType { get; set; }
        [J("inverter_spec_id")] public string InverterSpecId { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("phase_configuration")] public PhaseConfiguration PhaseConfiguration { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
    }

    public partial class DistributedGeneratorAdditionalInfo
    {
        [J("customer_type")] public string CustomerType { get; set; }
        [J("generation_meter_number")] public string GenerationMeterNumber { get; set; }
        [J("gis_id")] public string GisId { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("KW")] public double Kw { get; set; }
        [J("ppa_type")] public string PpaType { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("recloser_id")] public object RecloserId { get; set; }
        [J("service_point_id")] public long ServicePointId { get; set; }
        [J("source_id")] public long SourceId { get; set; }
        [J("primary_meter")] public PrimaryMeter PrimaryMeter { get; set; }
    }

    public partial class PrimaryMeter
    {
        [J("primary_meter_id")] public string PrimaryMeterId { get; set; }
        [J("summer_peak_load")] public double SummerPeakLoad { get; set; }
        [J("winter_peak_load")] public double WinterPeakLoad { get; set; }
        [J("customer_count")] public long? CustomerCount { get; set; }
    }

    public partial class Elbow : Asset
    {
        [J("additional_info")] public ElbowAdditionalInfo AdditionalInfo { get; set; }
        [J("elbow_spec_id")] public string ElbowSpecId { get; set; }
        [J("elbow_unit_list")] public List<UnitList> ElbowUnitList { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
    }

    public partial class ElbowAdditionalInfo
    {
        [J("downstream_customers")] public long? DownstreamCustomers { get; set; }
        [J("gis_asset_type")] public GisAssetTypeEnum GisAssetType { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("normal_status")] public NormalStatusAdditional NormalStatus { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
    }

    public partial class Fuse : Asset
    {
        [J("additional_info")] public FuseAdditionalInfo AdditionalInfo { get; set; }
        [J("fuse_spec_id")] public string FuseSpecId { get; set; }
        [J("fuse_unit_list")] public List<UnitList> FuseUnitList { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
    }

    public partial class FuseAdditionalInfo
    {
        [J("downstream_customers")] public long? DownstreamCustomers { get; set; }
        [J("gis_asset_type")] public GisAssetTypeEnum GisAssetType { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("normal_status")] public NormalStatusAdditional NormalStatus { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("units_info")] public Dictionary<string, FluffyUnitsInfo> UnitsInfo { get; set; }
        [J("original_nodes")] public List<string> OriginalNodes { get; set; }
    }

    public partial class FluffyUnitsInfo
    {
        [J("gis_phase")] public GisPhase GisPhase { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("type")] public UnitsInfoType Type { get; set; }
        [J("bypass_status")] public BypassStatus? BypassStatus { get; set; }
    }

    public partial class Load : Asset
    {
        [J("additional_info")] public LoadAdditionalInfo AdditionalInfo { get; set; }
        [J("load_spec_id")] public string LoadSpecId { get; set; }
        [J("meters")] public List<string> Meters { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("num_of_customers")] public long NumOfCustomers { get; set; }
        [J("phase_configuration")] public PhaseConfiguration PhaseConfiguration { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
    }

    public partial class LoadAdditionalInfo
    {
        [J("ami_tech_type")] public Dictionary<string, AmiTechType> AmiTechType { get; set; }
        [J("generation_capable")] public bool? GenerationCapable { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("primary_meter")] public PrimaryMeter PrimaryMeter { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("transformer_id")] public string TransformerId { get; set; }
    }

    public partial class OverheadPrimaryConductor : Asset
    {
        [J("additional_info")] public OverheadPrimaryConductorAdditionalInfo AdditionalInfo { get; set; }
        [J("conductor_length_mile")] public double ConductorLengthMile { get; set; }
        [J("conductor_spec_id")] public string ConductorSpecId { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("num_of_conductors")] public long NumOfConductors { get; set; }
        [J("phase_configuration")] public MonitoredPhase PhaseConfiguration { get; set; }
        [J("phase_order")] public string PhaseOrder { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
    }

    public partial class OverheadPrimaryConductorAdditionalInfo
    {
        [J("downstream_customers")] public long DownstreamCustomers { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("original_nodes")] public List<string> OriginalNodes { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
    }

    public partial class Recloser : Asset
    {
        [J("additional_info")] public RecloserAdditionalInfo AdditionalInfo { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("recloser_spec_id")] public string RecloserSpecId { get; set; }
        [J("recloser_unit_list")] public List<UnitList> RecloserUnitList { get; set; }
    }

    public partial class RecloserAdditionalInfo
    {
        [J("control_type")] public ControlType ControlType { get; set; }
        [J("gis_asset_type")] public GisAssetTypeEnum GisAssetType { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("normal_status")] public NormalStatusAdditional NormalStatus { get; set; }
        [J("original_nodes")] public List<string> OriginalNodes { get; set; }
        [J("pi_enabled")] public bool? PiEnabled { get; set; }
        [J("units_info")] public Dictionary<string, FluffyUnitsInfo> UnitsInfo { get; set; }
        [J("downstream_customers")] public long? DownstreamCustomers { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
    }

    public partial class Regulator : Asset
    {
        [J("additional_info")] public RegulatorAdditionalInfo AdditionalInfo { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("phase_configuration")] public PhaseConfiguration PhaseConfiguration { get; set; }
        [J("regulator_unit_list")] public List<RegulatorUnitList> RegulatorUnitList { get; set; }
        [J("total_kva_rating")] public long TotalKvaRating { get; set; }
    }

    public partial class RegulatorAdditionalInfo
    {
        [J("downstream_customers")] public long? DownstreamCustomers { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("load_carrying_capacity")] public long LoadCarryingCapacity { get; set; }

        [JsonConverter(typeof(SubPlotConverter))]
        [J("sub_plot")]
        public dynamic SubPlot { get; set; }

        [J("subeditor_name")] public string SubeditorName { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
        [J("pi_enabled")] public bool? PiEnabled { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("parent_conductor")] public string ParentConductor { get; set; }
        [J("original_nodes")] public List<string> OriginalNodes { get; set; }
    }

    public partial class RegulatorUnitList
    {
        [J("first_house_protection_enabled")] public bool FirstHouseProtectionEnabled { get; set; }
        [J("kva_rating")] public long KvaRating { get; set; }
        [J("monitored_phase")] public MonitoredPhase MonitoredPhase { get; set; }
        [J("phasing")] public Phasing Phasing { get; set; }
        [J("regulator_spec_id")] public string RegulatorSpecId { get; set; }
        [J("regulator_unit_id")] public string RegulatorUnitId { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
    }

    public partial class Sectionalizer : Asset
    {
        [J("additional_info")] public SectionalizerAdditionalInfo AdditionalInfo { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("sectionalizer_spec_id")] public string SectionalizerSpecId { get; set; }
        [J("sectionalizer_unit_list")] public List<UnitList> SectionalizerUnitList { get; set; }
    }

    public partial class SectionalizerAdditionalInfo
    {
        [J("downstream_customers")] public long DownstreamCustomers { get; set; }
        [J("gis_asset_type")] public GisAssetTypeEnum GisAssetType { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("normal_status")] public NormalStatusAdditional NormalStatus { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("units_info")] public Dictionary<string, FluffyUnitsInfo> UnitsInfo { get; set; }
        [J("original_nodes")] public List<string> OriginalNodes { get; set; }
    }

    public partial class Source : Asset
    {
        [J("additional_info")] public SourceAdditionalInfo AdditionalInfo { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("phase_configuration")] public PhaseConfiguration PhaseConfiguration { get; set; }
        [J("source_spec_id")] public string SourceSpecId { get; set; }
        [J("voltage_rating_kvll")] public long VoltageRatingKvll { get; set; }
    }

    public partial class SourceAdditionalInfo
    {
        [J("downstream_customers")] public long DownstreamCustomers { get; set; }

        [JsonConverter(typeof(SubPlotConverter))]
        [J("sub_plot")]
        public dynamic SubPlot { get; set; }
    }

    public partial class Switch : Asset
    {
        [J("additional_info")] public SwitchAdditionalInfo AdditionalInfo { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("switch_spec_id")] public string SwitchSpecId { get; set; }
        [J("switch_unit_list")] public List<UnitList> SwitchUnitList { get; set; }
    }

    public partial class SwitchAdditionalInfo
    {
        [J("downstream_customers")] public long? DownstreamCustomers { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("normal_status")] public string NormalStatus { get; set; }

        [JsonConverter(typeof(SubPlotConverter))]
        [J("sub_plot")]
        public dynamic SubPlot { get; set; }

        [J("subeditor_name")] public string SubeditorName { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
        [J("gis_asset_type")] public GisAssetTypeEnum GisAssetType { get; set; }
        [J("sub_type")] public string SubType { get; set; }
    }

    /// <summary>
    /// A tie_switch is any type of a switch that interconnects two circuits.
    /// Recloser / Fuse / Elbow / Sectionalizer / Switch / Circuit Breaker
    /// </summary>
    public partial class TieSwitch //: Asset
    {
        [J("circuit_id1")] public string CircuitId1 { get; set; }
        [J("circuit_id2")] public string CircuitId2 { get; set; }
        [J("switch_id")] public string SwitchId { get; set; }
        [J("switch_type")] public SwitchTypeEnum SwitchType { get; set; }
    }

    public partial class Transformer : Asset
    {
        [J("additional_info")] public TransformerAdditionalInfo AdditionalInfo { get; set; }
        [J("mounting")] public string Mounting { get; set; }
        [J("total_kva_rating")] public double TotalKvaRating { get; set; }
        [J("transformer_unit_list")] public List<TransformerUnitList> TransformerUnitList { get; set; }
    }

    public partial class TransformerAdditionalInfo
    {
        [J("circuits_fed")] public List<string> CircuitsFed { get; set; }
        [J("control_capability")] public string ControlCapability { get; set; }
        [J("current_control_mode")] public string CurrentControlMode { get; set; }
        [J("downstream_customers")] public long DownstreamCustomers { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }

        [JsonConverter(typeof(SubPlotConverter))]
        [J("sub_plot")]
        public dynamic SubPlot { get; set; }

        [J("subeditor_name")] public string SubeditorName { get; set; }
        [J("transformer_type")] public string TransformerType { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
        [J("banked_indicator")] public string BankedIndicator { get; set; }
        [J("customer_count")] public long? CustomerCount { get; set; }
        [J("parallel_ind")] public string ParallelInd { get; set; }
        [J("percent_load")] public long? PercentLoad { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("summer_peak_load")] public double? SummerPeakLoad { get; set; }
        [J("winter_peak_load")] public double? WinterPeakLoad { get; set; }
    }

    public partial class TransformerUnitList
    {
        [J("kva_rating")] public double KvaRating { get; set; }
        [J("transformer_spec_id")] public string TransformerSpecId { get; set; }
        [J("transformer_unit_id")] public string TransformerUnitId { get; set; }
        [J("winding_list")] public List<WindingList> WindingList { get; set; }
    }

    public partial class WindingList
    {
        [J("grounded_neutral")] public bool GroundedNeutral { get; set; }
        [J("ltc_spec_id")] public string LtcSpecId { get; set; }
        [J("monitored_phase")] public MonitoredPhase MonitoredPhase { get; set; }
        [J("node_id")] public string NodeId { get; set; }
        [J("phase_configuration")] public PhaseConfiguration PhaseConfiguration { get; set; }
        [J("phasing")] public Phasing Phasing { get; set; }
        [J("transformer_winding_id")] public string TransformerWindingId { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
        [J("winding_type")] public WindingType WindingType { get; set; }
    }

    public partial class UndergroundPrimaryConductor : Asset
    {
        [J("additional_info")] public UndergroundPrimaryConductorAdditionalInfo AdditionalInfo { get; set; }
        [J("conductor_length_mile")] public double ConductorLengthMile { get; set; }
        [J("conductor_spec_id")] public string ConductorSpecId { get; set; }
        [J("mounting")] public Mounting Mounting { get; set; }
        [J("num_of_conductors")] public long NumOfConductors { get; set; }
        [J("phase_configuration")] public MonitoredPhase PhaseConfiguration { get; set; }
        [J("phase_order")] public Phasing PhaseOrder { get; set; }
        [J("voltage_rating_kvll")] public double VoltageRatingKvll { get; set; }
    }

    public partial class UndergroundPrimaryConductorAdditionalInfo
    {
        [J("downstream_customers")] public long DownstreamCustomers { get; set; }
        [J("gis_status")] public GisStatus GisStatus { get; set; }
        [J("protection_device")] public List<ProtectionDevice> ProtectionDevice { get; set; }
        [J("units_info")] public Dictionary<string, PurpleUnitsInfo> UnitsInfo { get; set; }
    }

    public partial class GeoCoordinate
    {
        [JsonConverter(typeof(GeoJsonConverter))]
        [J("geojson")]
        public GeoJson Geojson { get; set; }

        [J("object_id")] public string ObjectId { get; set; }
        [J("object_type")] public AssetType ObjectType { get; set; }
    }

    [JsonConverter(typeof(GeoJsonConverter))]
    public partial class GeoJson
    {
        [J("coordinates")] public dynamic Coordinates { get; set; }
        [J("type")] public GeoJsonType Type { get; set; }
    }

    public partial class Group
    {
        [J("additional_info")] public GroupAdditionalInfo AdditionalInfo { get; set; }
        [J("group_id")] public string GroupId { get; set; }
        [J("group_name")] public string GroupName { get; set; }
        [J("group_type")] public string GroupType { get; set; } //TODO: can be an enum
        [J("groupings")] public Groupings Groupings { get; set; }
    }

    public partial class GroupAdditionalInfo
    {
        [J("circuits")] public List<string> Circuits { get; set; }
        [J("substation_number")] public string SubstationNumber { get; set; }
        [J("gis_status")] public GisStatus? GisStatus { get; set; }
        [J("self_healing_indicator")] public string SelfHealingIndicator { get; set; }
        [J("voltage_rating_kvll")] public double? VoltageRatingKvll { get; set; }
    }

    public partial class StateVariables
    {
        [J("battery_control_settings")] public List<object> BatteryControlSettings { get; set; }
        [J("battery_values")] public List<BatteryValue> BatteryValues { get; set; }
        [J("capacitor_control_settings")] public List<CapacitorControlSetting> CapacitorControlSettings { get; set; }
        [J("capacitor_states")] public List<CapacitorState> CapacitorStates { get; set; }
        [J("flow_constraints")] public List<object> FlowConstraints { get; set; }
        [J("generator_values")] public List<GeneratorValue> GeneratorValues { get; set; }
        [J("inverter_values")] public List<object> InverterValues { get; set; }
        [J("load_values")] public List<LoadValue> LoadValues { get; set; }
        [J("ltc_control_settings")] public List<LtcControlSetting> LtcControlSettings { get; set; }
        [J("regulator_control_settings")] public List<RegulatorControlSetting> RegulatorControlSettings { get; set; }
        [J("statevar_timestamp")] public long StatevarTimestamp { get; set; }
        [J("switch_states")] public List<SwitchState> SwitchStates { get; set; }
    }

    public partial class BatteryValue
    {
        [J("battery_id")] public string BatteryId { get; set; }
        [J("battery_operation_mode")] public BatteryOperationMode BatteryOperationMode { get; set; }
        [J("pq_by_phase")] public PqByPhase PqByPhase { get; set; }
        [J("voltage_by_phase_kv")] public VoltageByPhaseKv VoltageByPhaseKv { get; set; }
    }

    public partial class CapacitorControlSetting
    {
        [J("capacitor_id")] public string CapacitorId { get; set; }
        [J("off_setting")] public long OffSetting { get; set; }
        [J("on_setting")] public long OnSetting { get; set; }
    }

    public partial class CapacitorState
    {
        [J("capacitor_id")] public string CapacitorId { get; set; }

        /// <summary>
        /// CAPACITOR_UNIT_ID, enum [OFF, ON], The key will be the actual capacitor unit id for each entry.
        /// </summary>
        [J("capacitor_status_by_units")]
        public Dictionary<string, string> CapacitorStatusByUnits { get; set; }
    }

    public partial class GeneratorValue
    {
        [J("generator_id")] public string GeneratorId { get; set; }
        [J("pq_by_phase")] public PqByPhase PqByPhase { get; set; }
        [J("voltage_by_phase_kv")] public VoltageByPhaseKv VoltageByPhaseKv { get; set; }
    }

    public partial class PqByPhase
    {
        [J("A")] public PQ A { get; set; }
        [J("B")] public PQ B { get; set; }
        [J("C")] public PQ C { get; set; }
    }

    public partial class PQ
    {
        [J("kvar")] public double Kvar { get; set; }
        [J("kw")] public double Kw { get; set; }
    }

    public partial class VoltageByPhaseKv
    {
        [J("A")] public double? A { get; set; }
        [J("B")] public double? B { get; set; }
        [J("C")] public double? C { get; set; }
    }

    public partial class LoadValue
    {
        [J("load_id")] public string LoadId { get; set; }
        [J("pq_by_phase")] public PqByPhase PqByPhase { get; set; }
        [J("voltage_by_phase_kv")] public VoltageByPhaseKv VoltageByPhaseKv { get; set; }
    }

    public partial class LtcControlSetting
    {
        [J("ltc_unit_control_settings")] public List<object> LtcUnitControlSettings { get; set; }
        [J("transformer_id")] public string TransformerId { get; set; }
    }

    public partial class RegulatorControlSetting
    {
        [J("regulator_id")] public string RegulatorId { get; set; }

        [J("regulator_unit_control_settings")]
        public List<RegulatorUnitControlSetting> RegulatorUnitControlSettings { get; set; }
    }

    public partial class RegulatorUnitControlSetting
    {
        [J("regulator_unit_id")] public string RegulatorUnitId { get; set; }
        [J("tap_position")] public long TapPosition { get; set; }
        [J("voltage_set_point")] public long VoltageSetPoint { get; set; }
    }

    public partial class SwitchState
    {
        [J("asset_type")] public AssetType AssetType { get; set; }
        [J("switch_id")] public string SwitchId { get; set; }
        [J("switch_status_by_units")] public Dictionary<string, string> SwitchStatusByUnits { get; set; }
    }

    public partial class TsStateVariables
    {
        [J("ts_battery_values")] public List<object> TsBatteryValues { get; set; }
        [J("ts_capacitor_states")] public List<object> TsCapacitorStates { get; set; }
        [J("ts_description")] public string TsDescription { get; set; }
        [J("ts_end_dttm")] public string TsEndDttm { get; set; }
        [J("ts_flow_constraints")] public List<object> TsFlowConstraints { get; set; }
        [J("ts_generator_values")] public List<object> TsGeneratorValues { get; set; }
        [J("ts_load_values")] public List<object> TsLoadValues { get; set; }
        [J("ts_name")] public string TsName { get; set; }
        [J("ts_start_dttm")] public string TsStartDttm { get; set; }
        [J("ts_switch_states")] public List<object> TsSwitchStates { get; set; }
    }

    //not used inside json
    public class Node
    {
        public string Name { get; set; }
        public HashSet<Asset> ConnectedAssets { get; set; } //= new HashSet<Asset>();
        public string FeederName { get; set; }

        public Node(string name)
        {
            Name = name;
            ConnectedAssets = new HashSet<Asset>();
            FeederName = null;
        }

        public void AddDevice(Asset asset)
        {
            ConnectedAssets.Add(asset);
        }
    }

}
