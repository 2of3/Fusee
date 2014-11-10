using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fusee.LFG.Core.Importer;
using Fusee.Engine;
using Fusee.Math;

namespace Fusee.LFG.Core.src.Importer
{
    class FusContainerImporter : ILfgImporter
    {
        private List<GeoFace> _GeoFaces;
        private String _Path = "";
        private FuseeSerializer _Serializer;
        private Mesh _Mesh;

        private List<float3> _Vertices;
        private List<float2> _UVList;
        private List<float3> _Normals;

        public List<GeoFace> TmpData { get { return _GeoFaces; } }
        public string Filepath { get { return _Path; } }

        
        public List<GeoFace> LoadAsset(string path)
        {
            _Path = path;

        }

        private void LoadProtobufFile(string path)
        {
            using (var file = File.OpenRead(path))
            {
                _Mesh = _Serializer.Deserialize(file, null, typeof(Mesh)) as Mesh;
            }

            ConvertMesh();
        }

        private void ConvertMesh()
        {
            foreach (var vertice in _Mesh.Vertices)
            {
                
            }
        }
    }
}
