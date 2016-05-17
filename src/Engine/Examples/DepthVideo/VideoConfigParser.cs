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
        public static List<VideoConfig> ParseConfigs(string path)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine("No valid Path");
                return default(List<VideoConfig>);
            }

            var configFilePaths = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
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
                            videoConfig.VideoDirectory = path;
                        }
                        videoConfigs.Add(videoConfig);
                    }
                }
            }
            return videoConfigs;
        }
    }
}
