using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusee.LFG.Core.Importer;

namespace Fusee.LFG.Core.src.Importer
{
    interface ILfgImporter
    {
        List<GeoFace> TmpData { get; }
        String Filepath { get; }

        /// <summary>
        /// This loads the asset specified by the path to the memory for further use.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns a List of GeoFace</returns>
        List<GeoFace> LoadAsset(String path);
    }
}
