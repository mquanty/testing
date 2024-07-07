       //Breadth-First-Search Approach for tracing
        public List<Asset> TraceFeeder(Asset startingAsset, Node startingNode = null)
        {
            var feederAssets = new List<Asset>();
            try
            {
                if (string.IsNullOrEmpty(startingNode?.Name))
                    startingNode = startingAsset?.Node1 ?? startingAsset?.Node2;
                //if (!string.IsNullOrEmpty(feederName))
                //    startingNode.FeederName = feederName;

                //feederAssets.Add(startingAsset);   //add the first device to the list
                startingAsset.NodeSideTo = startingNode; //make the startingnode as to side node of the device

                Queue<(Asset asset, Node node, Asset parent)> queue = new Queue<(Asset, Node, Asset)>();
                HashSet<string> visitedNodes = new HashSet<string>();

                queue.Enqueue((startingAsset, startingNode, null));
                visitedNodes.Add(startingNode.Name);
                while (queue.Count > 0)
                {
                    var (currentAsset, currentNode, parentAsset) = queue.Dequeue();

                    string feederName = parentAsset?.FeederName; // currentAsset.Parent?.FeederName; //read feedername from parent asset
                    //for feederheads already the feedername will be there
                    //get the feeder name from the import feeder head dictionary, reset and assign the feeder name
                    if (currentAsset.IsFeederHead)
                        feederName = currentAsset.FeederName;//feederHeadName2FeederName?.GetValueOrDefault(currentAsset.AssetId);

                    //if (string.IsNullOrEmpty(currentAsset.FeederName) || currentAsset.IsFeederHead)
                    if(currentAsset.Parent == null)
                    {
                        //if the parent is null, set the parent, case when starting device has existing parent
                        currentAsset.Parent ??= parentAsset;
                        if (!string.IsNullOrEmpty(feederName))
                            currentAsset.FeederName = feederName;

                        feederAssets.Add(currentAsset);
                    }
                    // Continue to the next iteration if the FeederName is already set, never enters the else condition
                    else continue;

                    foreach (var connectedAsset in currentNode.ConnectedAssets)
                    {
                        if (connectedAsset == currentAsset) continue;
                        if (connectedAsset.IsFeederHead)
                            feederName = connectedAsset.FeederName;
                        //if already traced and if it is a feederhead, nodesideto should not be null
                        if (connectedAsset.IsTraced && !(connectedAsset.IsFeederHead && connectedAsset.NodeSideTo == null))
                        {
                            //if (connectedAsset.IsFeederHead)
                            //    continue;

                            if (connectedAsset.FeederName == feederName)
                                //the asset is already been traced but again appeared through a closed circuit or mesh circuit
                                continue;
                            else
                                //set from another feeder
                                continue;
                        }

                        var nextNode = connectedAsset.NodeList.First() == currentNode.Name ?
                            connectedAsset.Node2 : connectedAsset.Node1;

                        // Set from-side and to-side nodes
                        connectedAsset.NodeSideFrom = currentNode;
                        connectedAsset.NodeSideTo = nextNode;

                        //stop if the switch is open or
                        //stop if the IsSubstationTracing is true and the device's circuitid is not null, mostly for the breakers which acts as feederheads where the circuitid is not null will have this.
                        if (connectedAsset.IsOpenSwitch())
                        {
                            if (string.IsNullOrEmpty(connectedAsset.FeederName))
                            {
                                connectedAsset.FeederName = feederName;
                                connectedAsset.Parent = currentAsset;
                                feederAssets.Add(connectedAsset);
                            }

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
                
                //Console.WriteLine($"Feeder tracing complete in total time {st.ElapsedMilliseconds} ms. " +
                //                  $"Asset:{startingAsset.AssetType}_{startingAsset.AssetId}, Node:{startingNode.Name}, Count:{feederAssets?.Count()} Feeder:{feederName}");
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return feederAssets;
        }
 
