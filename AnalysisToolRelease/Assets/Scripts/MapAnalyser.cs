using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class MapAnalyser : MonoBehaviour
{
    public class MapData
    {
        public string name;

        public struct nodeData
        {
            public string name;
            public string id;
            public float[] axisdata;

            public nodeData(string name, string id, float[] axisdata)
            {
                this.name = name;
                this.id = id;
                this.axisdata = axisdata;
            }
        }

        public List<nodeData> nodeDatas;

        public MapData(string name, List<nodeData> nodeDatas)
        {
            this.name = name;
            this.nodeDatas = nodeDatas;
        }

        public nodeData GetNode(string name)
        {
            foreach(nodeData data in nodeDatas)
            {
                if(data.name.Equals(name))
                {
                    return data;
                }
            }
            Debug.Log("Node " + name + " of Map " + this.name + " cannot be found");
            return nodeDatas[0];
        }

        public override string ToString()
        {
            string res = "";
            res += name + "\n";
            foreach(nodeData data in nodeDatas)
            {
                res += data.name + " || " + data.axisdata[0].ToString("F3") + " | " + data.axisdata[1].ToString("F3") + " | " + data.axisdata[2].ToString("F3") + " | " + data.axisdata[3].ToString("F3") + " | " + data.axisdata[4].ToString("F3") + " | " + data.axisdata[5].ToString("F3") + "\n";
            }
            return res;
        }

        public void SortNodeData()
        {
            nodeData[] tmp = new nodeData[nodeDatas.Count];
            foreach(nodeData data in nodeDatas)
            {
                if(data.name.Equals("CT"))
                {
                    tmp[tmp.Length - 4] = data;
                } else if(data.name.Equals("T"))
                {
                    tmp[tmp.Length - 3] = data;
                }
                else if (data.name.Equals("A"))
                {
                    tmp[tmp.Length - 2] = data;
                }
                else if (data.name.Equals("B"))
                {
                    tmp[tmp.Length - 1] = data;
                } else
                {
                    tmp[int.Parse(data.name) - 1] = data;
                }
            }
            nodeDatas = tmp.ToList<nodeData>();
        }
    }

    public struct match
    {
        public string nodeName;
        public List<string> matchesNames;

        public match(string name) 
        {
            nodeName = name;
            matchesNames = new List<string>();
        }
    }

    struct mapMatches
    {
        public string mapName;
        public List<match> matches;

        public mapMatches(string mapName)
        {
            this.mapName = mapName;
            matches = new List<match>();
        }

        public mapMatches(string mapName, List<match> matches)
        {
            this.mapName = mapName;
            this.matches = matches;
        }
    }

    struct nodeDataNumeric
    {
        public string nodeName;
        public float value;
        public int matches;

        public nodeDataNumeric(string nodeName)
        {
            this.nodeName = nodeName;
            value = 0;
            matches = 0;
        }
    }

    struct nodeDataString
    {
        public string nodeName;
        Dictionary<string, float> matches;
        public int matchCount;

        public nodeDataString(string nodeName)
        {
            this.nodeName = nodeName;
            matches = new Dictionary<string, float>();
            matchCount = 0;
        }

        public void AddMatch(string data)
        {
            if(matches.ContainsKey(data))
            {
                matches[data] = matches[data] + 1;
            } else
            {
                matches.Add(data, 1);
            }
            matchCount++;
        }

        public string Top3()
        {
            string res = "";

            Dictionary<string, float> matchesCopy = matches.ToDictionary(entry => entry.Key, entry => entry.Value);

            int maxEntries = Mathf.Min(3,matches.Count);

            for(int i = 0; i < maxEntries; i++)
            {
                string keyMax = matchesCopy.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

                res += keyMax + " " + (matches[keyMax] / matchCount * 100).ToString("F3") + "%";
                if(i != maxEntries - 1)
                {
                    res += ", ";
                }

                matchesCopy.Remove(keyMax);
            }


            return res;
        }
    }

    [Tooltip("UI Manager script reference")]
    [SerializeField] UIManager uIManager;
    [Tooltip("Location of the Data Sheets text files")]
    [SerializeField] string dataSheetPath = "Assets/DataSheets";
    [Tooltip("Factor that is multiplied with the maximum allowed offsets to increase or decrease the number of matches (Not recommended)")]
    [SerializeField] float factor = 1f;
    [Tooltip("If activated, the data output will be sorted numericaly, only works if all non reference nodes are named squentialy with numbers beginning with 1")]
    [SerializeField] bool sortData = true;

    List<MapData> maps = new List<MapData>();

    public void AddMap(MapData map)
    {

        if(sortData)
        {
            map.SortNodeData();
        }
        maps.Add(map);
        uIManager.AddMapOption(map.name);
        Debug.Log(map.ToString());
    }

    public void AnalyseData(string mapName, string dataName, bool isNumeric, string[] references)
    {

        Debug.Log("Analyzing for " + mapName + " regarding " + dataName);

        MapData map = FindMapByName(mapName);

        List<mapMatches> mapMatches = new List<mapMatches>();

        foreach(MapData data in maps)
        {
            if(data != map && references.Contains(data.name))
            {
                mapMatches.Add(new MapAnalyser.mapMatches(data.name, FindMatches(mapName,data.name,false)));
            }
            
        }

        if(isNumeric)
        {
            List<nodeDataNumeric> nodeDataNumerics = new List<nodeDataNumeric>();
            foreach(MapData.nodeData node in map.nodeDatas)
            {
                nodeDataNumerics.Add(new nodeDataNumeric(node.name));
            }

            foreach(mapMatches mapM in mapMatches)
            {
                string path = Path.Combine(dataSheetPath, mapM.mapName + "_" + dataName + ".txt");
                StreamReader reader = new StreamReader(path);
                string txt = reader.ReadToEnd();
                reader.Close();
                string[] lines = txt.Split("\n");
                lines[lines.Length - 1] += '\r';

                foreach (match match in mapM.matches)
                {
                    nodeDataNumeric toUpdate = new nodeDataNumeric("null");
                    int index = 0;
                    for(index = 0; index < nodeDataNumerics.Count; index++)
                    {
                        if(nodeDataNumerics[index].nodeName.Equals(match.nodeName))
                        {
                            toUpdate = nodeDataNumerics[index];
                            break;
                        }
                    }

                    foreach(string nodeName in match.matchesNames)
                    {
                        float value = FindNumericData(lines, nodeName);
                        toUpdate.value += value;
                        toUpdate.matches++;
                    }
                    nodeDataNumerics[index] = toUpdate;
                }
            }

            for(int i = 0; i < nodeDataNumerics.Count; i++)
            {
                if(nodeDataNumerics[i].matches != 0)
                {
                    nodeDataNumeric nCopy = nodeDataNumerics[i];
                    nCopy.value /= nCopy.matches;
                    nodeDataNumerics[i] = nCopy;
                }

            }

            string res = "Statistic estimate of " + dataName + " for " + mapName + ":\n";
            foreach(nodeDataNumeric nDN in nodeDataNumerics)
            {
                res += nDN.nodeName + ": " + nDN.value + " from " + nDN.matches + "\n";
            }

            Debug.Log(res);

        } else
        {
            List<nodeDataString> nodeDataStrings = new List<nodeDataString>();
            foreach (MapData.nodeData node in map.nodeDatas)
            {
                nodeDataStrings.Add(new nodeDataString(node.name));
            }

            foreach (mapMatches mapM in mapMatches)
            {
                string path = Path.Combine(dataSheetPath, mapM.mapName + "_" + dataName + ".txt");
                StreamReader reader = new StreamReader(path);
                string txt = reader.ReadToEnd();
                reader.Close();
                string[] lines = txt.Split("\n");
                lines[lines.Length - 1] += '\r';

                foreach (match match in mapM.matches)
                {
                    nodeDataString toUpdate = new nodeDataString("null");
                    int index = 0;
                    for (index = 0; index < nodeDataStrings.Count; index++)
                    {
                        if (nodeDataStrings[index].nodeName.Equals(match.nodeName))
                        {
                            toUpdate = nodeDataStrings[index];
                            break;
                        }
                    }

                    foreach (string nodeName in match.matchesNames)
                    {
                        string value = FindStringData(lines, nodeName);
                        toUpdate.AddMatch(value);
                    }
                    nodeDataStrings[index] = toUpdate;
                }
            }

            string res = "Statistic estimate of " + dataName + " for " + mapName + ":\n";
            foreach (nodeDataString nDS in nodeDataStrings)
            {
                res += nDS.nodeName + ": " + nDS.Top3() + " from " + nDS.matchCount + "\n";
            }

            Debug.Log(res);

        }

    }

    private float FindNumericData(string[] lines, string nodeName)
    {
        float value = 0;
        foreach(string str in lines)
        {
            if(str.Substring(0,str.IndexOf(" ")).Equals(nodeName))
            {

                if (float.TryParse(str.Substring(str.IndexOf(" ") + 1, str.Length - (str.IndexOf(" ") + 1)), out value))
                {
                    break;
                } else
                {
                    Debug.Log("Couldnt parse float from " + str);
                    break;
                }
            }
        }
        return value;
    }

    private string FindStringData(string[] lines, string nodeName)
    {
        string value = "";
        foreach (string str in lines)
        {
            if (str.Substring(0, str.IndexOf(" ")).Equals(nodeName))
            {
                value = str.Substring(str.IndexOf(" ") + 1, str.Length - (str.IndexOf(" ") + 1));
            }
        }
        return value;
    }


    public List<match> FindMatches(string map1, string map2, bool useAverages)
    {
        MapData m1 = FindMapByName(map1);
        MapData m2 = FindMapByName(map2);

        float[] maxOffsets = new float[] { 0, 0, 0, 0, 0, 0 };
        string[] referenceNames = new string[] { "CT", "T", "A", "B" };

        if(!useAverages)
        {
            foreach (string refN in referenceNames)
            {
                maxOffsets = GetCombinedMax(maxOffsets, GetAbsDiff(m1.GetNode(refN).axisdata, m2.GetNode(refN).axisdata));
            }
        } else
        {
            List<float[]> arrays = new List<float[]>();

            foreach(string refN in referenceNames)
            {
                arrays.Add(GetAbsDiff(m1.GetNode(refN).axisdata, m2.GetNode(refN).axisdata));
            }
            maxOffsets = GetAVG(arrays);

        }

        maxOffsets = MultiplyArray(maxOffsets, factor);

        Debug.Log("Max allowed Offsets: " + maxOffsets[0] + " | " + maxOffsets[1] + " | " + maxOffsets[2] + " | " + maxOffsets[3] + " | " + maxOffsets[4] + " | " + maxOffsets[5]);

        List<match> matches = new List<match>();

        foreach(MapData.nodeData data in m1.nodeDatas)
        {
            match match = new match(data.name);

            foreach(MapData.nodeData toCmp in m2.nodeDatas)
            {
                if(IsSmallerOrEqual(GetAbsDiff(data.axisdata,toCmp.axisdata),maxOffsets))
                {
                    match.matchesNames.Add(toCmp.name);
                }
            }
            matches.Add(match);
        }

        string res = map1 + " x " + map2 + "\n";
        foreach(match m in matches)
        {
            res += m.nodeName + " --> " + ListToString(m.matchesNames) + "\n";
        }
        Debug.Log(res);

        return matches;
    }

    private float[] GetAbsDiff(float[] f1, float[] f2)
    {
        if(f1.Length != f2.Length)
        {
            Debug.Log("Trying to subtract Arrays with dissimilar lengths");
            return null;
        }

        float[] res = new float[f1.Length];

        for(int i = 0; i < f1.Length; i++)
        {
            res[i] = Mathf.Abs(f1[i] - f2[i]);
        }
        return res;
    }

    private float[] GetAVG(List<float[]> arrays)
    {
        foreach(float[] arr in arrays)
        {
            if(arr.Length != arrays[0].Length)
            {
                Debug.Log("Trying to Average Arrays with dissimilar lengths");
                return null;
            }
        }

        float[] resArr = new float[arrays[0].Length];

        for (int i = 0; i < arrays[0].Length; i++)
        {
            float res = 0;
            foreach(float[] arr in arrays)
            {
                res += arr[i];
            }

            res /= arrays.Count;
            resArr[i] = res;
        }
        return resArr;
    }

    private float[] GetCombinedMax(float[] f1, float[] f2)
    {
        if (f1.Length != f2.Length)
        {
            Debug.Log("Trying to max Arrays with dissimilar lengths");
            return null;
        }

        float[] res = new float[f1.Length];

        for (int i = 0; i < f1.Length; i++)
        {
            res[i] = Mathf.Max(f1[i], f2[i]);
        }
        return res;
    }

    private float[] MultiplyArray(float[] arr, float value)
    {
        float[] res = new float[arr.Length];
        for (int i = 0; i < res.Length; i++)
        {
            res[i] = arr[i] * value; 
        }
        return res;
    }

    private bool IsSmallerOrEqual(float[] f1, float[] f2)
    {
        if (f1.Length != f2.Length)
        {
            Debug.Log("Trying to compare Arrays with dissimilar lengths");
            return false;
        }
        for(int i = 0; i < f1.Length; i++)
        {
            if (f1[i] > f2[i]) return false;
        }

        return true;
    }

    private MapData FindMapByName(string name)
    {
        foreach(MapData data in maps)
        {
            if(data.name.Equals(name))
            {
                return data;
            }
        }
        Debug.Log("Map " + name + " cannot be found");
        return null;
    }

    private string ListToString(List<string> list)
    {
        string res = "";
        foreach(string str in list)
        {
            res += str + " ";
        }
        return res;
    }

}
