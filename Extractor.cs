using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace jetSceneCrusher
{
    class Extractor
    {
        public const uint EXECUTABLE_ALLOCATION_ADDRESS = 0x8C010000;
        public const uint TEXTURE_ALLOCATION_ADDRESS = 0x8c800000;
        public const uint STAGE_ALLOCATION_ADDRESS = 0x8CB00000;
        public const ulong TEXTABLE_LIST_END = 0xFFFFFFFF00000000;
        public const uint PVR_GBIX_HEADER = 0x58494247u;

        public uint[] readU32Arr(BinaryReader reader, uint count)
        {
            var ret = new uint[count];
            for (int i = 0; i < count; i++)
                ret[i] = reader.ReadUInt32();

            return ret;
        }

        public static int readTableCount(BinaryReader read)
        {
            var anchor = read.BaseStream.Position;
            var count = 0;
            while ((read.ReadUInt32() & 0xF0000000) == 0x80000000)
                count++;
            read.BaseStream.Position = anchor;
            return count;
        }


    }
}
