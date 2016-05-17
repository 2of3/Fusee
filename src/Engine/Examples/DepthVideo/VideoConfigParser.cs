using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Examples.DepthVideo
{
    public static class VideoConfigParser
    {
        public static List<VideoConfig> ParseConfigs()
        {
            if (!Directory.Exists("Assets/S3DVideos"))
            {
                Console.WriteLine("No valid Path");
                return default(List<VideoConfig>);
            }

            var configFilePaths = Directory.GetFiles("Assets/S3DVideos", "*.json", SearchOption.TopDirectoryOnly);
            var videoConfigs = new List<VideoConfig>();
            var serializer = new JsonSerializer();
            foreach (var configFilePath in configFilePaths)
            {
                using (var streamReader = new StreamReader(configFilePath))
                {
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        var videoConfig = serializer.Deserialize<VideoConfig>(jsonReader);
                        if (videoConfig.VideoDirectory == string.Empty)
                        {
                            videoConfig.VideoDirectory = "Assets/S3DVideos";
                        }
                        videoConfigs.Add(videoConfig);
                    }
                }
            }
            return videoConfigs;
        }

        public static void WriteConfigToDisk(VideoConfig config)
        {
            var fileName = "Assets/S3DVideos/" + config.Name + ".json";
            if (!File.Exists(fileName))
            {
                    Console.WriteLine("ConfigFile doesn't exist - Wrong Path?");
            }
            var serializer = new JsonSerializer();
            using (var streamWriter = new StreamWriter(fileName))
            {
                using (var jsonWriter = new JsonTextWriter(streamWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    serializer.Serialize(jsonWriter, config);
                }
            }
        }
    }
}
