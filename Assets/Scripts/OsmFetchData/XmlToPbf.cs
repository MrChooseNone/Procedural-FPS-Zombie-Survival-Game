using OsmSharp.Streams;
using OsmSharp.Streams.Complete;
using System.IO;
using UnityEngine;

public class OSMConverter : MonoBehaviour
{

    public static void ConvertToPBF(string xmlFilePath, string pbfFilePath)
    {
        using (var xmlStream = File.OpenRead(xmlFilePath))
        using (var pbfStream = File.OpenWrite(pbfFilePath))
        {
            // Read XML data
            var source = new XmlOsmStreamSource(xmlStream);

            // Convert and save to .pbf
            var target = new PBFOsmStreamTarget(pbfStream);
            target.RegisterSource(source);
            target.Pull();

            Debug.Log($"OSM data successfully converted to {pbfFilePath}");
            
        }
    }
}
