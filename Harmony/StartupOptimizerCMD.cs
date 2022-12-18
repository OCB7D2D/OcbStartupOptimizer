using System;
using System.Collections.Generic;
using System.IO;

public class StartupOptimizerCMD : ConsoleCmdAbstract
{

    private static readonly string info = "StartupOptimizer";
    public override string[] GetCommands()
    {
        return new string[2] { info, "optimizer" };
    }

    public override bool IsExecuteOnClient => false;
    public override bool AllowedInMainMenu => true;

    public override string GetDescription() => "Startup Optimizer Helpers";

    public override string GetHelp() => "Use this to cleanup after optimization was successful\n";

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {

        if (_params.Count == 1 && _params[0] == "cleanup")
        {

            var bundles = StartupOptimizer.GetAllAssetBundles();
            long size_before = 0; long size_after = 0; var removed = 0;

            foreach (var bundle in bundles)
            {
                try
                {
                    if (!File.Exists(bundle + ".org")) continue;
                    size_before += new FileInfo(bundle + ".org").Length;
                    size_after += new FileInfo(bundle).Length;
                    Log.Out("Deleting {0}", bundle + ".org");
                    File.Delete(bundle + ".org");
                    removed += 1;
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to delete {0}", bundle + ".org");
                    Log.Warning("Error given: {0}", ex.Message);
                }
            }

            if (removed == 0)
            {
                Log.Out("Nothing to clean up, all good!");
            }
            else
            {
                Log.Out("Removed {0} asset bundle backups", removed);
                string before = FormatSize(size_before);
                string after = FormatSize(size_after);
                Log.Out("Asset Bundle sizes before optimizer: {0}", before);
                Log.Out("Asset Bundle sizes after optimizer: {0}", after);
            }

        }
        else
        {
            Log.Out("Only `cleanup` command is available");
        }

    }

    // Load all suffixes in an array  
    static readonly string[] suffixes =
        { "Bytes", "KB", "MB", "GB", "TB", "PB" };
    public static string FormatSize(Int64 bytes)
    {
        int counter = 0;
        decimal number = (decimal)bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number = number / 1024;
            counter++;
        }
        return string.Format("{0:n1}{1}", number, suffixes[counter]);
    }

}
