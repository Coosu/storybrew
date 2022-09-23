
using StorybrewEditor.Mapset;
using StorybrewEditor.Processes;

var processWorker = new ProcessWorker(
    @"D:\GitHub\storybrew_coosu\editor\bin\Debug\net6.0-windows\cache\scripts2\1511c37f-5676-4199-96d4-6542e4ad0d3e.dll",
    "StorybrewScripts.Aaaa",
    @"D:\GitHub\storybrew_coosu\editor\bin\Debug\net6.0-windows\projects\asdf",
    @"D:\GitHub\storybrew_coosu\editor\bin\Debug\net6.0-windows\projects\asdf\assetlibrary",
    @"C:/Users/milkitic/AppData/Local/osu!/Songs/16223 ZUN - Dark Side of Fate",
    new[]
    {
        @"D:\GitHub\storybrew_coosu\editor\bin\Debug\net6.0-windows\cache\mapsets\ZUN - Dark Side of Fate (Muya) [Easy].bin",
        @"D:\GitHub\storybrew_coosu\editor\bin\Debug\net6.0-windows\cache\mapsets\ZUN - Dark Side of Fate (Muya) [Normal].bin",
        @"D:\GitHub\storybrew_coosu\editor\bin\Debug\net6.0-windows\cache\mapsets\ZUN - Dark Side of Fate (Muya) [Hard].bin",
        @"D:\GitHub\storybrew_coosu\editor\bin\Debug\net6.0-windows\cache\mapsets\ZUN - Dark Side of Fate (Muya) [Lunatic].bin",
    },
    @"Lunatic");

//processWorker.UpdateAndGenerate();


var beatmap = EditorBeatmap.Load(@"F:\milkitic\Songs\1376486 Risshuu feat. Choko - Take\Risshuu feat. Choko - Take (yf_bmp) [Ta~ke take take take take take tatata~].osu");

using var ms = new MemoryStream();
beatmap.SaveToStream(ms);
ms.Position = 0;
var beatmap2 = EditorBeatmap.LoadFromStream(ms);

var arr = ms.ToArray();
File.WriteAllBytes("BeatmapSerialization.bin", arr);
Console.ReadKey(true);