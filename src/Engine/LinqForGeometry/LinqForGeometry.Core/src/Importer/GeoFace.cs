﻿/*
	Author: Dominik Steffen
	E-Mail: dominik.steffen@hs-furtwangen.de, dominik.steffen@gmail.com
	Bachelor Thesis Summer Semester 2013
	'Computer Science in Media'
	Project: LinqForGeometry
	Professors:
	Mr. Prof. C. Müller
	Mr. Prof. W. Walter
*/

using System.Collections.Generic;
using Fusee.Math;

namespace Fusee.LFG.Core.Importer
{
    /// <summary>
    /// This struct is a helper 'container' to temporary save data during the import and conversion process.
    /// </summary>
    public struct GeoFace
    {
        internal List<float3> _Vertices;
        internal List<float3> _Normals;
        
        internal List<float2> _UV;
    }
}
