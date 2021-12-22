using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace jetSceneCrusher
{
    class AssetTableConverter : Extractor
    {
        BinaryReader binary;
        uint AssetTableAddress = 0;
        uint TextureTableAddress = 0;
        uint TableLength;
        AFSOffset OffsetData; 

        public AssetTableConverter(BinaryReader bread, uint assAddress, uint texAddress, uint count, AFSOffset offsetInfo)
        {
            binary = bread;
            AssetTableAddress = assAddress;
            TextureTableAddress = texAddress;
            TableLength = count;
            OffsetData = offsetInfo;
        }
        public JSRAsset[] processAssetTable()
        {
            var ret = new JSRAsset[TableLength];
            binary.BaseStream.Position = AssetTableAddress - EXECUTABLE_ALLOCATION_ADDRESS;
            var models = readU32Arr(binary, TableLength);
            binary.BaseStream.Position = TextureTableAddress - EXECUTABLE_ALLOCATION_ADDRESS;
            var textures = readU32Arr(binary, TableLength);
            for (int i=0; i < TableLength; i++)
                ret[i] = new JSRAsset() { Model = new AFSOffset { FileIndex = OffsetData.FileIndex, Filename = OffsetData.Filename, Offset = models[i] } , TextureAddress = textures[i]};
            return ret;
        }
    }
}
