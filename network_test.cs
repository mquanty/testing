{
"switches": [
	{ "asset_id": "switch1", "asset_type": "SWITCH", "node_list": [ "node1", "node2" ], "switch_specific": 1 },
	{ "asset_id": "switch2", "asset_type": "SWITCH", "node_list": [ "node2", "node3" ] },
	{ "asset_id": "switch3", "asset_type": "SWITCH", "node_list": [ null, "node4" ] },
	{ "asset_id": "switch4", "asset_type": "SWITCH", "node_list": [ "node1", "node4" ] },
	{ "asset_id": "switch5", "asset_type": "SWITCH", "node_list": [ "node3", "node1" ] },
	{ "asset_id": "switch6", "asset_type": "SWITCH", "node_list": [ "node3","node6" ] },
	{ "asset_id": "switch7", "asset_type": "SWITCH", "node_list": [ "node3", null ] }
],
"lines": [
	{ "asset_id": "line1", "asset_type": "LINE", "node_list": [ "node11", "node2" ] },
	{ "asset_id": "line2", "ass et_type": "LINE", "node_list": [ "node12", "node3" ] },
	{ "asset_id": "line3", "asset_type": "LINE", "node_list": [ null, "node14" ] },
	{ "asset_id": "line4", "asset_type": "LINE", "node_list": [ "node1", "node14" ], "line_specific" : false },
	{ "asset_id": "line5", "asset_type": "LINE", "node_list": [ "node3", "node11" ] },
	{ "asset_id": "line6", "asset_type": "LINE", "node_list": [ "node13", "node6" ] },
	{ "asset_id": "line7", "asset_type": "LINE", "node_list": [ "node13", null ] }
],
"motors": [
	{ "asset_id": "motor1", "asset_type": "MOTOR", "node_list": [ "node2", "node22" ] },
	{ "asset_id": "motor2", "asset_type": "MOTOR", "node_list": [ "node22", "node23" ] },
	{ "asset_id": "motor3", "asset_type": "MOTOR", "node_list": [ "node22", "node26" ] },
	{ "asset_id": "motor4", "asset_type": "MOTOR", "node_list": [ "node21", null ] },
	{ "asset_id": "motor5", "asset_type": "MOTOR", "node_list": [ "node3", "node1" ] },
	{ "asset_id": "motor6", "asset_type": "MOTOR", "node_list": [ "node3", "node26" ] },
	{ "asset_id": "motor7", "asset_type": "MOTOR", "node_list": [ null, "node27" ], "motor_specific" : 23.44 }
]
}

i have some json file content like this, I want to import this to newtonsoft json to deserialize to objects,
i need the model classes to convert to object, and also i need the connectivity of completel network as well.
you can see each type of class has id, type and node list there, Each asset has 2 nodes, either can be none,
either first or second node can also appear in first or second node of other asset's node list.
basically all the assets are interconnected through the nodes. like switch1 -> node1 -> switch4 -> node4 -> switch3
and one more network like, switch1 -> node2 -> switch2 -> ( switch5 and switch6 and switch7). switch5 -> node1 -> switch1.
switch6 -> node6 -> none. switch7 -> none. Some asset can have only 1 node like switch7. And some node can be connected to only one asset. 
And some node can be connected to multiple assets. 

I need to find the hierarachy at both the node end of any asset. If a node is appeared in one end of an asset, while traversing to other asset we neeed to 
check other node, and further connected assets. 

Also i need to find all the assets connected to a single node. 
use for loop or span type, and use efficient design pattern to handle this. and provide the code in c#.
all the 3 type of class like switche, line and motor can inherit from a parent class like Asset which has id, type and node list as attributes.
use parent constructor initialize so that it should automatically establish connected network while doing deserialize only.






using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public abstract class Asset
{
    public string asset_id { get; set; }
    public string asset_type { get; set; }
    public List<string> node_list { get; set; }
    public List<Asset> connectedAssets { get; set; }

    protected Asset()
    {
        connectedAssets = new List<Asset>();
    }

    public void ConnectAsset(Asset asset)
    {
        connectedAssets.Add(asset);
        asset.connectedAssets.Add(this);
    }

    public void ConnectAssetsBasedOnNodeList(IEnumerable<Asset> allAssets)
    {
        foreach (var node in node_list.Where(n => n != null))
        {
            foreach (var otherAsset in allAssets.Where(a => a != this && a.node_list.Contains(node)))
            {
                ConnectAsset(otherAsset);
            }
        }
    }

    public static List<Asset> GetConnectedAssetsFromNodeId(string assetId, string nodeId, IEnumerable<Asset> allAssets)
    {
        var asset = allAssets.FirstOrDefault(a => a.asset_id == assetId);
        if (asset == null)
            return new List<Asset>();

        var nodeIndex = asset.node_list.IndexOf(nodeId);
        if (nodeIndex == -1)
            return new List<Asset>();

        var connectedAssets = new List<Asset>();
        var startingAsset = asset;
        var currentNode = nodeId;

        while (true)
        {
            var nextAsset = startingAsset.connectedAssets.FirstOrDefault(a => a.node_list.Contains(currentNode) && a != startingAsset);
            if (nextAsset == null)
                break;

            connectedAssets.Add(nextAsset);
            var nextNodeIndex = nextAsset.node_list.IndexOf(currentNode) == 0 ? 1 : 0;
            currentNode = nextAsset.node_list[nextNodeIndex];
            startingAsset = nextAsset;
        }

        return connectedAssets;
    }
}

// Other classes (Switch, Line, Motor, NetworkData) remain the same
