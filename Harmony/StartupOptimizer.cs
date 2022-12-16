using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Extra.Decompressors.LZ4;
using HarmonyLib;
using SevenZip.Compression.LZMA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;

public class StartupOptimizer : IModApi
{

    private static bool FileIsAssetBundle(string path)
    {
        try
        {
            if (Path.GetExtension(path) == ".unity3d")
                return true;

            byte[] buffer = new byte[7];
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
            }
            return System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length) == "UnityFS";
        }
        catch { return false; }
    }

    public static bool ContainsLZMA(AssetBundleFile bundle)
    {
        AssetsFileReader reader = bundle.reader;
        reader.Position = 0L;
        if (bundle.Read(reader, allowCompressed: true))
        {
            reader.Position = bundle.bundleHeader6.GetBundleInfoOffset();
            int compressedSize = (int)bundle.bundleHeader6.compressedSize;
            int decompressedSize = (int)bundle.bundleHeader6.decompressedSize;

            MemoryStream memoryStream;
            switch (bundle.bundleHeader6.GetCompressionType())
            {
                case 1:
                    using (MemoryStream compressedStream = new MemoryStream(reader.ReadBytes(compressedSize)))
                    {
                        memoryStream = new MemoryStream();
                        SevenZipHelper.StreamDecompress(compressedStream,
                            memoryStream, compressedSize, decompressedSize);
                    }
                    break;
                case 2:
                case 3:
                    byte[] buffer = new byte[bundle.bundleHeader6.decompressedSize];
                    using (MemoryStream input = new MemoryStream(reader.ReadBytes(compressedSize)))
                    {
                        Lz4DecoderStream lz4DecoderStream = new Lz4DecoderStream(input);
                        lz4DecoderStream.Read(buffer, 0, (int)bundle.bundleHeader6.decompressedSize);
                        lz4DecoderStream.Dispose();
                    }
                    memoryStream = new MemoryStream(buffer);
                    break;
                default:
                    memoryStream = null;
                    break;
            }

            if (bundle.bundleHeader6.GetCompressionType() != 0)
            {
                AssetsFileReader assetsFileReader;
                using (assetsFileReader = new AssetsFileReader(memoryStream))
                {
                    assetsFileReader.Position = 0L;
                    bundle.bundleInf6.Read(0L, assetsFileReader);
                }
            }

            reader.Position = bundle.bundleHeader6.GetFileDataOffset();
            for (int l = 0; l < bundle.bundleInf6.blockCount; l++)
            {
                if (bundle.bundleInf6.blockInf[l].GetCompressionType() == 1) return true;
            }

            return false;
        }

        return false;
    }

    public void InitMod(Mod mod)
    {
        Log.Out(" Loading Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        List<Tuple<AssetBundleRecompressOperation,long>> tasks =
            new List<Tuple<AssetBundleRecompressOperation, long>>();

        int AssetCount = 0;

        foreach (Mod other in ModManager.GetLoadedMods())
        {
            CollectOptimizationTasks(Path.Combine(
                other.Path, "Resources"),
                tasks, ref AssetCount);
            CollectOptimizationTasks(Path.Combine(
                other.Path, "Assets"),
                tasks, ref AssetCount);
        }

        if (tasks.Count == 0)
        {
            Log.Out("Found {0} optimized asset, all good!", AssetCount);
            return;
        }

        Log.Out("Start optimization of {0} bundles", tasks.Count);

        while (true)
        {
            long sizes = 0;
            float progress = 0;
            var waiting = tasks.Count;
            foreach (var op in tasks)
            {
                sizes += op.Item2;
                progress += op.Item2 * op.Item1.progress;
                if (op.Item1.isDone) waiting -= 1;
            }
            if (waiting == 0) break;
            Log.Out(" optimization pending {1}/{2} ({0:0.00}%)",
                100 * progress / sizes, waiting, tasks.Count);
            Thread.Sleep(400);
        }

        Log.Out("Optimization has finished");

        // Alternative to complete callback
        // foreach (var op in tasks) {}

    }   

    private void CollectOptimizationTasks(string path, List<Tuple<AssetBundleRecompressOperation, long>> ops, ref int AssetCount)
    {
        if (Directory.Exists(path) == false) return;
        foreach (var file in Directory.GetFiles(path))
        {
            if (file.EndsWith(".tmp")) continue;
            if (file.EndsWith(".org")) continue;
            if (file.EndsWith(".bac")) continue;
            if (file.EndsWith(".fast")) continue;
            if (!FileIsAssetBundle(file)) continue;
            try
            {
                AssetCount += 1;
                AssetsManager assetsManager = new AssetsManager();
                BundleFileInstance bundle = assetsManager.LoadBundleFile(file, false);
                if (!ContainsLZMA(bundle.file)) continue;
                // Log.Out("Collected {0}", file);
                if (File.Exists(file + ".fast"))
                {
                    // Log.Out("Remove old {0}.fast", file);
                    File.Delete(file + ".fast");
                }
                if (File.Exists(file + ".fast.tmp"))
                {
                    // Log.Out("Remove old {0}.fast.tmp", file);
                    File.Delete(file + ".fast.tmp");
                }
                // Log.Out("Close asset bundle reader");
                bundle.file.Close();
                // Log.Out("Starting async bundle recompression");
                var task = new Tuple<AssetBundleRecompressOperation, long>(
                    AssetBundle.RecompressAssetBundleAsync(file,
                        file + ".fast", BuildCompression.LZ4Runtime),
                    bundle.file.bundleHeader6.totalFileSize);
                // Register the callback for when complete
                // Runs a tad bit later than actually expected!?
                task.Item1.completed += (AsyncOperation obj) =>
                {
                    if (task.Item1.result != AssetBundleLoadResult.Success)
                    {
                        Log.Error("Error optimizing {0}", task.Item1.inputPath);
                    }
                    else try
                    {
                        // Move original to backup, if it doesn't exists yet
                        if (File.Exists(task.Item1.inputPath + ".org") == false)
                        {
                            // Log.Out("Moving original to backup location");
                            File.Move(task.Item1.inputPath, task.Item1.inputPath + ".org");
                        }
                        // Otherwise remove the compressed file
                        else
                        {
                            // Log.Warning("Removing original resource");
                            File.Delete(task.Item1.inputPath);
                        }
                        // Move the faster resource file in place now
                        // Log.Out("Moving compressed file into place");
                        File.Move(task.Item1.outputPath, task.Item1.inputPath);
                        Log.Out("Optimized {0}", task.Item1.inputPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error with {0}", task.Item1.inputPath);
                        Log.Error("  result is {0}", task.Item1.result);
                        if (File.Exists(task.Item1.outputPath))
                            File.Delete(task.Item1.outputPath);
                        Log.Error(ex.Message);
                    }
                };
                ops.Add(task);
            }
            catch (Exception ex)
            {
                Log.Error("Error optimizing {0}", file);
                Log.Error(ex.Message.ToString());
            }
        }

    }

}
