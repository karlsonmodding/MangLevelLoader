﻿using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SevenZip.Compression.LZMA;

namespace KarlsonLevels.Workshop_API
{
    public static class WML_Convert
    {
        public static WML Decode(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                byte[] magic = reader.ReadBytes(3);
                if (Encoding.ASCII.GetString(magic) != "WML") { MelonLogger.Error("[WML ERROR] Magic didn't match"); return null; }
                string name = reader.ReadString();
                int sz = reader.ReadInt32();
                byte[] img_cmp = reader.ReadBytes(sz);
                byte[] image = SevenZipHelper.Decompress(img_cmp);
                sz = reader.ReadInt32();
                byte[] lvl_cmp = reader.ReadBytes(sz);
                byte[] level = SevenZipHelper.Decompress(lvl_cmp);
                return new WML(name, image, level);
            }
        }

        public static byte[] Encode(WML data)
        {
            using(MemoryStream stream = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("WML"));
                writer.Write(data.Name);
                byte[] img_cmp = SevenZipHelper.Compress(data.Thumbnail);
                writer.Write(img_cmp.Length);
                writer.Write(img_cmp);
                byte[] lvl_cmp = SevenZipHelper.Compress(data.LevelData);
                writer.Write(lvl_cmp.Length);
                writer.Write(lvl_cmp);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public class WML
        {
            public WML(string name, byte[] thumbnail, byte[] levelData)
            {
                Name = name;
                Thumbnail = thumbnail;
                LevelData = levelData;
            }
            public string Name;
            public byte[] Thumbnail;
            public byte[] LevelData;
        }
    }
}
