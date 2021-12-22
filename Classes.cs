using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;
using Newtonsoft.Json;

namespace jetSceneCrusher
{

    public class NJTextureList
    {
        public NJTextureList(uint len) { length = len; pvrs = new uint[len]; }
        public uint length;
        public uint[] pvrs;
    }


    public class JSRAngle
    {
        public short P;
        public short Y;
        public short R;
    }


    public class JSRGameObjectGroup
    {
        public int order;
        public JSRGameObject[] assets;
    };


    public class JSRAsset
    {
        public AFSOffset Model;
        public uint[] Texlist;
        [JsonIgnore]
        public uint TextureAddress;
    }
    public class JSRGameObject
    {
        public uint ObjectID;
        public Vector3 Translation;
        public JSRAngle Rotation;
        public uint AssetIndex;
        public uint TexlistIndex;
        public void load(BinaryReader strm)
        {
            ObjectID = strm.ReadUInt32();
            Translation = new Vector3(strm.ReadSingle(), strm.ReadSingle(), strm.ReadSingle());
            Rotation = new JSRAngle();
            Rotation.P = strm.ReadInt16();
            strm.ReadInt16();
            Rotation.Y = strm.ReadInt16();
            strm.ReadInt16();
            Rotation.R = strm.ReadInt16();
        }
    }

    public class AFSOffset
    {
        public byte FileIndex;
        public string Filename;
        public uint Offset;
    }

    public class JSRAssetInfo
    {
        public AFSOffset Model;
        public int[] Texlist;
    }
    public class JSRScene
    {
        public AFSOffset[] Textures;
        public JSRAsset[] Assets;
        public JSRGameObject[][] MapParts;
    }
}
