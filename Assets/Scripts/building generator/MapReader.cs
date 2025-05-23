using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using MapMaker;
using System.IO;
using System.Collections;
using System;

/*
    Copyright (c) 2017 Sloan Kelly
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

class MapReader : MonoBehaviour
{
    [HideInInspector]
    public Dictionary<ulong, OsmNode> nodes;

    [HideInInspector]
    public List<OsmWay> ways;
    [HideInInspector]
    public OsmBounds bounds;

    public GameObject groundPlane;

    [Tooltip("The resource file that contains the OSM map data")]
    public string resourceFile;
    private object pathToData;

    public bool IsReady { get; private set; }

	// Use this for initialization
	void Start ()
    {
		System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
		customCulture.NumberFormat.NumberDecimalSeparator = ".";
		System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
		
        nodes = new Dictionary<ulong, OsmNode>();
        ways = new List<OsmWay>();

        pathToData = Path.Combine(Application.persistentDataPath, resourceFile);

        if (File.Exists((string)pathToData))
        {
            
            Parse();
        }else{
            StartCoroutine(CheckFile());
        }
    }

    void Parse(){
            string xmlContent = File.ReadAllText((string)pathToData);
        
            // Now parse the XML content as a string
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            SetBounds(doc.SelectSingleNode("/osm/bounds"));
            
            
            XmlNodeList nodeNode = doc.SelectNodes("/osm/node");
            if(nodeNode != null){
                GetNodes(nodeNode);
            }
            XmlNodeList wayNode = doc.SelectNodes("/osm/way");
            if(wayNode != null){
                GetWays(wayNode);
            }
            

            float minx = (float)MercatorProjection.lonToX(bounds.MinLon);
            float maxx = (float)MercatorProjection.lonToX(bounds.MaxLon);
            float miny = (float)MercatorProjection.latToY(bounds.MinLat);
            float maxy = (float)MercatorProjection.latToY(bounds.MaxLat);

            //groundPlane.transform.localScale = new Vector3((maxx - minx) / 2, 1, (maxy - miny) / 2);

            IsReady = true;
    }

    IEnumerator CheckFile()
    {
        for (int i = 0; i < 20; i++)
        {
            // Wait for the bounding box to be initialized (non-zero values)
            if (File.Exists((string)pathToData))
            {
                Parse();
                yield break; // Exit the coroutine once the URL is created
            }
            else
            {
                Debug.Log($"Waiting for file... Attempt {i + 1}");
                yield return new WaitForSeconds(2); // Wait for 1 second before checking again
            }
        }
        Debug.LogError("file was not initialized after 20 attempts.");
    }

    void Update()
    {
        foreach (OsmWay w in ways)
        {
            if (w.Visible)
            {
                Color c = Color.cyan;               // cyan for buildings
                if (!w.IsBoundary) c = Color.red; // red for roads

                for (int i = 1; i < w.NodeIDs.Count; i++)
                {
                    OsmNode p1 = nodes[w.NodeIDs[i - 1]];
                    OsmNode p2 = nodes[w.NodeIDs[i]];

                    Vector3 v1 = p1 - bounds.Centre;
                    Vector3 v2 = p2 - bounds.Centre;

                    Debug.DrawLine(v1, v2, c);                   
                }
            }
        }
    }

    void GetWays(XmlNodeList xmlNodeList)
    {
        foreach (XmlNode node in xmlNodeList)
        {
            OsmWay way = new OsmWay(node);
            ways.Add(way);
        }
    }

    void GetNodes(XmlNodeList xmlNodeList)
    {
        foreach (XmlNode n in xmlNodeList)
        {
            OsmNode node = new OsmNode(n);
            nodes[node.ID] = node;
        }
    }

    void SetBounds(XmlNode xmlNode)
    {
        bounds = new OsmBounds(xmlNode);
    }
}