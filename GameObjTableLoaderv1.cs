using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace jetSceneCrusher
{
    class GameObjTableLoaderV1 : Extractor
    {
        BinaryReader binary;
        uint ObjectTableAddress = 0;
        uint TableLength = 0; 
        AFSOffset OffsetData; 

        public GameObjTableLoaderV1(BinaryReader bread,  uint oTableAddress, uint count, AFSOffset stage)
        {
            binary = bread;
            ObjectTableAddress = oTableAddress;
            TableLength = count;
            OffsetData = stage;

        }
        public JSRGameObject[][] processAssetTable()
        {
            var stageBinFile = new BinaryReader(File.OpenRead(OffsetData.Filename));
            var stageBinAFS = AFSFile.load(stageBinFile);
            var stageReader = new BinaryReader(new MemoryStream(stageBinAFS.partitions[OffsetData.FileIndex].data));
            
           
            var ret = new JSRGameObject[TableLength][];
            binary.BaseStream.Position = ObjectTableAddress - EXECUTABLE_ALLOCATION_ADDRESS; 
            var objGrpPointers = readU32Arr(binary, TableLength); 
            for (int i=0; i < objGrpPointers.Length;i++)
            {
                var curPnt = objGrpPointers[i];
                if (curPnt == 0)
                    continue;
                stageReader.BaseStream.Position = curPnt - STAGE_ALLOCATION_ADDRESS;
                var subObjCount = readTableCount(stageReader);
                var subAssetPointers = readU32Arr(stageReader, (uint)subObjCount);
                var grp = new JSRGameObject[subObjCount];
                for (int so = 0; so < subObjCount; so++)
                {
                    stageReader.BaseStream.Position = subAssetPointers[so] - STAGE_ALLOCATION_ADDRESS;
                    grp[so] = new JSRGameObject();
                    grp[so].load(stageReader);
                }
                ret[i] = grp;
            }
            return ret;
        }
    }
}
