using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using UnityEditor;
using System.IO;

public class JAPSEncoder
{

    public const int IndentSize = 2;

    public static string Encode(PlayableSong song, string clipName)
    {
        string str = "JANOARG Playable Song Format\ngithub.com/ducdat0507/janoarg";
        
        str += "\n\n[METADATA]";

        str += "\nName: " + song.SongName;
        if (!string.IsNullOrWhiteSpace(song.AltSongName))
            str += "\nAlt Name: " + song.AltSongName;

        str += "\nArtist: " + song.SongArtist;
        if (!string.IsNullOrWhiteSpace(song.AltSongArtist))
            str += "\nAlt Artist: " + song.AltSongArtist;

        str += "\nGenre: " + song.Genre;
        str += "\nLocation: " + song.Location;
        
        str += "\n\n[RESOURCES]";
        str += "\nClip: " + clipName;

        str += "\n\n[COLORS]";
        str += "\nBackground: " + EncodeColor(song.BackgroundColor);
        str += "\nInterface: " + EncodeColor(song.InterfaceColor);
        
        str += "\n\n[TIMING]";
        foreach (BPMStop stop in song.Timing.Stops) {
            str += EncodeBPMStop(stop);
        }
        
        str += "\n\n[CHARTS]";
        foreach (ExternalChartMeta chart in song.Charts) {
            str += EncodeExternalChartMeta(chart);
        }


        return str;
    }
    
    public static string EncodeBPMStop(BPMStop stop, int depth = 0)
    {
        string indent = new string(' ', depth);
        string indent2 = new string(' ', depth + IndentSize);

        string str = "\n" + indent + "+ BPM"
            + " " + stop.Offset.ToString(CultureInfo.InvariantCulture)
            + " " + stop.BPM.ToString(CultureInfo.InvariantCulture)
            + " " + stop.Signature.ToString(CultureInfo.InvariantCulture)
            + " " + (stop.Significant ? "S" : "_");

        return str;
    }
    
    public static string EncodeExternalChartMeta(ExternalChartMeta chart, int depth = 0)
    {
        string indent = new string(' ', depth);
        string indent2 = new string(' ', depth + IndentSize);

        string str = "\n" + indent + "+ Chart"
            + "\n" + indent2 + "Target: " + chart.Target
            + "\n" + indent2 + "Index: " + chart.DifficultyIndex.ToString(CultureInfo.InvariantCulture)
            + "\n" + indent2 + "Name: " + chart.DifficultyName
            + "\n" + indent2 + "Level: " + chart.DifficultyLevel
            + "\n" + indent2 + "Constant: " + chart.ChartConstant.ToString(CultureInfo.InvariantCulture);

        return str;
    }

    public static string EncodeColor(Color col)
    {
        return col.r.ToString(CultureInfo.InvariantCulture)
            + " " + col.g.ToString(CultureInfo.InvariantCulture)
            + " " + col.b.ToString(CultureInfo.InvariantCulture)
            + " " + col.a.ToString(CultureInfo.InvariantCulture);
    }
}
