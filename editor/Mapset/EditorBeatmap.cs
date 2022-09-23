using OpenTK.Graphics;
using OpenTK.Mathematics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Util;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using StorybrewEditor.Util;

namespace StorybrewEditor.Mapset
{
    public class EditorBeatmap : Beatmap
    {
        public readonly string Path;

        public override string AudioFilename => audioFilename;
        private string audioFilename = "audio.mp3";

        private string name = string.Empty;
        public override string Name => name;

        private long id;
        public override long Id => id;

        private readonly List<int> bookmarks = new List<int>();
        public override IEnumerable<int> Bookmarks => bookmarks;

        private double hpDrainRate = 5;
        public override double HpDrainRate => hpDrainRate;

        private double circleSize = 5;
        public override double CircleSize => circleSize;

        private double overallDifficulty = 5;
        public override double OverallDifficulty => overallDifficulty;

        private double approachRate = 5;
        public override double ApproachRate => approachRate;

        private double sliderMultiplier = 1.4;
        public override double SliderMultiplier => sliderMultiplier;

        private double sliderTickRate = 1;
        public override double SliderTickRate => sliderTickRate;

        private readonly List<OsuHitObject> hitObjects = new List<OsuHitObject>();
        public override IEnumerable<OsuHitObject> HitObjects => hitObjects;

        private readonly List<Color4> comboColors = new List<Color4>()
        {
            new Color4(255, 192, 0, 255),
            new Color4(0, 202, 0, 255),
            new Color4(18, 124, 255, 255),
            new Color4(242, 24, 57, 255),
        };
        public override IEnumerable<Color4> ComboColors => comboColors;

        public string backgroundPath;
        public override string BackgroundPath => backgroundPath;

        private readonly List<OsuBreak> breaks = new List<OsuBreak>();
        public override IEnumerable<OsuBreak> Breaks => breaks;

        public EditorBeatmap(string path)
        {
            Path = path;
        }

        public override string ToString() => Name;

        #region Timing

        private readonly List<ControlPoint> controlPoints = new List<ControlPoint>();

        public override IEnumerable<ControlPoint> ControlPoints => controlPoints;
        public override IEnumerable<ControlPoint> TimingPoints
        {
            get
            {
                var timingPoints = new List<ControlPoint>();
                foreach (var controlPoint in controlPoints)
                    if (!controlPoint.IsInherited)
                        timingPoints.Add(controlPoint);
                return timingPoints;
            }
        }

        public ControlPoint GetControlPointAt(int time, Predicate<ControlPoint> predicate)
        {
            if (controlPoints == null) return null;
            var closestTimingPoint = (ControlPoint)null;
            foreach (var controlPoint in controlPoints)
            {
                if (predicate != null && !predicate(controlPoint)) continue;
                if (closestTimingPoint == null || controlPoint.Offset - time <= ControlPointLeniency)
                    closestTimingPoint = controlPoint;
                else break;
            }
            return closestTimingPoint ?? ControlPoint.Default;
        }

        public override ControlPoint GetControlPointAt(int time)
            => GetControlPointAt(time, null);

        public override ControlPoint GetTimingPointAt(int time)
            => GetControlPointAt(time, cp => !cp.IsInherited);

        #endregion

        #region .osu parsing

        public static EditorBeatmap Load(string path)
        {
            Trace.WriteLine($"Loading beatmap {path}");
            try
            {
                var beatmap = new EditorBeatmap(path);
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream, Project.Encoding))
                    reader.ParseSections(sectionName =>
                    {
                        switch (sectionName)
                        {
                            case "General": parseGeneralSection(beatmap, reader); break;
                            case "Editor": parseEditorSection(beatmap, reader); break;
                            case "Metadata": parseMetadataSection(beatmap, reader); break;
                            case "Difficulty": parseDifficultySection(beatmap, reader); break;
                            case "Events": parseEventsSection(beatmap, reader); break;
                            case "TimingPoints": parseTimingPointsSection(beatmap, reader); break;
                            case "Colours": parseColoursSection(beatmap, reader); break;
                            case "HitObjects": parseHitObjectsSection(beatmap, reader); break;
                        }
                    });
                return beatmap;
            }
            catch (Exception e)
            {
                throw new BeatmapLoadingException($"Failed to load beatmap \"{System.IO.Path.GetFileNameWithoutExtension(path)}\".", e);
            }
        }

        private static void parseGeneralSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "AudioFilename": beatmap.audioFilename = value; break;
                }
            });
        }
        private static void parseEditorSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "Bookmarks":
                        foreach (var bookmark in value.Split(','))
                            if (value.Length > 0)
                                beatmap.bookmarks.Add(int.Parse(bookmark));
                        break;
                }
            });
        }
        private static void parseMetadataSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "Version": beatmap.name = value; break;
                    case "BeatmapID": beatmap.id = long.Parse(value); break;
                }
            });
        }
        private static void parseDifficultySection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "HPDrainRate": beatmap.hpDrainRate = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "CircleSize": beatmap.circleSize = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "OverallDifficulty": beatmap.overallDifficulty = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "ApproachRate": beatmap.approachRate = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "SliderMultiplier": beatmap.sliderMultiplier = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "SliderTickRate": beatmap.sliderTickRate = double.Parse(value, CultureInfo.InvariantCulture); break;
                }
            });
        }
        private static void parseTimingPointsSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseSectionLines(line => beatmap.controlPoints.Add(ControlPoint.Parse(line)));
            beatmap.controlPoints.Sort();
        }
        private static void parseColoursSection(EditorBeatmap beatmap, StreamReader reader)
        {
            beatmap.comboColors.Clear();
            reader.ParseKeyValueSection((key, value) =>
            {
                if (!key.StartsWith("Combo"))
                    return;

                var rgb = value.Split(',');
                beatmap.comboColors.Add(new Color4(byte.Parse(rgb[0]), byte.Parse(rgb[1]), byte.Parse(rgb[2]), 255));
            });
        }
        private static void parseEventsSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseSectionLines(line =>
            {
                if (line.StartsWith("//")) return;
                if (line.StartsWith(" ")) return;

                var values = line.Split(',');
                switch (values[0])
                {
                    case "0":
                        beatmap.backgroundPath = removePathQuotes(values[2]);
                        break;
                    case "2":
                        beatmap.breaks.Add(OsuBreak.Parse(beatmap, line));
                        break;
                }
            }, false);
        }
        private static void parseHitObjectsSection(EditorBeatmap beatmap, StreamReader reader)
        {
            OsuHitObject previousHitObject = null;
            var colorIndex = 0;
            var comboIndex = 0;

            reader.ParseSectionLines(line =>
            {
                var hitobject = OsuHitObject.Parse(beatmap, line);

                if (hitobject.NewCombo || previousHitObject == null || (previousHitObject.Flags & HitObjectFlag.Spinner) > 0)
                {
                    hitobject.Flags |= HitObjectFlag.NewCombo;

                    var colorIncrement = hitobject.ComboOffset;
                    if ((hitobject.Flags & HitObjectFlag.Spinner) == 0)
                        colorIncrement++;
                    colorIndex = (colorIndex + colorIncrement) % beatmap.comboColors.Count;
                    comboIndex = 1;
                }
                else comboIndex++;

                hitobject.ComboIndex = comboIndex;
                hitobject.ColorIndex = colorIndex;
                hitobject.Color = beatmap.comboColors[colorIndex];

                beatmap.hitObjects.Add(hitobject);
                previousHitObject = hitobject;
            });
        }

        private static string removePathQuotes(string path)
            => path.StartsWith("\"") && path.EndsWith("\"") ? path.Substring(1, path.Length - 2) : path;

        #endregion

        public void SaveToStream(Stream stream)
        {
            using var binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true);
            binaryWriter.Write(Path);
            binaryWriter.Write(audioFilename);
            binaryWriter.Write(name);
            binaryWriter.Write(id);
            binaryWriter.Write(bookmarks.Count);
            foreach (var bookmark in bookmarks)
            {
                binaryWriter.Write(bookmark);
            }

            binaryWriter.Write(hpDrainRate);
            binaryWriter.Write(circleSize);
            binaryWriter.Write(overallDifficulty);
            binaryWriter.Write(approachRate);
            binaryWriter.Write(sliderMultiplier);
            binaryWriter.Write(sliderTickRate);

            binaryWriter.Write(hitObjects.Count);
            foreach (var osuHitObject in hitObjects)
            {
                binaryWriter.Write(osuHitObject.RawLine);
            }

            binaryWriter.Write(comboColors.Count);
            foreach (var comboColor in comboColors)
            {
                binaryWriter.Write(comboColor.R);
                binaryWriter.Write(comboColor.G);
                binaryWriter.Write(comboColor.B);
                binaryWriter.Write(comboColor.A);
            }

            binaryWriter.Write(backgroundPath);
            binaryWriter.Write(breaks.Count);
            foreach (var osuBreak in breaks)
            {
                binaryWriter.Write(osuBreak.StartTime);
                binaryWriter.Write(osuBreak.EndTime);
            }

            binaryWriter.Write(controlPoints.Count);
            foreach (var controlPoint in controlPoints)
            {
                binaryWriter.Write(controlPoint.Offset);
                binaryWriter.Write(controlPoint.beatDurationSV);
                binaryWriter.Write(controlPoint.BeatPerMeasure);
                binaryWriter.Write((int)controlPoint.SampleSet);
                binaryWriter.Write(controlPoint.CustomSampleSet);
                binaryWriter.Write(controlPoint.Volume);
                binaryWriter.Write(controlPoint.IsInherited);
                binaryWriter.Write(controlPoint.IsKiai);
                binaryWriter.Write(controlPoint.OmitFirstBarLine);
            }
        }

        public static EditorBeatmap LoadFromStream(Stream stream)
        {
            using var binaryReader = new BinaryReader(stream, Encoding.UTF8, true);
            var path = binaryReader.ReadString();
            var editorBeatmap = new EditorBeatmap(path);
            editorBeatmap.audioFilename = binaryReader.ReadString();
            editorBeatmap.name = binaryReader.ReadString();
            editorBeatmap.id = binaryReader.ReadInt64();
            var bookmarkCount = binaryReader.ReadInt32();
            editorBeatmap.bookmarks.Capacity = bookmarkCount;
            for (int i = 0; i < bookmarkCount; i++)
            {
                editorBeatmap.bookmarks.Add(binaryReader.ReadInt32());
            }

            editorBeatmap.hpDrainRate = binaryReader.ReadDouble();
            editorBeatmap.circleSize = binaryReader.ReadDouble();
            editorBeatmap.overallDifficulty = binaryReader.ReadDouble();
            editorBeatmap.approachRate = binaryReader.ReadDouble();
            editorBeatmap.sliderMultiplier = binaryReader.ReadDouble();
            editorBeatmap.sliderTickRate = binaryReader.ReadDouble();

            var hitObjectCount = binaryReader.ReadInt32();
            editorBeatmap.hitObjects.Capacity = hitObjectCount;
            for (int i = 0; i < hitObjectCount; i++)
            {
                editorBeatmap.hitObjects.Add(OsuHitObject.Parse(editorBeatmap, binaryReader.ReadString()));
            }

            var colorCount = binaryReader.ReadInt32();
            editorBeatmap.comboColors.Clear();
            for (int i = 0; i < colorCount; i++)
            {
                editorBeatmap.comboColors.Add(binaryReader.ReadTkColor4());
            }

            editorBeatmap.backgroundPath = binaryReader.ReadString();
            var breakCount = binaryReader.ReadInt32();
            for (int i = 0; i < breakCount; i++)
            {
                editorBeatmap.breaks.Add(binaryReader.ReadOsuBreak());
            }

            var controlPointCount = binaryReader.ReadInt32();
            for (int i = 0; i < controlPointCount; i++)
            {
                editorBeatmap.controlPoints.Add(binaryReader.ReadControlPoint());
            }

            return editorBeatmap;
        }
    }
}