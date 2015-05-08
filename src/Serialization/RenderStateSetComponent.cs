using System.Collections.Generic;
using ProtoBuf;

namespace Fusee.Serialization
{
    [ProtoContract]

    class RenderStateSetComponent : SceneComponentContainer
    {
        public List<KeyValuePair<RenderState, int>> StateBlockList;
    }
}
