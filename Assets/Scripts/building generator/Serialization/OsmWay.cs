using System.Collections.Generic;
using System.Xml;

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
    ----------------------------------------
    Modified 2025 by Alexander Ohlsson
*/

/// <summary>
/// An OSM object that describes an arrangement of OsmNodes into a shape or road.
/// </summary>
/// 
namespace MapMaker
{
    class OsmWay : BaseOsm
    {

        public ulong ID { get; private set; }

        public bool Visible { get; private set; }

        public List<ulong> NodeIDs { get; private set; }

        public bool IsBoundary { get; private set; }

        public bool IsBuilding { get; private set; }

        public bool IsRoad { get; private set; }

        public float Height { get; private set; }

        public string Name { get; private set; }

        public int Lanes { get; private set; }
        public bool IsFence { get; private set; } // Indicates if the way is a fence
        public bool IsHedge { get; private set; }
        public bool IsWall { get; private set; }
        public bool IsKerb { get; private set; }
        public bool IsGuardRail { get; private set; }
        public bool IsHandrail { get; private set; }
        public bool IsDitch { get; private set; }
        public bool IsTree { get; private set; }
        public OsmWay(XmlNode node)
        {
            NodeIDs = new List<ulong>();
            Height = 5.0f; // Default height for structures is 1 story (approx. 3m)
            Lanes = 1;      // Number of lanes either side of the divide 
            Name = "";

            // Get the data from the attributes
            ID = GetAttribute<ulong>("id", node.Attributes);
            Visible = GetAttribute<bool>("visible", node.Attributes);

            // Get the nodes
            XmlNodeList nds = node.SelectNodes("nd");
            foreach(XmlNode n in nds)
            {
                ulong refNo = GetAttribute<ulong>("ref", n.Attributes);
                NodeIDs.Add(refNo);
            }

            if (NodeIDs.Count > 1)
            {
                IsBoundary = NodeIDs[0] == NodeIDs[NodeIDs.Count - 1];
            }

            // Read the tags
            XmlNodeList tags = node.SelectNodes("tag");
            foreach (XmlNode t in tags)
            {
                string key = GetAttribute<string>("k", t.Attributes);
                if (key == "building:levels")
                {
                    Height = 5.0f * GetAttribute<float>("v", t.Attributes);
                }
                else if (key == "height")
                {
                    Height = 1f * GetAttribute<float>("v", t.Attributes);
                }
                else if (key == "building")
                {
                    IsBuilding = true; // GetAttribute<string>("v", t.Attributes) == "yes";
                }
                else if (key == "highway")
                {
                    IsRoad = true;
                }
                else if (key=="lanes")
                {
                    Lanes = GetAttribute<int>("v", t.Attributes);
                }
                else if (key=="name")
                {
                    Name = GetAttribute<string>("v", t.Attributes);
                }
                else if (key == "barrier" && GetAttribute<string>("v", t.Attributes) == "fence")
                {
                    IsFence = true; // Mark the way as a fence
                }
                else if (key == "barrier" && GetAttribute<string>("v", t.Attributes) == "hedge")
                {
                    IsHedge = true; // Mark the way as a fence
                }
                else if (key == "barrier" && GetAttribute<string>("v", t.Attributes) == "wall")
                {
                    IsWall = true; // Mark the way as a fence
                }
                else if (key == "barrier" && GetAttribute<string>("v", t.Attributes) == "kerb")
                {
                    IsKerb = true; // Mark the way as a fence
                }
                else if (key == "barrier" && GetAttribute<string>("v", t.Attributes) == "guard_rail")
                {
                    IsGuardRail = true; // Mark the way as a fence
                }
                else if (key == "barrier" && GetAttribute<string>("v", t.Attributes) == "handrail")
                {
                    IsHandrail = true; // Mark the way as a fence
                }
                else if (key == "barrier" && GetAttribute<string>("v", t.Attributes) == "ditch")
                {
                    IsDitch = true; // Mark the way as a fence
                }
                else if (key == "natural" && GetAttribute<string>("v", t.Attributes) == "wood")
                {
                    IsTree = true; // Mark the way as a fence
                }
                else if (key == "landuse" && GetAttribute<string>("v", t.Attributes) == "forest")
                {
                    IsTree = true; // Mark the way as a fence
                }
            
            }
        }
    }
}