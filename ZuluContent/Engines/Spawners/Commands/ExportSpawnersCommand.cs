using System.Collections.Generic;
using System.IO;
using Server.Commands.Generic;
using Server.Json;
using Server.Network;

namespace Server.Engines.Spawners
{
    public class ExportSpawnersCommand : BaseCommand
    {
        public static void Initialize()
        {
            TargetCommands.Register(new ExportSpawnersCommand());
        }

        public ExportSpawnersCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllItems & ~CommandSupport.Contained;
            Commands = new[] { "ExportSpawners" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "ExportSpawners";
            Description = "Exports the given the spawners to the a file";
            ListOptimized = true;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            var path = e.Arguments.Length == 0 ? null : e.Arguments[0].Trim();

            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(Core.BaseDirectory, $"Data/Spawns/{Utility.GetTimeStamp()}.json");
            }
            else
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path!))!;
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(Core.BaseDirectory, path);
                    PathUtility.EnsureDirectory(directory);
                }
                else if (!Directory.Exists(directory))
                {
                    LogFailure("Directory doesn't exist.");
                    return;
                }
            }

            NetState.FlushAll();

            var options = JsonConfig.GetOptions(new TextDefinitionConverterFactory());

            var spawnRecords = new List<DynamicJson>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] is BaseSpawner spawner)
                {
                    var dynamicJson = DynamicJson.Create(spawner.GetType());
                    spawner.ToJson(dynamicJson, options);
                    spawnRecords.Add(dynamicJson);
                }
            }

            if (spawnRecords.Count == 0)
            {
                LogFailure("No matching spawners found.");
                return;
            }

            e.Mobile.SendMessage("Exporting spawners...");

            JsonConfig.Serialize(path, spawnRecords, options);

            e.Mobile.SendMessage($"Spawners exported to {path}");
        }
    }
}