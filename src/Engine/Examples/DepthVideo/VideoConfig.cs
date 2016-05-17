using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Fusee.Math;

namespace Examples.DepthVideo
{
    public class VideoConfig
    {
        public string Name;
        public string VideoDirectory;
        public string LeftVideoRgb;
        public string LeftVideoDepth;
        public string RightVideoRgb;
        public string RightVideoDepth;
        public int FrameCount;
        public float Hit;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float ScalePlane;
        public float DepthScale;
    }
}
