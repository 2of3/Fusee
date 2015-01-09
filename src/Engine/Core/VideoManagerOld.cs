using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusee.Engine
{
    public class VideoManagerOld
    {
        private static VideoManagerOld _instance;
        private IVideoManagerImpOld _videoManagerImpOld;

        internal IVideoManagerImpOld VideoManagerImpOld
        {
            set { _videoManagerImpOld = value; }
        }

        public IVideoStreamImpOld LoadVideoFromFile (string filename, bool loopVideo, bool useAudio = true)
        {
            return _videoManagerImpOld.CreateVideoStreamImpFromFile(filename, loopVideo, useAudio);
        }

        public IVideoStreamImpOld LoadVideoFromCamera(int cameraIndex = 0, bool useAudio = false)
        {
            return _videoManagerImpOld.CreateVideoStreamImpFromCamera(cameraIndex, useAudio);
        }

        public static VideoManagerOld Instance
        {
            get { return _instance ?? (_instance = new VideoManagerOld()); }
        }
    }
}
