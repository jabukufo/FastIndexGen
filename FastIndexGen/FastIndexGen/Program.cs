using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastIndexGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Space will be filled with empty cfgt tags\n");
            Console.Write("Last index to be created: ");

            uint lastTag = 0;

            try
            {
                lastTag = Convert.ToUInt32(Console.ReadLine(), 16);
            }
            catch
            {
                Console.WriteLine("Index must be a hex value between 0x0 and 0xFFFF");
                Console.ReadLine();
                return;
            }

            if (lastTag > 0xFFFF)
            {
                Console.WriteLine("Index must be a hex value between 0x0 and 0xFFFF");
                Console.ReadLine();
                return;
            }

            Console.Write("Path to tags.dat: ");
            var tagsPath = Console.ReadLine();

            if (!File.Exists(tagsPath))
            {
                Console.WriteLine("Unable to locate the tags file...");
                Console.ReadLine();
                return;
            }

            // Hardcoded byte[] equal to a new empty cfgt tag.
            var cfgtEmpty = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                         0x30, 0x00, 0x00, 0x00, 0x74, 0x67, 0x66, 0x63, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                         0xFC, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };


            // initializing variables for holding data.
            uint offsetsTableOffset = 0x00;
            uint tagsCount = 0x00;
            byte[] oldData = null;
            byte[] offsetsTableData = null;
            uint endOfLastTag = 0;

            using (BinaryReader reader = new BinaryReader(File.Open(tagsPath, FileMode.Open)))
            {
                reader.BaseStream.Position = 0x04; // Set postion forward 4 bytes, after the padding at beginning of file.
                offsetsTableOffset = reader.ReadUInt32(); // Offset the "Offsets-Table" begins at.
                tagsCount = reader.ReadUInt32(); // Amount of tags specified in header

                reader.BaseStream.Position = 0x10; // Sets position to end header (there is also some more header stuff 
                                                   // between 0x10 and 0x30 that doesn't change, so it can be treated as
                                                   // part of the tag data.

                oldData = reader.ReadBytes((int)offsetsTableOffset - 0x10); // Read bytes up to beginning of offsets table (all tag data).

                reader.BaseStream.Position = offsetsTableOffset; // Sets position to where offsets table begins

                // Read bytes to end of file (all Offsets Table data).
                offsetsTableData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

                endOfLastTag = offsetsTableOffset; // Offset where tag data ends and offsets table begins.
            }

            if (lastTag < tagsCount)
            {
                Console.WriteLine("Index already exists!");
                Console.ReadLine();
                return;
            }

            using (BinaryWriter writer = new BinaryWriter(File.Open(tagsPath, FileMode.Create)))
            {
                uint amount = lastTag - tagsCount + 1; // Amount of tags to add
                uint tagBytes = (uint)cfgtEmpty.Count() * amount; // Size of the new tag data being added.

                writer.Write(0); // 4-byte padding at beginning of file
                writer.Write(offsetsTableOffset + tagBytes); // New Location of the offsets table
                writer.Write(tagsCount + amount); // Amount of tags in tags.dat
                writer.Write(0); // 4-byte padding following the tags-count in header.
                writer.Write(oldData); // Old tag data

                for (var i = 0; i < amount; i++) // Add empty cfgt data to end X amount of times
                    writer.Write(cfgtEmpty);

                writer.Write(offsetsTableData); // Old offsets table data.

                for (var i = 0; i < amount; i++) // Add new offset table entires X amount of times
                {
                    writer.Write(endOfLastTag);
                    endOfLastTag += 0x40; // Increment offset to write into the table by the size of an empty cfgt tag.
                }
            }

            Console.WriteLine($"Tags duplicated successfully. Last tag: 0x{lastTag.ToString("X4")}  Offset: 0x{endOfLastTag.ToString("X4")}");

            Console.ReadLine();
        }
    }
}
