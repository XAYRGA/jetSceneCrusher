using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga;
using System.IO;

namespace jetSceneCrusher
{
    class TextureListDictionaryBuilderV1 : Extractor
    {


        public class TextureListDictionaryBuilderPayload
        {
            public AFSOffset[] Textures;
            public uint[][] TexLists;
            public Dictionary<uint, uint> NormalizerMap;

        }

        AFSOffset[] fileinfo;
        BinaryReader[] fileHandles;
        Dictionary<uint, uint[]> globalTextureTable = new Dictionary<uint, uint[]>();
        int mistakes = 0;
        BinaryReader binexecReader;
        uint textureTable;

        private static bool isPointer(uint data)
        {
            return (data & 0x8C000000) == 0x8C000000;
        }

        public TextureListDictionaryBuilderV1(BinaryReader bread, uint tlist, AFSOffset[] fSOffsets)
        {
            textureTable = tlist;
            fileinfo = fSOffsets;
            binexecReader = bread;
        }

        /// <summary>
        /// Resolves PVR file + pointer.
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        public  AFSOffset GetPVRReference(uint pointer, out string reason)
        {
            reason = "OK";
            for (int i = 0; i < fileHandles.Length; i++)
            {
                var currentFileName = fileinfo[i];
                var currentBinaryReader = fileHandles[i];
                var physicalOffset = pointer - TEXTURE_ALLOCATION_ADDRESS;

                if (physicalOffset >= currentBinaryReader.BaseStream.Length)
                    continue; // Pointer exceeds current PVR cluster range. 
                currentBinaryReader.BaseStream.Position = physicalOffset;
                if (currentBinaryReader.ReadUInt32() != PVR_GBIX_HEADER)
                {
                    Console.WriteLine($"Phys {physicalOffset:X} vir {pointer:X}");
                    reason = "HNOTMATCH";
                    continue; // We weren't pointing to a texture 
                }

                return new AFSOffset { Filename = currentFileName.Filename, FileIndex = currentFileName.FileIndex, Offset = physicalOffset };
            }
            return null; // No matches found in any of the loaded files. 
        }

        public bool fillReferenceTable(uint textureid, uint reftable)
        {
            binexecReader.BaseStream.Position = reftable - EXECUTABLE_ALLOCATION_ADDRESS;
            var TEXINFO = 0Lu;
            while ((TEXINFO = binexecReader.ReadUInt64()) != TEXTABLE_LIST_END)
            {
                var anchor = binexecReader.BaseStream.Position;

                var listaddr = (uint)(TEXINFO & 0xFFFFFFFF);

                var slot = (uint)(TEXINFO >> 32);

                uint[] curList = null;
                if (!globalTextureTable.TryGetValue(listaddr, out curList))
                {
                    binexecReader.BaseStream.Position = listaddr - EXECUTABLE_ALLOCATION_ADDRESS;
                    binexecReader.ReadUInt32(); // Pointer to runtime data, ignore. 
                    var len = binexecReader.ReadUInt32();
                    curList = new uint[len];
                    globalTextureTable[listaddr] = curList;
                }
                curList[slot] = textureid;
                binexecReader.BaseStream.Position = anchor;
            }

            return true;
        }
        public TextureListDictionaryBuilderPayload build() {


            fileHandles = new BinaryReader[fileinfo.Length] ;
            Console.WriteLine("Creating PVR cluster handles...");
            for (int i =0; i < fileinfo.Length; i++)
                if (!File.Exists(fileinfo[i].Filename))
                    cmdarg.assert($"PVRCluster File doesn't exist: {fileinfo[i].Filename}");
            
    
            for (int i = 0; i < fileinfo.Length; i++)
                try
                {
                    // RAM disaster, sorry. Had to convert it from the old file mounting system to an AFS mounting system.
                    var fH = File.OpenRead(fileinfo[i].Filename);
                    var afs = AFSFile.load(new BinaryReader(fH));
                    var newData = afs.partitions[fileinfo[i].FileIndex];
                    var ms = new MemoryStream(newData.data);
                    fileHandles[i] = new BinaryReader(ms);
                }
                catch (Exception E)
                {
                    cmdarg.assert($"Cannot open PVRCluster file {fileinfo[i].Filename}");
                }

            binexecReader.BaseStream.Position = textureTable - EXECUTABLE_ALLOCATION_ADDRESS;

            /* Test to see if it is a texturetable */
            var test = binexecReader.ReadUInt32();
            if (!isPointer(test)) { Console.WriteLine("This doesn't look like a texturetable! ENTER to continue parsing anyways."); Console.ReadLine(); }
            binexecReader.BaseStream.Position -= 4;

            var TEXINFO = 0lu;
            var index = 0;
            var TextureTable = new Queue<AFSOffset>();
            var textureID = 0u;
            while ((TEXINFO = binexecReader.ReadUInt64()) != TEXTABLE_LIST_END)
            {
                var anchor = binexecReader.BaseStream.Position;
                var reftable = (uint)(TEXINFO & 0xFFFFFFFF);
                var pvrAddr = (uint)(TEXINFO >> 32);

                if (pvrAddr == 0)
                {
                    Console.WriteLine($"Empty PVR reference {pvrAddr} @ {binexecReader.BaseStream.Position + EXECUTABLE_ALLOCATION_ADDRESS:X}");
                    goto nextIter;
                }

                string fail = "";
                var currentPVRHandle = GetPVRReference(pvrAddr, out fail);
                if (currentPVRHandle == null)
                {
                    Console.WriteLine($"PVR at {pvrAddr:X} could not be resolved! {fail}");
                    mistakes++;
                    goto nextIter;
                }

                textureID++;

                TextureTable.Enqueue(currentPVRHandle);

                fillReferenceTable(textureID, reftable);

                nextIter: // Have to restore position before next operation.
                binexecReader.BaseStream.Position = anchor;
                index++;
                continue;
            }
            Console.WriteLine($"END PVR Table");
            if (mistakes == 0)
            {
                var occ = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Process completed with no mistakes.");
                Console.ForegroundColor = occ;
            }

            Console.WriteLine("Normalizing...");

            uint[][] lists = new uint[globalTextureTable.Count][];
            Dictionary<uint, uint> map = new Dictionary<uint, uint>();
            AFSOffset[] Textures = new AFSOffset[TextureTable.Count];
            var normIndex = 0u;
            foreach (KeyValuePair<uint,uint[]> cur in globalTextureTable)
            {
                lists[normIndex] = cur.Value;
                map[cur.Key] = normIndex;
                normIndex++;
            }

            Console.WriteLine("Unrolling...");
            for (int i=0; i < Textures.Length; i++)
                Textures[i] = TextureTable.Dequeue();           
            var export = new TextureListDictionaryBuilderPayload() { NormalizerMap = map, Textures = Textures, TexLists = lists };

            return export;
        }
    }
}
