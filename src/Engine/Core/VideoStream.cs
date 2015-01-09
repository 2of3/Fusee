using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Fusee.Engine
{
    public class VideoStream
    {
        public IVideoStreamImpOld ImpOld;

        public ImageData GetCurrentFrame ()
        {
            return ImpOld.GetCurrentFrame();
        }

        public void Start()
        {
            ImpOld.Start();
        }

        public void Stop ()
        {
            ImpOld.Stop();
        }

    }
}
