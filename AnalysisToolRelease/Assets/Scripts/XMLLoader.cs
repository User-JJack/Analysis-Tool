using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Globalization;
using TMPro;

public class XMLLoader : MonoBehaviour
{
    public class Node
    {
        public string id;
        public string name;

        public Node(string id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class Edge
    {
        public string id;
        public string fromId;
        public string toId;

        public Edge(string id, float value, string fromId, string toId)
        {
            this.id = id;
            this.fromId = fromId;
            this.toId = toId;
        }

        public override string ToString()
        {
            return"| from: " + fromId + "| to: " + toId;
        }
    }


    XDocument xmlFile;
    [Tooltip("Disable to use legacy method of value calculation")]
    [SerializeField] bool useMinMax = true;
    [Tooltip("Add file location of XML connectivity maps")]
    [SerializeField] List<string> fileLocations;
    [Tooltip("Divider string for XML deserialization")]
    [SerializeField] string divider = "mxCell";
    [Tooltip("Map Analyzer script reference")]
    [SerializeField] MapAnalyser analyzer;


    enum itemTypes
    {
        Node, Edge

    };

    enum searchTypes
    {
        Name, Id

    };

    List<Node> nodes = new List<Node>();
    List<Edge> edges = new List<Edge>();

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        foreach(string file in fileLocations)
        {
            LoadXML(file);
        }

        //analyzer.FindMatches("Mirage", "Dust2",false);
        //analyzer.AnalyseData("Mirage", "DoD", true);
    }

    void LoadXML(string fileToLoad)
    {
        Debug.Log("Loading " + fileToLoad + "...");
        nodes = new List<Node>();
        edges = new List<Edge>();
        xmlFile = XDocument.Load(fileToLoad);
        var items = xmlFile.Descendants(divider);

        foreach (var item in items)
        {

            if (CheckItem(item, itemTypes.Edge, items))
            {
                string xId = item.Attribute("id").Value;
                //float xValue = float.Parse(item.Attribute("value").Value, CultureInfo.CreateSpecificCulture("en-US"));
                //string xSource = FindWithId(items, item.Attribute("parent").Value).Attribute("source").Value;
                //string xTarget = FindWithId(items, item.Attribute("parent").Value).Attribute("target").Value;
                string xSource = item.Attribute("source").Value;
                string xTarget = item.Attribute("target").Value;

                Edge e = new Edge(xId, 1, xSource, xTarget);
                edges.Add(e);
                
            } else if (CheckItem(item, itemTypes.Node, items))
            {
                string xId = item.Attribute("id").Value;
                string xValue = item.Attribute("value").Value;

                Node n = new Node(xId, xValue);
                nodes.Add(n);
                
            }
            else
            {
                continue;
            }
        }
        /*
        Debug.Log("Completed:");
        Debug.Log("___Nodes___");
        foreach (Node node in nodes)
        {
            Debug.Log(node.ToString());
        }
        Debug.Log("___Edges___");
        foreach (Edge edge in edges)
        {
            Debug.Log(edge.ToString());
        }
        */
        CalculateMapData(fileToLoad);
    }

    bool CheckItem(XElement item, itemTypes type, IEnumerable<XElement> items)
    {



        if (type == itemTypes.Edge)
        {
            return item.Attribute("value") == null && item.Attribute("source") != null;
            //return item.Attribute("id") != null && item.Attribute("value") != null && FindWithId(items, item.Attribute("parent").Value).Attribute("source") != null && FindWithId(items, item.Attribute("parent").Value).Attribute("target") != null;
        } else
        {
            return item.Attribute("id") != null && item.Attribute("value") != null;
        }
    }

    XElement FindWithId(IEnumerable<XElement> items, string id)
    {
        foreach (var item in items)
        {
            if (item.Attribute("id").Value.Equals(id))
            {
                return item;
            }

        }
        Debug.LogError("XElement with id \"" + id + "\" could not be found");
        return null;

    }

    Dictionary<Node, float> GetNeighbors(Node node)
    {
        Dictionary<Node, float> neighbors = new Dictionary<Node, float>();

        List<Edge> connections = GetConnections(node);

        foreach (Edge edge in connections)
        {
            if (node.id.Equals(edge.fromId) && FindNodeById(edge.toId) != null)
            {
                Node n = FindNodeById(edge.toId);
                //Debug.Log(n.ToString() + " neighbor of " + node.ToString());


                neighbors.Add(FindNodeById(edge.toId), 1);
            }
            else if(node.id.Equals(edge.toId) && FindNodeById(edge.fromId) != null)
            {
                Node n = FindNodeById(edge.fromId);
                //Debug.Log(n.ToString() + "(" + n.id + ")" + " neighbor of " + node.ToString() + "(" + node.id + ") ");

                neighbors.Add(FindNodeById(edge.fromId), 1);
            }
        }


        return neighbors;
    }

    Dictionary<Node, float> GetNeighborsWithBlacklist(Node node, Node bannedNode)
    {
        Dictionary<Node, float> neighbors = new Dictionary<Node, float>();

        List<Edge> connections = GetConnections(node);

        foreach (Edge edge in connections)
        {
            if (node.id.Equals(edge.fromId) && FindNodeById(edge.toId) != null)
            {
                Node n = FindNodeById(edge.toId);
                //Debug.Log(n.ToString() + " neighbor of " + node.ToString());

                if (n != bannedNode)
                {
                    neighbors.Add(n, 1);
                }

            }
            else if (node.id.Equals(edge.toId) && FindNodeById(edge.fromId) != null)
            {
                Node n = FindNodeById(edge.fromId);
                //Debug.Log(n.ToString() + "(" + n.id + ")" + " neighbor of " + node.ToString() + "(" + node.id + ") ");

                if (n != bannedNode)
                {
                    neighbors.Add(n, 1);
                }
            }
        }


        return neighbors;
    }

    Dictionary<Node, float> GetNeighbors(string input, searchTypes type)
    {
        Dictionary<Node, float> neighbors = new Dictionary<Node, float>();

        Node conNode;
        if (type == searchTypes.Name)
        {
            conNode = FindNodeByName(input);
        }
        else
        {
            conNode = FindNodeById(input);
        }

        if (conNode == null)
        {
            return null;
        }

        List<Edge> connections = GetConnections(conNode);

        foreach (Edge edge in connections)
        {
            if (conNode.id.Equals(edge.fromId) && FindNodeById(edge.toId) != null)
            {
                neighbors.Add(FindNodeById(edge.toId), 1);
            } else if (FindNodeById(edge.fromId) != null)
            {
                neighbors.Add(FindNodeById(edge.fromId), 1);
            }
        }


        return neighbors;
    }

    List<Edge> GetConnections(Node node)
    {

        if (node == null)
        {
            return null;
        }

        List<Edge> connections = new List<Edge>();
        foreach (Edge edge in edges)
        {
            if (edge.fromId.Equals(node.id) || edge.toId.Equals(node.id))
            {
                connections.Add(edge);
            }
        }
        return connections;
    }


    List<Edge> GetConnections(string input, searchTypes type)
    {
        Node conNode;
        if (type == searchTypes.Name)
        {
            conNode = FindNodeByName(input);
        } else
        {
            conNode = FindNodeById(input);
        }

        if (conNode == null)
        {
            return null;
        }

        List<Edge> connections = new List<Edge>();
        foreach (Edge edge in edges)
        {
            if (edge.fromId.Equals(conNode.id) || edge.toId.Equals(conNode.id))
            {
                connections.Add(edge);
            }
        }
        return connections;
    }

    Node FindNodeByName(string name)
    {
        foreach (Node node in nodes) {
            if (node.name.Equals(name))
            {
                return node;
            }
        }
        //Debug.LogError("Node with name \"" + name + "\" could not be found");
        return null;
    }

    Node FindNodeById(string id)
    {
        foreach (Node node in nodes)
        {
            if (node.id.Equals(id))
            {
                return node;
            }
        }
        //Debug.LogError("Node with id \"" + id + "\" could not be found");
        return null;
    }


    private void CalculateMapData(string fileToSave)
    {
        string[] str = fileToSave.Split('/');
        MapAnalyser.MapData mapData = new MapAnalyser.MapData(str[str.Length - 1].Substring(0, str[str.Length-1].IndexOf('.')), new List<MapAnalyser.MapData.nodeData>());
        Debug.Log("Created MapData for " + mapData.name);
        float[] max = new float[] { 0, 0, 0, 0, 0, 0 };
        //MIN_MAX EXTENTION
        float[] min = new float[] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
        //MIN_MAX EXTENTION END
        List<Node> nodesClone = new List<Node>(nodes);
        foreach (Node node in nodesClone)
        {
            MapAnalyser.MapData.nodeData data = new MapAnalyser.MapData.nodeData(node.name, node.id, new float[6]);

            data.axisdata[0] = DistanceFromToOver("CT", "T", node.name);
            if(data.axisdata[0] > max[0]) { max[0] = data.axisdata[0]; };
            //MIN_MAX EXTENSION
            if(data.axisdata[0] < min[0]) { min[0] = data.axisdata[0]; };
            //MIN_MAX EXTENSION END

            data.axisdata[1] = DistanceFromToOver("CT", "A", node.name);
            if (data.axisdata[1] > max[1]) { max[1] = data.axisdata[1]; };
            //MIN_MAX EXTENSION
            if (data.axisdata[1] < min[1]) { min[1] = data.axisdata[1]; };
            //MIN_MAX EXTENSION END

            data.axisdata[2] = DistanceFromToOver("CT", "B", node.name);
            if (data.axisdata[2] > max[2]) { max[2] = data.axisdata[2]; };
            //MIN_MAX EXTENSION
            if (data.axisdata[2] < min[2]) { min[2] = data.axisdata[2]; };
            //MIN_MAX EXTENSION END

            data.axisdata[3] = DistanceFromToOver("T", "A", node.name);
            if (data.axisdata[3] > max[3]) { max[3] = data.axisdata[3]; };
            //MIN_MAX EXTENSION
            if (data.axisdata[3] < min[3]) { min[3] = data.axisdata[3]; };
            //MIN_MAX EXTENSION END

            data.axisdata[4] = DistanceFromToOver("T", "B", node.name);
            if (data.axisdata[4] > max[4]) { max[4] = data.axisdata[4]; };
            //MIN_MAX EXTENSION
            if (data.axisdata[4] < min[4]) { min[4] = data.axisdata[4]; };
            //MIN_MAX EXTENSION END

            data.axisdata[5] = DistanceFromToOver("A", "B", node.name);
            if (data.axisdata[5] > max[5]) { max[5] = data.axisdata[5]; };
            //MIN_MAX EXTENSION
            if (data.axisdata[5] < min[5]) { min[5] = data.axisdata[5]; };
            //MIN_MAX EXTENSION END

            mapData.nodeDatas.Add(data);
        }

        Debug.Log("Maximums are: " + max[0] + " | " + max[1] + " | " + max[2] + " | " + max[3] + " | " + max[4] + " | " + max[5]);
        Debug.Log("Minimums are: " + min[0] + " | " + min[1] + " | " + min[2] + " | " + min[3] + " | " + min[4] + " | " + min[5]);

        foreach (MapAnalyser.MapData.nodeData nodeData in mapData.nodeDatas)
        {
            for(int i = 0; i < 6; i++)
            {
                if(!useMinMax)
                {
                    nodeData.axisdata[i] /= max[i];
                } else
                {
                    if(min[i] != max[i])
                    {
                        //MIN_MAX EXTENSION
                        nodeData.axisdata[i] = (nodeData.axisdata[i] - min[i]) / (max[i] - min[i]);
                        //MIN_MAX EXTENSION END
                    } else
                    {
                        nodeData.axisdata[i] = 0.5f;
                    }

                }

            }
        }

        Debug.Log("Saving Map in Map Analyzer");

        analyzer.AddMap(mapData);
    }


    private float DistanceFromToOver(string from, string to, string over)
    {
        if (over.Equals("Over") || from.Equals(over) || to.Equals(over))
        {
            return CalculateDistance(from, searchTypes.Name, to, searchTypes.Name);
        }
        else
        {
            return (CalculateDistanceWithBlacklist(from, searchTypes.Name, over, searchTypes.Name, to, searchTypes.Name) + CalculateDistanceWithBlacklist(over, searchTypes.Name, to, searchTypes.Name, from, searchTypes.Name));
        }
    }

    float CalculateDistance(string start, searchTypes type1, string end, searchTypes type2)
    {
        Node startNode;
        if (type1 == searchTypes.Name)
        {
            startNode = FindNodeByName(start);
        }
        else
        {
            startNode = FindNodeById(start);
        }

        Node endNode;
        if (type2 == searchTypes.Name)
        {
            endNode = FindNodeByName(end);
        }
        else
        {
            endNode = FindNodeById(end);
        }

        //Setup
        List<Node> nodesClone = new List<Node>(nodes);

        Dictionary<Node, float> distances = new Dictionary<Node, float>();
        foreach(Node node in nodes)
        {
            distances.Add(node, float.PositiveInfinity);
        }

        Dictionary<Node, Node> routes = new Dictionary<Node, Node>();
        foreach (Node node in nodes)
        {
            routes.Add(node, null);
        }

        distances[startNode] = 0;

        while(nodes.Count != 0)
        {
            Node leatExpensive = GetLeastExpensiveNode(nodes, distances);

            ExamineConnections(leatExpensive, ref distances, ref routes);

            nodes.Remove(leatExpensive);
        }

        nodes = new List<Node>(nodesClone);
        //Debug.Log("From " + start + " to " + end + " taking: " + distances[endNode]);
        //printLeg(endNode, routes);
        return distances[endNode];
    }

    float CalculateDistanceWithBlacklist(string start, searchTypes type1, string end, searchTypes type2, string banned, searchTypes type3)
    {
        if(banned.Equals(end))
        {
            Debug.Log("Banned Node is End Node, so its unreachable");
            return float.PositiveInfinity;
        }

        Node startNode;
        if (type1 == searchTypes.Name)
        {
            startNode = FindNodeByName(start);
        }
        else
        {
            startNode = FindNodeById(start);
        }

        Node endNode;
        if (type2 == searchTypes.Name)
        {
            endNode = FindNodeByName(end);
        }
        else
        {
            endNode = FindNodeById(end);
        }

        Node bannedNode;
        if (type3 == searchTypes.Name)
        {
            bannedNode = FindNodeByName(banned);
        }
        else
        {
            bannedNode = FindNodeById(banned);
        }

        //Setup
        List<Node> nodesClone = new List<Node>(nodes);

        Dictionary<Node, float> distances = new Dictionary<Node, float>();
        foreach (Node node in nodes)
        {
            distances.Add(node, float.PositiveInfinity);
        }

        Dictionary<Node, Node> routes = new Dictionary<Node, Node>();
        foreach (Node node in nodes)
        {
            routes.Add(node, null);
        }

        distances[startNode] = 0;

        while (nodes.Count != 0)
        {
            Node leatExpensive = GetLeastExpensiveNode(nodes, distances);

            ExamineConnectionsWithBlacklist(leatExpensive, ref distances, ref routes,bannedNode);

            nodes.Remove(leatExpensive);
        }

        nodes = new List<Node>(nodesClone);
        //Debug.Log("From " + start + " to " + end + " taking: " + distances[endNode]);
        //printLeg(endNode, routes);
        return distances[endNode];
    }

    void ExamineConnections(Node n, ref Dictionary<Node,float> distances, ref Dictionary<Node,Node> routes)
    {
        foreach (var neigbor in GetNeighbors(n))
        {
            if(distances[n] + neigbor.Value < distances[neigbor.Key])
            {
                distances[neigbor.Key] = neigbor.Value + distances[n];
                routes[neigbor.Key] = n;
                //Debug.Log("Distances updated: " + neigbor.Key.ToString() + " | " + distances[neigbor.Key].ToString());
                //Debug.Log("New Rout " + neigbor.Key.ToString() + " --> " + n.ToString());
            }
        }
    }

    void ExamineConnectionsWithBlacklist(Node n, ref Dictionary<Node, float> distances, ref Dictionary<Node, Node> routes, Node bannedNode)
    {
        foreach (var neigbor in GetNeighborsWithBlacklist(n,bannedNode))
        {
            if (distances[n] + neigbor.Value < distances[neigbor.Key])
            {
                distances[neigbor.Key] = neigbor.Value + distances[n];
                routes[neigbor.Key] = n;
                //Debug.Log("Distances updated: " + neigbor.Key.ToString() + " | " + distances[neigbor.Key].ToString());
                //Debug.Log("New Rout " + neigbor.Key.ToString() + " --> " + n.ToString());
            }
        }
    }

    Node GetLeastExpensiveNode(List<Node> AllNodes, Dictionary<Node, float> distances)
    {
        Node leastExpensive = AllNodes[0];
        foreach(var n in AllNodes)
        {
            if(distances[n] < distances[leastExpensive])
            {
                leastExpensive = n;
            }
        }
        //Debug.Log("Least Expensive Node: " + leastExpensive.ToString());
        return leastExpensive;
    }

    void printLeg(Node d, Dictionary<Node, Node> routes)
    {
        if(routes[d] == null)
        {
            return;
        }
        Debug.Log(d.ToString() + " <-- " + routes[d].ToString());
        printLeg(routes[d], routes);
    }


}
