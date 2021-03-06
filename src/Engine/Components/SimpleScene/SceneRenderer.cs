﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

using System.Linq;
using System.Threading;
using Fusee.Engine;using Fusee.KeyFrameAnimation;
using Fusee.Math;
using Fusee.Serialization;
namespace Fusee.Engine.SimpleScene
{

    /// <summary>
    /// Axis-Aligned Bounding Box Calculator. Use instances of this class to calculate axis-aligned bounding boxes
    /// on scenes, list of scene nodes or individual scene nodes. Calculations always include any child nodes.
    /// </summary>
    public class AABBCalculator : SceneVisitor
    {
        public class AABBState : VisitorState
        {
            private CollapsingStateStack<float4x4> _modelView = new CollapsingStateStack<float4x4>();

            public float4x4 ModelView
            {
                set { _modelView.Tos = value; }
                get { return _modelView.Tos; }
            }
            public AABBState()
            {
                RegisterState(_modelView);
            }
        }

        //private SceneContainer _sc;
        private IEnumerable<SceneNodeContainer> _sncList;
        private AABBState _state = new AABBState();
        private bool _boxValid;
        private AABBf _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="AABBCalculator"/> class.
        /// </summary>
        /// <param name="sc">The scene container to calculate an axis-aligned bounding box for.</param>
        public AABBCalculator(SceneContainer sc)
        {
            _sncList = sc.Children;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AABBCalculator"/> class.
        /// </summary>
        /// <param name="sncList">The list of scene nodes to calculate an axis-aligned bounding box for.</param>
        public AABBCalculator(IEnumerable<SceneNodeContainer> sncList)
        {
            _sncList = sncList;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AABBCalculator"/> class.
        /// </summary>
        /// <param name="snc">A single scene node to calculate an axis-aligned bounding box for.</param>
        public AABBCalculator(SceneNodeContainer snc)
        {
            _sncList = SceneVisitorHelpers.SingleRootEnumerable(snc);
        }

        /// <summary>
        /// Performs the calculation and returns the resulting box on the object(s) passed in the constructor. Any calculation
        /// always includes a full traversal over all child nodes.
        /// </summary>
        /// <returns>The resulting axis-aligned bounding box.</returns>
        public AABBf? GetBox()
        {
            Traverse(_sncList);
            if (_boxValid)
                return _result;
            return null;
        }

        #region Visitors
        /// <summary>
        /// Do not call. Used for internal traversal purposes only
        /// </summary>
        /// <param name="transform">The transform component.</param>
        [VisitMethod]
        public void OnTransform(TransformComponent transform)
        {
            _state.ModelView *= transform.Matrix();
        }

        /// <summary>
        /// Do not call. Used for internal traversal purposes only
        /// </summary>
        /// <param name="meshComponent">The mesh component.</param>
        [VisitMethod]
        public void OnMesh(MeshComponent meshComponent)
        {
            AABBf box = _state.ModelView * meshComponent.BoundingBox;
            if (!_boxValid)
            {
                _result = box;
                _boxValid = true;
            }
            else
            {
                _result = AABBf.Union((AABBf) _result, box);
            }
        }
        #endregion

        #region HierarchyLevel
        protected override void InitState()
        {
            _boxValid = false;
            _state.Clear();
            _state.ModelView = float4x4.Identity;
        }

        protected override void PushState()
        {
            _state.Push();
        }

        protected override void PopState()
        {
            _state.Pop();
        }
        #endregion
    }


    class LightInfo // Todo: TBD...
    {
    }


    /// <summary>
    /// Use a Scene Renderer to traverse a scene hierarchy (made out of scene nodes and components) in order
    /// to have each visited element contribute to the result rendered against a given render context.
    /// </summary>
    public class SceneRenderer : SceneVisitor
    {

        #region Traversal information
        private Dictionary<MeshComponent, Mesh> _meshMap;
        private Dictionary<MaterialComponent, ShaderEffect> _matMap;
        private Dictionary<SceneNodeContainer, float4x4> _boneMap;
        private Animation _animation;
        private SceneContainer _sc;

        private RenderContext _rc;
        private List<LightInfo> _lights;

        private string _scenePathDirectory;
        private ShaderEffect _defaultEffect;
        #endregion

        #region State
        public class RendererState : VisitorState
        {

            private CollapsingStateStack<float4x4> _model = new CollapsingStateStack<float4x4>();
            public float4x4 Model
            {
                set { _model.Tos = value; }
                get { return _model.Tos; }
            }

            private StateStack<ShaderEffect> _effect = new StateStack<ShaderEffect>();
            public ShaderEffect Effect
            {
                set { _effect.Tos = value; }
                get { return _effect.Tos; }
            }

            public RendererState()
            {
                RegisterState(_model);
                RegisterState(_effect);
            }
        };

        private RendererState _state;
        private float4x4 _view;

        #endregion

        #region Initialization Construction Startup
        public SceneRenderer(SceneContainer sc, string scenePathDirectory)
        {
            _lights = new List<LightInfo>();
            _sc = sc;
            _scenePathDirectory = scenePathDirectory;
            _state = new RendererState();
            InitAnimations(_sc);
        }
        public void InitAnimations(SceneContainer sc)
        {
            _animation = new Animation();

            foreach (AnimationComponent ac in sc.Children.FindComponents<AnimationComponent>(c => true))
            {
                if (ac.AnimationTracks != null)
                {
                    foreach (AnimationTrackContainer animTrackContainer in ac.AnimationTracks)
                    {
                        Type t = animTrackContainer.KeyType;
                        if (typeof(int).IsAssignableFrom(t))
                        {
                            Channel<int> channel = new Channel<int>(Lerp.IntLerp);
                            foreach (AnimationKeyContainerInt key in animTrackContainer.KeyFrames)
                            {
                                channel.AddKeyframe(new Keyframe<int>(key.Time, key.Value));
                            }
                            _animation.AddAnimation(channel, animTrackContainer.SceneComponent,
                                animTrackContainer.Property);
                        }
                        else if (typeof(float).IsAssignableFrom(t))
                        {
                            Channel<float> channel = new Channel<float>(Lerp.FloatLerp);
                            foreach (AnimationKeyContainerFloat key in animTrackContainer.KeyFrames)
                            {
                                channel.AddKeyframe(new Keyframe<float>(key.Time, key.Value));
                            }
                            _animation.AddAnimation(channel, animTrackContainer.SceneComponent,
                                animTrackContainer.Property);
                        }
                        else if (typeof(float2).IsAssignableFrom(t))
                        {
                            Channel<float2> channel = new Channel<float2>(Lerp.Float2Lerp);
                            foreach (AnimationKeyContainerFloat2 key in animTrackContainer.KeyFrames)
                            {
                                channel.AddKeyframe(new Keyframe<float2>(key.Time, key.Value));
                            }
                            _animation.AddAnimation(channel, animTrackContainer.SceneComponent,
                                animTrackContainer.Property);
                        }
                        else if (typeof(float3).IsAssignableFrom(t))
                        {
                            Channel<float3>.LerpFunc lerpFunc;
                            switch (animTrackContainer.LerpType)
                            {
                                case LerpType.Lerp:
                                    lerpFunc = Lerp.Float3Lerp;
                                    break;
                                case LerpType.Slerp:
                                    lerpFunc = Lerp.Float3QuaternionSlerp;
                                    break;
                                default:
                                    // C# 6throw new InvalidEnumArgumentException(nameof(animTrackContainer.LerpType), (int)animTrackContainer.LerpType, typeof(LerpType));
                                    throw new InvalidEnumArgumentException("animTrackContainer.LerpType", (int)animTrackContainer.LerpType, typeof(LerpType));
                            }
                            Channel<float3> channel = new Channel<float3>(lerpFunc);
                            foreach (AnimationKeyContainerFloat3 key in animTrackContainer.KeyFrames)
                            {
                                channel.AddKeyframe(new Keyframe<float3>(key.Time, key.Value));
                            }
                            _animation.AddAnimation(channel, animTrackContainer.SceneComponent,
                                animTrackContainer.Property);
                        }
                        else if (typeof(float4).IsAssignableFrom(t))
                        {
                            Channel<float4> channel = new Channel<float4>(Lerp.Float4Lerp);
                            foreach (AnimationKeyContainerFloat4 key in animTrackContainer.KeyFrames)
                            {
                                channel.AddKeyframe(new Keyframe<float4>(key.Time, key.Value));
                            }
                            _animation.AddAnimation(channel, animTrackContainer.SceneComponent,
                                animTrackContainer.Property);
                        }
                        //TODO : Add cases for each type
                    }
                }
            }
        }

        public void Animate()
        {
            if (_animation.ChannelBaseList.Count != 0)
                _animation.Animate();
        }

        public void SetContext(RenderContext rc)
        {
            if (rc == null)
                throw new ArgumentNullException("rc");
            
            if (rc != _rc)
            {
                _rc = rc;
                _meshMap = new Dictionary<MeshComponent, Mesh>();
                _matMap = new Dictionary<MaterialComponent, ShaderEffect>();
                _boneMap = new Dictionary<SceneNodeContainer, float4x4>();
                _defaultEffect = MakeMaterial(new MaterialComponent
                {
                    Diffuse = new MatChannelContainer()
                    {
                        Color = new float3(0.5f, 0.5f, 0.5f)
                    },
                    Specular = new SpecularChannelContainer()
                    {
                        Color = new float3(1, 1, 1),
                        Intensity = 0.5f,
                        Shininess = 22
                    }
                });
                _defaultEffect.AttachToContext(_rc);
            }
        }
        #endregion

        public void Render(RenderContext rc)
        {
            SetContext(rc);
            Traverse(_sc.Children);
        }

        #region Visitors

        [VisitMethod]
        public void RenderBone(BoneComponent bone)
        {
            SceneNodeContainer boneContainer = CurrentNode;
            float4x4 transform;
            if (!_boneMap.TryGetValue(boneContainer, out transform))
                _boneMap.Add(boneContainer, _rc.Model);
            else
                _boneMap[boneContainer] = _rc.Model;
        }

        [VisitMethod]
        public void RenderWeight(WeightComponent weight)
        {
            float4x4[] boneArray = new float4x4[weight.Joints.Count()];
            for (int i = 0; i < weight.Joints.Count(); i++)
            {
                float4x4 tmp = weight.BindingMatrices[i];
                boneArray[i] = _boneMap[weight.Joints[i]] * tmp;
            }
            _rc.Bones = boneArray;
        }


        [VisitMethod]
        public void RenderTransform(TransformComponent transform)
        {
            _state.Model *= transform.Matrix();
            _rc.Model = _view * _state.Model;
        }

        [VisitMethod]
        public void RenderMaterial(MaterialComponent matComp)
        {
            var effect = LookupMaterial(matComp);
            _state.Effect = effect;
        }

        
        [VisitMethod]
        public void RenderMesh(MeshComponent meshComponent)
        {
            Mesh rm;
            if (!_meshMap.TryGetValue(meshComponent, out rm))
            {
                rm = MakeMesh(meshComponent);
                _meshMap.Add(meshComponent, rm);
            }

            if (null != _state.Effect.GetEffectParam(ShaderCodeBuilder.LightDirectionName))
            {
                RenderWithLights(rm, _state.Effect);
            }
            else
            {
                _state.Effect.RenderMesh(rm);
            }
        }
        #endregion

        #region HierarchyLevel
        protected override void InitState()
        {
            _state.Clear();
            _state.Model = float4x4.Identity;
            _view = _rc.ModelView;

            _state.Effect = _defaultEffect;
        }

        protected override void PushState()
        {
            _state.Push();
        }

        protected override void PopState()
        {
            _state.Pop();
            _rc.ModelView = _view * _state.Model;
        }
        #endregion


        private void RenderWithLights(Mesh rm, ShaderEffect effect)
        {
            if (_lights.Count > 0)
            {
                foreach (LightInfo li in _lights)
                {
                    // SetupLight(li);
                    effect.RenderMesh(rm);
                }
            }
            else
            {
                // No light present - switch on standard light
                effect.SetEffectParam(ShaderCodeBuilder.LightColorName, new float3(1, 1, 1));
                // float4 lightDirHom = new float4(0, 0, -1, 0);
                float4 lightDirHom = _rc.InvModelView * new float4(0, 0, -1, 0);
                // float4 lightDirHom = _rc.TransModelView * new float4(0, 0, -1, 0);
                float3 lightDir = lightDirHom.xyz;
                lightDir.Normalize();
                effect.SetEffectParam(ShaderCodeBuilder.LightDirectionName, lightDir);
                effect.SetEffectParam(ShaderCodeBuilder.LightIntensityName, (float)1);
                effect.RenderMesh(rm);
            }
        }




        #region RenderContext/Asset Setup
        private ShaderEffect LookupMaterial(MaterialComponent mc)
        {
            ShaderEffect mat;
            if (!_matMap.TryGetValue(mc, out mat))
            {
                mat = MakeMaterial(mc);
                mat.AttachToContext(_rc);
                _matMap.Add(mc, mat);
            }
            return mat;
        }

        public Mesh MakeMesh(MeshComponent mc)
        {
            WeightComponent wc = CurrentNode.GetWeights();
            Mesh rm;
            if (wc == null)
            {
                rm = new Mesh()
                {
                    Colors = null,
                    Normals = mc.Normals,
                    UVs = mc.UVs,
                    Vertices = mc.Vertices,
                    Triangles = mc.Triangles
                };
            }
            else // Create Mesh with weightdata
            {
                float4[] boneWeights = new float4[wc.WeightMap.Count];
                float4[] boneIndices = new float4[wc.WeightMap.Count];

                // Iterate over the vertices
                for (int iVert = 0; iVert < wc.WeightMap.Count; iVert++)
                {
                    int nJoints = System.Math.Min(4, wc.WeightMap[iVert].VertexWeights.Count);
                    for (int iJoint = 0; iJoint < nJoints; iJoint++)
                    {
                        // boneWeights[iVert][iJoint] = wc.WeightMap[iVert].VertexWeights[iJoint].Weight;
                        // boneIndices[iVert][iJoint] = wc.WeightMap[iVert].VertexWeights[iJoint].JointIndex;
                        // Darn JSIL cannot handle float4 indexer. Map [0..3] to [x..z] by hand
                        switch (iJoint)
                        {
                            case 0:
                                boneWeights[iVert].x = wc.WeightMap[iVert].VertexWeights[iJoint].Weight;
                                boneIndices[iVert].x = wc.WeightMap[iVert].VertexWeights[iJoint].JointIndex;
                                break;
                            case 1:
                                boneWeights[iVert].y = wc.WeightMap[iVert].VertexWeights[iJoint].Weight;
                                boneIndices[iVert].y = wc.WeightMap[iVert].VertexWeights[iJoint].JointIndex;
                                break;
                            case 2:
                                boneWeights[iVert].z = wc.WeightMap[iVert].VertexWeights[iJoint].Weight;
                                boneIndices[iVert].z = wc.WeightMap[iVert].VertexWeights[iJoint].JointIndex;
                                break;
                            case 3:
                                boneWeights[iVert].w = wc.WeightMap[iVert].VertexWeights[iJoint].Weight;
                                boneIndices[iVert].w = wc.WeightMap[iVert].VertexWeights[iJoint].JointIndex;
                                break;
                        }
                    }
                    boneWeights[iVert].Normalize1();
                }

                rm = new Mesh()
                {
                    Colors = null,
                    Normals = mc.Normals,
                    UVs = mc.UVs,
                    BoneIndices = boneIndices,
                    BoneWeights = boneWeights,
                    Vertices = mc.Vertices,
                    Triangles = mc.Triangles
                };




                /*
                // invert weightmap to handle it easier
                float[,] invertedWeightMap = new float[wc.WeightMap[0].JointWeights.Count, wc.Joints.Count];
                for (int i = 0; i < wc.WeightMap.Count; i++)
                {
                    for (int j = 0; j < wc.WeightMap[i].JointWeights.Count; j++)
                    {
                        invertedWeightMap[j, i] = (float) wc.WeightMap[i].JointWeights[j];
                    }
                }

                float4[] boneWeights = new float4[invertedWeightMap.GetLength(0)];
                float4[] boneIndices = new float4[invertedWeightMap.GetLength(0)];

                // Contents of the invertedWeightMap:
                // ----------------------------------
                // Imagine the weight table as seen in 3d modelling programs, i.e. cinema4d;
                // wij are values in the range between 0..1 and specify to which percentage 
                // the vertex (i) is controlled by the bone (j).
                //
                //            bone 0   bone 1   bone 2   bone 3   ....  -> indexed by j
                // vertex 0:   w00      w01      w02      w03
                // vertex 1:   w10      w11      w12      w13
                // vertex 2:   w20      w21      w22      w23
                // vertex 3:   w30      w31      w32      w33
                //   ...
                //  indexed 
                //   by i

                // Iterate over the vertices
                for (int iVert = 0; iVert < invertedWeightMap.GetLength(0); iVert++)
                {
                    boneWeights[iVert] = new float4(0, 0, 0, 0);
                    boneIndices[iVert] = new float4(0, 0, 0, 0);

                    var tempDictionary = new Dictionary<int, float>();

                    // For the given vertex i, see which bones control us
                    for (int j = 0; j < invertedWeightMap.GetLength(1); j++)
                    {
                        if (j < 4)
                        {
                            tempDictionary.Add(j, invertedWeightMap[iVert, j]);
                        }
                        else
                        {
                            float tmpWeight = invertedWeightMap[iVert, j];
                            var keyAndValue = tempDictionary.OrderBy(kvp => kvp.Value).First();
                            if (tmpWeight > keyAndValue.Value)
                            {
                                tempDictionary.Remove(keyAndValue.Key);
                                tempDictionary.Add(j, tmpWeight);
                            }
                        }
                    }

                    if (tempDictionary.Count != 0)
                    {
                        var keyValuePair = tempDictionary.First();
                        boneIndices[iVert].x = keyValuePair.Key;
                        boneWeights[iVert].x = keyValuePair.Value;
                        tempDictionary.Remove(keyValuePair.Key);
                    }
                    if (tempDictionary.Count != 0)
                    {
                        var keyValuePair = tempDictionary.First();
                        boneIndices[iVert].y = keyValuePair.Key;
                        boneWeights[iVert].y = keyValuePair.Value;
                        tempDictionary.Remove(keyValuePair.Key);
                    }
                    if (tempDictionary.Count != 0)
                    {
                        var keyValuePair = tempDictionary.First();
                        boneIndices[iVert].z = keyValuePair.Key;
                        boneWeights[iVert].z = keyValuePair.Value;
                        tempDictionary.Remove(keyValuePair.Key);
                    }
                    if (tempDictionary.Count != 0)
                    {
                        var keyValuePair = tempDictionary.First();
                        boneIndices[iVert].w = keyValuePair.Key;
                        boneWeights[iVert].w = keyValuePair.Value;
                        tempDictionary.Remove(keyValuePair.Key);
                    }

                    boneWeights[iVert].Normalize1();
                }

                rm = new Mesh()
                {
                    Colors = null,
                    Normals = mc.Normals,
                    UVs = mc.UVs,
                    BoneIndices = boneIndices,
                    BoneWeights = boneWeights,
                    Vertices = mc.Vertices,
                    Triangles = mc.Triangles
                };
                */
            }


            return rm;
        }

        private ITexture LoadTexture(string path)
        {
            string texturePath = Path.Combine(_scenePathDirectory, path);
            var image = _rc.LoadImage(texturePath);
            return _rc.CreateTexture(image);
        }

        private ShaderEffect MakeMaterial(MaterialComponent mc)
        {

            WeightComponent wc = CurrentNode.GetWeights();
            ShaderCodeBuilder scb = new ShaderCodeBuilder(mc, null, wc); // TODO, CurrentNode.GetWeights() != null);
            var effectParameters = AssembleEffectParamers(mc, scb);

            ShaderEffect ret = new ShaderEffect(new []
                {
                    new EffectPassDeclaration()
                    {
                        VS = scb.VS,
                        //VS = VsBones,
                        PS = scb.PS,
                        StateSet = new RenderStateSet()
                        {
                            ZEnable = true,
                            AlphaBlendEnable = false
                        }
                    }
                },
                effectParameters
            );
            return ret;
        }

        private List<EffectParameterDeclaration> AssembleEffectParamers(MaterialComponent mc, ShaderCodeBuilder scb)
        {
            List<EffectParameterDeclaration> effectParameters = new List<EffectParameterDeclaration>();

            if (mc.HasDiffuse)
            {
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = scb.DiffuseColorName,
                    Value = (object) mc.Diffuse.Color
                });
                if (mc.Diffuse.Texture != null)
                {
                    effectParameters.Add(new EffectParameterDeclaration
                    {
                        Name = scb.DiffuseMixName,
                        Value = mc.Diffuse.Mix
                    });
                    effectParameters.Add(new EffectParameterDeclaration
                    {
                        Name = scb.DiffuseTextureName,
                        Value = LoadTexture(mc.Diffuse.Texture)
                    });
                }
            }

            if (mc.HasSpecular)
            {
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = scb.SpecularColorName,
                    Value = (object) mc.Specular.Color
                });
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = scb.SpecularShininessName,
                    Value = (object) mc.Specular.Shininess
                });
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = scb.SpecularIntensityName,
                    Value = (object) mc.Specular.Intensity
                });
                if (mc.Specular.Texture != null)
                {
                    effectParameters.Add(new EffectParameterDeclaration
                    {
                        Name = scb.SpecularMixName,
                        Value = mc.Specular.Mix
                    });
                    effectParameters.Add(new EffectParameterDeclaration
                    {
                        Name = scb.SpecularTextureName,
                        Value = LoadTexture(mc.Specular.Texture)
                    });
                }
            }

            if (mc.HasEmissive)
            {
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = scb.EmissiveColorName,
                    Value = (object) mc.Emissive.Color
                });
                if (mc.Emissive.Texture != null)
                {
                    effectParameters.Add(new EffectParameterDeclaration
                    {
                        Name = scb.EmissiveMixName,
                        Value = mc.Emissive.Mix
                    });
                    effectParameters.Add(new EffectParameterDeclaration
                    {
                        Name = scb.EmissiveTextureName,
                        Value = LoadTexture(mc.Emissive.Texture)
                    });
                }
            }

            if (mc.HasBump)
            {
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = scb.BumpIntensityName,
                    Value = mc.Bump.Intensity
                });
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = scb.BumpTextureName,
                    Value = LoadTexture(mc.Bump.Texture)
                });
            }

            // Any light calculation needed at all?
            if (mc.HasDiffuse || mc.HasSpecular)
            {
                // Light calculation parameters
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = ShaderCodeBuilder.LightColorName,
                    Value = new float3(1, 1, 1)
                });
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = ShaderCodeBuilder.LightIntensityName,
                    Value = (float) 1
                });
                effectParameters.Add(new EffectParameterDeclaration
                {
                    Name = ShaderCodeBuilder.LightDirectionName,
                    Value = new float3(0, 0, 1)
                });
            }

            return effectParameters;
        }
        #endregion
 
    }
}
