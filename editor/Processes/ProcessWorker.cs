using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrewLib.Audio;
using OpenTK.Mathematics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Mapset;
using StorybrewEditor.Scripting;
using StorybrewEditor.Storyboarding;

namespace StorybrewEditor.Processes;

public class ProcessWorker
{
    internal readonly ProcessGeneratorContext Context;
    internal readonly StoryboardObjectGenerator Generator;
    internal readonly ScriptProvider<StoryboardObjectGenerator> ScriptProvider;

    public ProcessWorker(
        string assemblyPath,
        string typeName,
        string projectPath,
        string projectAssetPath,
        string mapsetPath,
        string[] beatmapPaths,
        string currentBeatmapName)
    {
        Context = new ProcessGeneratorContext(projectPath, projectAssetPath, mapsetPath, beatmapPaths,
            currentBeatmapName);
        ScriptProvider = new ScriptProvider<StoryboardObjectGenerator>();
        ScriptProvider.Initialize(assemblyPath, typeName);
        Generator = ScriptProvider.CreateScript();
    }

    public void UpdateAndGenerate(EffectConfig effectConfig)
    {
        Generator.UpdateConfiguration(effectConfig);
        Generator.Generate(Context);
    }
}

public class LightStoryboardLayer : StoryboardLayer
{
    private readonly List<StoryboardObject> storyboardObjects = new List<StoryboardObject>();

    public LightStoryboardLayer(string identifier) : base(identifier)
    {
    }

    public override OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition)
    {
        var storyboardObject = new EditorOsbSprite()
        {
            TexturePath = path,
            Origin = origin,
            InitialPosition = initialPosition,
        };
        storyboardObjects.Add(storyboardObject);
        return storyboardObject;
    }

    public override OsbSprite CreateSprite(string path, OsbOrigin origin = OsbOrigin.Centre)
        => CreateSprite(path, origin, OsbSprite.DefaultPosition);

    public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin,
        Vector2 initialPosition)
    {
        var storyboardObject = new EditorOsbAnimation()
        {
            TexturePath = path,
            Origin = origin,
            FrameCount = frameCount,
            FrameDelay = frameDelay,
            LoopType = loopType,
            InitialPosition = initialPosition,
        };
        storyboardObjects.Add(storyboardObject);
        return storyboardObject;
    }

    public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType,
        OsbOrigin origin = OsbOrigin.Centre)
        => CreateAnimation(path, frameCount, frameDelay, loopType, origin, OsbSprite.DefaultPosition);

    public override OsbSample CreateSample(string path, double time, double volume = 100)
    {
        var storyboardObject = new EditorOsbSample()
        {
            AudioPath = path,
            Time = time,
            Volume = volume,
        };
        storyboardObjects.Add(storyboardObject);
        return storyboardObject;
    }

    public override void Discard(StoryboardObject storyboardObject)
    {
        storyboardObjects.Remove(storyboardObject);
    }
}

public class ProcessGeneratorContext : GeneratorContext
{
    private readonly List<EditorBeatmap> beatmaps;
    private readonly Dictionary<string, FftStream> fftAudioStreams = new Dictionary<string, FftStream>();
    private readonly StringBuilder log = new StringBuilder();
    private readonly Dictionary<string, StoryboardLayer> storyboardLayers = new();

    public ProcessGeneratorContext(string projectPath,
        string projectAssetPath,
        string mapsetPath,
        string[] beatmapPaths,
        string currentBeatmapName)
    {
        ProjectPath = projectPath;
        ProjectAssetPath = projectAssetPath;
        MapsetPath = mapsetPath;
        beatmaps = beatmapPaths.Select(k =>
        {
            using var fileStream = File.OpenRead(k);
            return EditorBeatmap.LoadFromStream(fileStream);
        }).ToList();
        Beatmap = beatmaps.FirstOrDefault(k => k.Name == currentBeatmapName) ?? beatmaps.FirstOrDefault();
    }

    public override double AudioDuration
        => getFftStream(Path.Combine(MapsetPath, Beatmap.AudioFilename)).Duration * 1000;

    public override Beatmap Beatmap { get; }
    public override IEnumerable<Beatmap> Beatmaps => beatmaps;
    public override string MapsetPath { get; }
    public override string ProjectAssetPath { get; }
    public override string ProjectPath { get; }

    public string GetLogs() => log.ToString();

    public override void AddDependency(string path)
    {
        throw new NotImplementedException();
    }

    public override void AppendLog(string message)
        => log.AppendLine(message);

    public override StoryboardLayer GetLayer(string identifier)
    {
        if (!storyboardLayers.TryGetValue(identifier, out var layer))
        {
            layer = new LightStoryboardLayer(identifier);
            storyboardLayers.Add(identifier, layer);
        }

        return layer;
    }

    public override float[] GetFft(double time, string path = null, bool splitChannels = false)
        => getFftStream(path ?? Path.Combine(MapsetPath, Beatmap.AudioFilename)).GetFft(time * 0.001, splitChannels);

    public override float GetFftFrequency(string path = null)
        => getFftStream(path ?? Path.Combine(MapsetPath, Beatmap.AudioFilename)).Frequency;

    private FftStream getFftStream(string path)
    {
        path = Path.GetFullPath(path);

        if (!fftAudioStreams.TryGetValue(path, out FftStream audioStream))
            fftAudioStreams[path] = audioStream = new FftStream(path);

        return audioStream;
    }
}