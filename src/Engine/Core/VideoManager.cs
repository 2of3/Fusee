using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusee.Engine
{
    public class VideoManager
    {
        private static VideoManager _instance;
        private IVideoManagerImp _videoManagerImp;

        internal IVideoManagerImp VideoManagerImp
        {
            set { _videoManagerImp = value; }
        }

        public IVideoStreamImp LoadVideoFromFile (string filename, bool loopVideo, bool useAudio = true)
        {
            return _videoManagerImp.CreateVideoStreamImpFromFile(filename, loopVideo, useAudio);
        }

        public IVideoStreamImp LoadVideoFromCamera(int cameraIndex = 0, bool useAudio = false)
        {
            return _videoManagerImp.CreateVideoStreamImpFromCamera(cameraIndex, useAudio);
        }

        internal void Dispose()
        {
            _instance._videoManagerImp.Dispose();
            _instance = null;
        }

        public static VideoManager Instance
        {
            get { return _instance ?? (_instance = new VideoManager()); }
        }
    }
}
