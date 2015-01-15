using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Fusee.Engine
{
    public class VideoStream
    {
        public IVideoStreamImp Imp;

        public ImageData GetCurrentFrame ()
        {
            return Imp.GetCurrentFrame();
        }

        public void Play()
        {
            Imp.Play();
        }

        public void Stop ()
        {
            Imp.Stop();
        }

    }
}
