using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using StorybrewCommon.Mapset;

namespace StorybrewEditor.Util;

public static class BinaryReaderExtensions
{
    public static ControlPoint ReadControlPoint(this BinaryReader binaryReader)
    {
        return new ControlPoint()
        {
            Offset = binaryReader.ReadDouble(),
            beatDurationSV = binaryReader.ReadDouble(),
            BeatPerMeasure = binaryReader.ReadInt32(),
            SampleSet = (SampleSet)binaryReader.ReadInt32(),
            CustomSampleSet = binaryReader.ReadInt32(),
            Volume = binaryReader.ReadSingle(),
            IsInherited = binaryReader.ReadBoolean(),
            IsKiai = binaryReader.ReadBoolean(),
            OmitFirstBarLine = binaryReader.ReadBoolean(),
        };
    }

    public static OsuBreak ReadOsuBreak(this BinaryReader binaryReader)
    {
        return new OsuBreak()
        {
            StartTime = binaryReader.ReadDouble(),
            EndTime = binaryReader.ReadDouble()
        };
    }

    public static Color4 ReadTkColor4(this BinaryReader binaryReader)
    {
        var r = binaryReader.ReadSingle();
        var g = binaryReader.ReadSingle();
        var b = binaryReader.ReadSingle();
        var a = binaryReader.ReadSingle();
        return new Color4(r, g, b, a);
    }

    public static Vector2 ReadTkVector2(this BinaryReader binaryReader)
    {
        var x = binaryReader.ReadSingle();
        var y = binaryReader.ReadSingle();
        return new Vector2(x, y);
    }
}