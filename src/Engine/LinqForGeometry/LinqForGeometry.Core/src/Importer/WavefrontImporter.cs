/*
	Author: Dominik Steffen
	E-Mail: dominik.steffen@hs-furtwangen.de, dominik.steffen@gmail.com
	Bachelor Thesis Summer Semester 2013
	'Computer Science in Media'
	Project: LinqForGeometry
	Professors:
	Mr. Prof. C. Müller
	Mr. Prof. W. Walter
*/

using System;
using System.Collections.Generic;
using System.IO;
using Fusee.Engine;
using Fusee.LFG.Core.Handles;
using Fusee.LFG.Core.src.Importer;
using Fusee.Math;

namespace Fusee.LFG.Core.Importer
{
    /// <summary>
    /// This is an importer for the <a href="http://en.wikipedia.org/wiki/Wavefront_.obj_file">Wavefront obj</a> computer graphics file
    /// To use it just create an instance and pass the file path to the LoadAsset() method
    /// </summary>
    class WavefrontImporter : ILfgImporter
    {
        // Interface related
        private String _FilePath;
        public List<GeoFace> TmpData
        {
            get { return _LgeoFaces; }
        }
        public string Filepath
        {
            get { return _FilePath; }
        }

        // Sytem File related
        private String _AssetFileContent;
        private String[] _EndOfLine = { "\n" };
        // GeometryData related
        internal List<GeoFace> _LgeoFaces;
        internal List<float2> _LuvCoords;
        private List<KeyValuePair<int, int>> _LKVuvandvert;
        List<float3> LvertexAttr = new List<float3>();
        List<String> LgeoValues = new List<string>();
        // String Handling
        String[] splitChar = { " " };
        String[] splitChar2 = { "/" };

        public WavefrontImporter()
        {
            // File related
            _AssetFileContent = "";

            // GeometryData related
            _LgeoFaces = new List<GeoFace>();
            _LuvCoords = new List<float2>();
            _LKVuvandvert = new List<KeyValuePair<int, int>>();
        }

        /// <summary>
        /// Loads an asset from file to the memory.
        /// </summary>
        /// <param name="path"></param>
        public List<GeoFace> LoadAsset(String path)
        {
            LoadFileToMemory(path);

            foreach (String line in LgeoValues)
            {

                if (line.StartsWith("vt"))
                {
                    // vertex texture
                    HandleVertexTextureCoordinates(line);
                }
                else if (line.StartsWith("vn"))
                {
                    // vertex normals
                    HandleVertexNormal(line);
                }
                else if (line.StartsWith("v"))
                {
                    // vertex
                    HandleVertex(line);
                }
                else if (line.StartsWith("p"))
                {
                    // point
                    HandlePoint(line);
                }
                else if (line.StartsWith("l"))
                {
                    // line
                    HandleLine(line);
                }
                else if (line.StartsWith("f"))
                {
                    HandleFace(line);
                }
                else if (line.StartsWith("g"))
                {
                    HandleGroup(line);
                }
                else if (line.StartsWith("usemtl"))
                {
                    // use material
                    HandleUseMaterial(line);
                }
                else if (line.StartsWith("usemtllib"))
                {
                    // material lib
                    HandleMaterialLib(line);
                }
            }

            if (_LgeoFaces == null)
            {
                throw new Exception("The content of the file did not match the wavefront.obj specifications this importer requires.");
            }

            // Some clean ups.
            _LuvCoords = null;
            _LKVuvandvert = null;
            LvertexAttr = null;
            LgeoValues = null;

            return _LgeoFaces;
        }

        private void LoadFileToMemory(String path)
        {
            _AssetFileContent = "";

            List<String> LvertexHelper = new List<string>();
            List<String> LfaceHelper = new List<string>();          
            // Load the file information to the memory
            if (File.Exists(path))
            {
                _FilePath = path;
                using (var assetFile = new StreamReader(path))
                {
                    _AssetFileContent = assetFile.ReadToEnd();
                }

                string[] contentAsLines = _AssetFileContent.Split(_EndOfLine, StringSplitOptions.None);

                List<String> LfileLines = new List<string>();
                foreach (string line in contentAsLines)
                {
                    if (!line.StartsWith("#"))
                    {
                        LfileLines.Add(line);
                    }
                }
                LgeoValues = LfileLines;
            }
            else
            {
                throw new Exception("The file that should be loaded is not present or corrupt. Please check the file.");
            }
        }

        private void HandleVertexTextureCoordinates(String line)
        {
            string[] lineSplitted = line.Split(splitChar, StringSplitOptions.None);

            // vertex texture coordinates
            if (LFGMessages._DEBUGOUTPUT)
            {
                Console.WriteLine(LFGMessages.INFO_UVFOUND + line);
            }

            List<Double> tmpSave = new List<double>();
            foreach (string str in lineSplitted)
            {
                if (!str.StartsWith("vt") && !str.Equals(""))
                {
                    tmpSave.Add(MeshReader.Double_Parse(str));
                }
            }
            float2 uvVal = new float2(
                (float)tmpSave[0],
                (float)tmpSave[1]
                );
            _LuvCoords.Add(uvVal);
        }

        private void HandleVertexNormal(String line)
        {
        }

        private void HandleVertex(String line)
        {
            string[] lineSplitted = line.Split(splitChar, StringSplitOptions.None);
            List<Double> tmpSave = new List<double>();
            foreach (string str in lineSplitted)
            {
                if (!str.StartsWith("v") && !str.Equals(""))
                {
                    tmpSave.Add(MeshReader.Double_Parse(str));
                }
            }
            float3 fVal = new float3(
                    (float)tmpSave[0],
                    (float)tmpSave[1],
                    (float)tmpSave[2]
            );
            LvertexAttr.Add(fVal);
        }

        private void HandlePoint(String line)
        {
            
        }

        private void HandleLine(String line)
        {

        }

        private void HandleFace(String line)
        {
            // there are faces, faces with texture coord, faces with vertex normals and faces with text and normals
            if (LFGMessages._DEBUGOUTPUT)
            {
                Console.WriteLine(LFGMessages.INFO_FACEFOUND + line);
            }
            string[] lineSplitted = line.Split(splitChar, StringSplitOptions.None);
            List<Double> tmpSave = new List<double>();

            GeoFace geoF = new GeoFace();
            geoF._Vertices = new List<float3>();
            geoF._UV = new List<float2>();
            foreach (string str in lineSplitted)
            {
                if (!str.StartsWith("f"))
                {
                    string[] faceSplit = str.Split(splitChar2, StringSplitOptions.None);
                    string s = faceSplit[0];

                    if (LFGMessages._DEBUGOUTPUT)
                    {
                        Console.WriteLine(LFGMessages.INFO_VERTEXIDFORFACE + s);
                    }
                    //if (s != null || s != "" || !s.Equals("") || !s.Equals(" ") || s != " " || !s.Equals("\n") || s != "\n" || s != "\0" || !s.Equals("\0") || !s.Equals("\r") || s != "\r")
                    if (!s.Equals("\r"))
                    {
                        try
                        {
                            int fv = Convert.ToInt32(s);
                            geoF._Vertices.Add(LvertexAttr[fv - 1]);

                            if (faceSplit.Length >= 1)
                            {
                                string uvIndex = faceSplit[1];
                                int uvAdress = Convert.ToInt32(uvIndex);
                                geoF._UV.Add(_LuvCoords[uvAdress - 1]);
                                _LKVuvandvert.Add(new KeyValuePair<int, int>(uvAdress - 1, fv - 1));
                            }
                        }
                        catch (FormatException e)
                        {
                            if (LFGMessages._DEBUGOUTPUT)
                            {
                                Console.WriteLine(LFGMessages.WARNING_INVALIDCHAR + s + e);
                            }
                            //Debug.WriteLine(e.StackTrace);
                            continue;
                        }
                    }
                }
            }
            _LgeoFaces.Add(geoF);
        }

        private void HandleMaterialLib(String line)
        {
        }

        private void HandleUseMaterial(String line)
        {
        }

        private void HandleGroup(String line)
        {
        }

    }
}
