using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO; 

namespace jetSceneCrusher
{
    public class AFSFile
    {
        private const int AFS_HEAD = 0x534641;
        public int sectionCount;
        public AFSSection[] partitions;    // sections are relative to file base

        public static AFSFile load(BinaryReader reader)
        {
            var head = reader.ReadInt32();
            if (head != AFS_HEAD)
                throw new InvalidDataException($"AFSFile.load expected 0x{AFS_HEAD:X} 'AFS\\x00', got {head:X}");
            var AFS = new AFSFile();

            AFS.sectionCount = reader.ReadInt32();
            AFS.partitions = new AFSSection[AFS.sectionCount];
           
            for (int i = 0; i < AFS.sectionCount; i++)
                AFS.partitions[i] = AFSSection.load(reader);

            for (int i=0; i < AFS.sectionCount; i++)
            {
                reader.BaseStream.Position = AFS.partitions[i].offset;
                //Console.WriteLine($"{reader.BaseStream.Position:X}, {AFS.sections[i].length:X} ");
                AFS.partitions[i].data = reader.ReadBytes(AFS.partitions[i].length);
            }
            // we're at the end anchor of the second section

            /*
            // locate filetable offset
            // there's probably better way to do this
            Console.WriteLine("Finding filetable...");
            while ((reader.ReadByte()) == 0) ;

            reader.BaseStream.Position -= 1; // Dec one pos.
            //NNGHGHGNGHH
            */
            reader.BaseStream.Position = AFS.partitions[0].offset - 0x10;
            reader.ReadUInt64();
            var filetable_offset = reader.ReadUInt32();
            // Console.WriteLine($"{filetable_offset:X}");
            if (filetable_offset != 0)
            {
                reader.BaseStream.Position = filetable_offset;
                for (int i = 0; i < AFS.sectionCount; i++)
                    AFS.partitions[i].descriptor = AFSSectionDescriptor.load(reader);
            }

            return AFS;
        }

    }

    public class AFSSection
    {
        public int offset;
        public int length;
        public AFSSectionDescriptor descriptor;
        public byte[] data;

        public static AFSSection load(BinaryReader reader)
        {
            var AFSection = new AFSSection
            {
                offset = reader.ReadInt32(),
                length = reader.ReadInt32(),
            };
            return AFSection;
        }
    }


    public class AFSSectionDescriptor
    {
        public string name; // 16 bytes
        public byte[] unknown;        
        public int un2;
        public ushort un3;
        public ushort un4;
        public uint un5;
        public uint length;

        private static string readArchiveName(BinaryReader aafRead)
        {
            var ofs = aafRead.BaseStream.Position; // Store where we started 
            byte nextbyte; // Blank byte
            byte[] name = new byte[0x20]; // Array for the name

            int count = 0; // How many we've done
            while ((nextbyte = aafRead.ReadByte()) != 0xFF & nextbyte != 0x00) // Read until we've read 0 or FF
            {
                name[count] = nextbyte; // Store into byte array
                count++; // Count  how many valid bytes  we've read.
            }
            aafRead.BaseStream.Seek(ofs + 0x20, SeekOrigin.Begin); // Seek 0x70 bytes, because thats the statically allocated space for the wavegroup path. 
            return Encoding.ASCII.GetString(name, 0, count); // Return a string with the name, but only of the valid bytes we've read. 
        }


        public static AFSSectionDescriptor load(BinaryReader reader)
        {
            var afsd = new AFSSectionDescriptor()
            {
                name = readArchiveName(reader),
                un2 = reader.ReadInt32(),
                un3 = reader.ReadUInt16(),
                un4 = reader.ReadUInt16(),
                un5 = reader.ReadUInt32(),  
                length = reader.ReadUInt32(),                
            };
            return afsd;
        }

    }

}
