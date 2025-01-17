﻿using CTRFramework.Shared;
using CTRFramework.Vram;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CTRFramework
{
    public struct FaceFlags
    {
        public RotateFlipType rotateFlipType;
        public FaceMode faceMode;

        public FaceFlags(byte x)
        {
            rotateFlipType = (RotateFlipType)(x & 7);
            faceMode = (FaceMode)(x >> 3 & 3);
        }
    }



    public class QuadBlock : IRead, IWrite
    {
        public static readonly int SizeOf = 0x5C;

        /*
         * 0--4--1
         * | /| /|
         * |/ |/ |
         * 5--6--7
         * | /| /|
         * |/ |/ |
         * 2--8--3
         */

        public long pos;

        //9 indices in vertex array, that form 4 quads, see above.
        public short[] ind = new short[9];
        public QuadFlags quadFlags;

        public uint bitvalue;

        //these values are contained in bitvalue, mask is 8b5b5b5b5b4z where b is bit and z is empty. or is it?
        public byte drawOrderLow;
        public FaceFlags[] faceFlags = new FaceFlags[4];
        public uint extradata;

        public byte[] drawOrderHigh = new byte[4];

        public UIntPtr[] ptrTexMid = new UIntPtr[4];    //offsets to mid texture definition

        public BoundingBox bb;              //a box that bounds

        public TerrainFlags terrainFlag;
        public byte WeatherIntensity;
        public byte WeatherType;
        public byte TerrainFlagUnknown; //almost always 0, only found in tiger temple and sewer speedway

        public short id;
        public byte trackPos;
        public byte midunk;

        //public byte[] midflags = new byte[2];

        public UIntPtr ptrTexLow;                 //offset to LOD texture definition
        public UIntPtr mosaicStruct;

        public UIntPtr mosaicPtr1;
        public UIntPtr mosaicPtr2;
        public UIntPtr mosaicPtr3;
        public UIntPtr mosaicPtr4;

        public List<Vector2> unk3 = new List<Vector2>();    //face normal vector or smth. 4*2 for mid + 2 for low

        //additional data
        public TextureLayout texlow;

        public List<CtrTex> tex = new List<CtrTex>();


        public bool isWater = false;

        public QuadBlock()
        {
        }

        public QuadBlock(BinaryReaderEx br)
        {
            Read(br);
        }


        public void Read(BinaryReaderEx br)
        {
            pos = br.BaseStream.Position;

            for (int i = 0; i < 9; i++)
                ind[i] = br.ReadInt16();

            quadFlags = (QuadFlags)br.ReadUInt16();

            bitvalue = br.ReadUInt32(); //big endian or little??
            {
                drawOrderLow = (byte)(bitvalue & 0xFF);

                for (int i = 0; i < 4; i++)
                {
                    byte val = (byte)((bitvalue >> 8 + 5 * i) & 0x1F);
                    faceFlags[i] = new FaceFlags(val);
                }

                //extradata = (byte)(bitvalue & 0xF0000000 >> 28);
                extradata = (bitvalue & 0xFFF);
            }

            drawOrderHigh = br.ReadBytes(4);

            for (int i = 0; i < 4; i++)
            {
                ptrTexMid[i] = br.ReadUIntPtr();

                if (Helpers.TestPointer(ptrTexMid[i].ToUInt32()) != 0)
                    Helpers.Panic(this, PanicType.Assume, $"ptrTexMid[{i}] {ptrTexMid[i].ToUInt32().ToString("X8")} - {Helpers.TestPointer(ptrTexMid[i].ToUInt32()).ToString("x2")}");
                // Console.ReadKey();
            }



            bb = new BoundingBox(br);

            byte tf = br.ReadByte();

            if (tf > 20)
                Helpers.Panic(this, PanicType.Assume, "unexpected terrain flag value -> " + tf);

            terrainFlag = (TerrainFlags)tf;
            WeatherIntensity = br.ReadByte();
            WeatherType = br.ReadByte();
            TerrainFlagUnknown = br.ReadByte();

            id = br.ReadInt16();

            trackPos = br.ReadByte();
            midunk = br.ReadByte();

            //midflags = br.ReadBytes(2);

            ptrTexLow = (UIntPtr)br.ReadUInt32();

            if (Helpers.TestPointer(ptrTexLow.ToUInt32()) != 0)
            {
                Console.WriteLine("ptrTexLow " + Helpers.TestPointer(ptrTexLow.ToUInt32()).ToString("x2"));
                //Console.ReadKey();
            }

            mosaicStruct = br.ReadUIntPtr();

            if (Helpers.TestPointer(mosaicStruct.ToUInt32()) != 0)
            {
                Console.WriteLine("offset2 " + Helpers.TestPointer(mosaicStruct.ToUInt32()).ToString("x2"));
                Console.ReadKey();
            }

            for (int i = 0; i < 5; i++)
                unk3.Add(br.ReadVector2s(1 / 4096f));



            /*
            //this is some value per tirangle
            foreach(var val in unk3)
            {
                Console.WriteLine(val.X / 4096f + " " + val.Y / 4096f);
            }
            */

            //struct done

            //read texture layouts
            int texpos = (int)br.BaseStream.Position;

            br.Jump(ptrTexLow);
            texlow = TextureLayout.FromReader(br);


            foreach (uint u in ptrTexMid)
            {
                if (u == 0)
                {
                    if (ptrTexLow != UIntPtr.Zero)
                        Helpers.Panic(this, PanicType.Assume, $"Got low tex without mid tex at {br.HexPos()}.");

                    continue;
                }

                br.Jump(u);
                tex.Add(new CtrTex(br, (int)mosaicStruct));
            }

            if (mosaicStruct != UIntPtr.Zero)
            {
                br.Jump(mosaicStruct);

                mosaicPtr1 = br.ReadUIntPtr();
                mosaicPtr2 = br.ReadUIntPtr();
                mosaicPtr3 = br.ReadUIntPtr();
                mosaicPtr4 = br.ReadUIntPtr();
            }

            br.BaseStream.Position = texpos;
        }


        //magic array of indices, each line contains 2 quads
        int[] inds = new int[]
        {
            6, 5, 1,
            6, 7, 5,
            7, 2, 5,
            7, 8, 2,
            3, 7, 6,
            3, 9, 7,
            9, 8, 7,
            9, 4, 8
        };

        private List<int[]> FaceIndices = new List<int[]>() {
            new int[] { 0, 4, 5, 6 },
            new int[] { 4, 1, 6, 7 },
            new int[] { 5, 6, 2, 8 },
            new int[] { 6, 7, 8, 3 }
        };

        /*
        //magic array of indices, each line contains 2 quads
        int[] inds = new int[]
        {
            1, 6, 5, 5, 6, 7,
            5, 7, 2, 2, 7, 8,
            6, 3, 7, 7, 3, 9,
            7, 9, 8, 8, 9, 4
        };
        */

        public List<Vertex> GetVertexList(Scene s)
        {
            List<Vertex> buf = new List<Vertex>();

            for (int i = inds.Length - 1; i >= 0; i--)
                buf.Add(s.verts[ind[inds[i] - 1]]);

            for (int i = 0; i < inds.Length / 6; i++)
            {
                buf[i * 6 + 0].uv = new Vector2b(0, 1);
                buf[i * 6 + 1].uv = new Vector2b(1, 0);
                buf[i * 6 + 2].uv = new Vector2b(0, 0);
                buf[i * 6 + 3].uv = new Vector2b(0, 1);
                buf[i * 6 + 4].uv = new Vector2b(1, 1);
                buf[i * 6 + 5].uv = new Vector2b(1, 0);
            }

            return buf;
        }


        //use this later for obj export too
        public List<Vertex> GetVertexListq(List<Vertex> vertexArray, int i)
        {
            try
            {
                List<Vertex> buf = new List<Vertex>();

                if (i == -1)
                {
                    int[] arrind = new int[] { 0, 1, 2, 3 };

                    for (int j = 0; j < 4; j++)
                        buf.Add(vertexArray[ind[arrind[j]]]);

                    for (int j = 0; j < 4; j++)
                    {
                        buf[j].uv = texlow.normuv[j];
                    }

                    if (buf.Count != 4)
                    {
                        Helpers.Panic(this, PanicType.Error, "not a quad! " + buf.Count);
                        Console.ReadKey();
                    }

                    return buf;
                }
                else
                {
                    int[] arrind;
                    int[] uvinds;

                    switch (faceFlags[i].rotateFlipType)
                    {
                        case RotateFlipType.None: uvinds = GetUVIndices2(1, 2, 3, 4); break;
                        case RotateFlipType.Rotate90: uvinds = GetUVIndices2(3, 1, 4, 2); break;
                        case RotateFlipType.Rotate180: uvinds = GetUVIndices2(4, 3, 2, 1); break;
                        case RotateFlipType.Rotate270: uvinds = GetUVIndices2(2, 4, 1, 3); break;
                        case RotateFlipType.Flip: uvinds = GetUVIndices2(2, 1, 4, 3); break;
                        case RotateFlipType.FlipRotate90: uvinds = GetUVIndices2(4, 2, 3, 1); break;
                        case RotateFlipType.FlipRotate180: uvinds = GetUVIndices2(3, 4, 1, 2); break;
                        case RotateFlipType.FlipRotate270: uvinds = GetUVIndices2(1, 3, 2, 4); break;
                        default: throw new Exception("Impossible rotatefliptype.");
                    }


                    switch (faceFlags[i].faceMode)
                    {
                        case FaceMode.SingleUV1:
                            {
                                uvinds = new int[] { uvinds[2], uvinds[0], uvinds[3], uvinds[1] };
                                //uvinds = new int[] { uvinds[0], uvinds[0], uvinds[0], uvinds[0] };
                                break;
                            }

                        case FaceMode.SingleUV2:
                            {
                                uvinds = new int[] { uvinds[1], uvinds[2], uvinds[3], uvinds[0] };
                                //uvinds = new int[] { uvinds[0], uvinds[0], uvinds[0], uvinds[0] };
                                break;
                            }
                    }


                    if (i > 4 || i < 0)
                    {
                        Helpers.Panic(this, PanicType.Warning, "Can't have more than 4 quads in a quad block.");
                        return null;
                    }

                    arrind = FaceIndices[i];

                    for (int j = 0; j < 4; j++)
                        buf.Add(vertexArray[ind[arrind[j]]]);

                    /*
                    for (int j = 0; j < 4; j++)
                    {
                        buf[j].color = new Vector4b(
                            (byte)(255 / 4 * j),
                            (byte)(255 / 4 * j),
                            (byte)(255 / 4 * j), 
                            0);
                    }
                    */

                    for (int j = 0; j < 4; j++)
                    {
                        if (tex.Count > 0)
                        {
                            if (!tex[i].isAnimated)
                            {
                                buf[j].uv = tex[i].midlods[2].normuv[uvinds[j] - 1];
                            }
                            else
                            {
                                buf[j].uv = tex[i].animframes[1].normuv[uvinds[j] - 1];
                            }
                        }
                        else
                        {
                            buf[j].uv = new Vector2b(0, 0);//new Vector2b((byte)((j & 3) >> 1), (byte)(j & 1));
                        }
                    }

                    if (buf.Count != 4)
                        Helpers.Panic(this, PanicType.Error, "not a quad! " + buf.Count);
                }

                return buf;
            }
            catch (Exception ex)
            {
                Helpers.Panic(this, PanicType.Error, "Can't export quad to MG. Give null.\r\n" + i + "\r\n" + ex.Message);
                return null;
            }
        }

        public int[] GetUVIndices2(int x, int y, int z, int w)
        {
            return new int[]
            {
                x, y, z, w
            };
        }

        bool objSaveQuads = false;

        public string ToObj(List<Vertex> v, Detail detail, ref int a, ref int b)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("g {0}\r\n", (quadFlags.HasFlag(QuadFlags.InvisibleTriggers) ? "invisible" : "visible"));
            sb.AppendFormat("o piece_{0}\r\n\r\n", id.ToString("X4"));

            switch (detail)
            {
                case Detail.Low:
                    {
                        List<Vertex> list = GetVertexListq(v, -1);

                        foreach (Vertex vt in list)
                        {
                            sb.AppendLine(vt.ToString());
                            sb.AppendLine("vt " + vt.uv.X / 255f + " " + vt.uv.Y / -255f);
                        }

                        sb.AppendLine("\r\nusemtl " + (ptrTexLow != UIntPtr.Zero ? texlow.Tag() : "default"));

                        if (objSaveQuads)
                        {
                            sb.Append(OBJ.ASCIIQuad("f", a, b));
                        }
                        else
                        {
                            sb.Append(OBJ.ASCIIFace("f", a, b, 1, 3, 2, 1, 3, 2));
                            sb.Append(OBJ.ASCIIFace("f", a, b, 2, 3, 4, 2, 3, 4));
                        }

                        a += 4;
                        b += 4;

                        break;
                    }
                case Detail.Med:
                    {

                        for (int i = 0; i < 4; i++)
                        {
                            List<Vertex> list = GetVertexListq(v, i);

                            //this normally shouldn't be null
                            if (list != null)
                            {

                                foreach (Vertex vt in list)
                                {
                                    sb.AppendLine(vt.ToString());
                                    sb.AppendLine("vt " + vt.uv.X / 255f + " " + vt.uv.Y / -255f);
                                }

                                sb.AppendLine("\r\nusemtl " + (ptrTexMid[i] != UIntPtr.Zero ? tex[i].midlods[2].Tag() : "default"));

                                if (objSaveQuads)
                                {
                                    sb.Append(OBJ.ASCIIQuad("f", a, b));
                                }
                                else
                                {
                                    sb.Append(OBJ.ASCIIFace("f", a, b, 1, 3, 2, 1, 3, 2));
                                    sb.Append(OBJ.ASCIIFace("f", a, b, 2, 3, 4, 2, 3, 4));
                                }

                                sb.AppendLine();

                                b += 4;
                                a += 4;
                            }
                            else
                            {
                                Helpers.Panic(this, PanicType.Error, $"something's wrong with quadblock {id} at {pos.ToString("X8")}, happens in secret2_4p and temple2_4p");
                            }
                        }

                        break;
                    }
            }

            return sb.ToString();
        }

        /*
        public string ToObj(List<Vertex> v, Detail detail, ref int a, ref int b)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("g {0}\r\n", (quadFlags.HasFlag(QuadFlags.InvisibleTriggers) ? "invisible" : "visible"));
            sb.AppendFormat("o piece_{0}\r\n\r\n", id.ToString("X4"));

            int vcnt;

            switch (detail)
            {
                case Detail.Low: vcnt = 4; break;
                case Detail.Med: vcnt = 9; break;
                default: vcnt = 0; break;
            }

            for (int i = 0; i < vcnt; i++)
            {
                sb.AppendLine(v[ind[i]].ToString());
            }

            sb.AppendLine();




            //if (!quadFlags.HasFlag(QuadFlags.InvisibleTriggers))
            {

                switch (detail)
                {
                    case Detail.Low:
                        {
                            sb.AppendLine(texlow.ToObj());

                            sb.Append(OBJ.ASCIIFace("f", a, b, 1, 3, 2, 1, 3, 2));
                            sb.Append(OBJ.ASCIIFace("f", a, b, 2, 3, 4, 2, 3, 4));

                            b += 4;

                            break;
                        }

                    case Detail.Med:
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (tex.Count == 4)
                                {
                                    sb.AppendLine(tex[i].midlods[2].ToObj());
                                }
                                else
                                {
                                    sb.AppendLine("usemtl default");
                                }

                                if (quadFlags.HasFlag(QuadFlags.InvisibleTriggers))
                                    sb.AppendLine("usemtl default");

                                switch (faceFlags[i].rotateFlipType)
                                {
                                    case RotateFlipType.None: uvinds = GetUVIndices(1, 2, 3, 4); break;
                                    case RotateFlipType.Rotate90: uvinds = GetUVIndices(3, 1, 4, 2); break;
                                    case RotateFlipType.Rotate180: uvinds = GetUVIndices(4, 3, 2, 1); break;
                                    case RotateFlipType.Rotate270: uvinds = GetUVIndices(2, 4, 1, 3); break;
                                    case RotateFlipType.Flip: uvinds = GetUVIndices(2, 1, 4, 3); break;
                                    case RotateFlipType.FlipRotate90: uvinds = GetUVIndices(4, 2, 3, 1); break;
                                    case RotateFlipType.FlipRotate180: uvinds = GetUVIndices(3, 4, 1, 2); break;
                                    case RotateFlipType.FlipRotate270: uvinds = GetUVIndices(1, 3, 2, 4); break;
                                }

                                switch (faceFlags[i].faceMode)
                                {
                                    case FaceMode.Normal:
                                        {
                                            sb.Append(OBJ.ASCIIFace("f", a, b, inds[i * 6], inds[i * 6 + 1], inds[i * 6 + 2], uvinds[0], uvinds[1], uvinds[2])); // 1 3 2 | 0 2 1
                                            sb.Append(OBJ.ASCIIFace("f", a, b, inds[i * 6 + 3], inds[i * 6 + 4], inds[i * 6 + 5], uvinds[3], uvinds[4], uvinds[5])); // 2 3 4 | 1 2 3
                                            break;
                                        }
                                    case FaceMode.SingleUV1:
                                        {
                                            sb.Append(OBJ.ASCIIFace("f", a, b, inds[i * 6], inds[i * 6 + 1], inds[i * 6 + 2], uvinds[1], uvinds[2], uvinds[0])); // 1 3 2 | 0 2 1
                                            break;
                                        }

                                    case FaceMode.SingleUV2:
                                        {
                                            sb.Append(OBJ.ASCIIFace("f", a, b, inds[i * 6], inds[i * 6 + 1], inds[i * 6 + 2], uvinds[2], uvinds[0], uvinds[1]));
                                            break;
                                        }
                                    case FaceMode.Unknown:
                                        {
                                            //should never happen i guess
                                            Helpers.Panic(this, "FaceMode: both flags are set!");
                                            Console.ReadKey();
                                            break;
                                        }
                                }

                                sb.AppendLine();

                                if (tex.Count == 4) b += 4;
                            }

                            break;
                        }
                }

            }
                
                a += vcnt;
            
            return sb.ToString();
        }
        */


        public void Write(BinaryWriterEx bw, List<UIntPtr> patchTable = null)
        {
            long sizeCheck = bw.BaseStream.Position;

            for (int i = 0; i < 9; i++)
                bw.Write(ind[i]);

            bw.Write((ushort)quadFlags);
            //bw.Write(unk1);

            // bw.Write(drawOrderLow);
            bw.Write(bitvalue);
            bw.Write(drawOrderHigh);

            for (int i = 0; i < 4; i++)
                bw.Write(ptrTexMid[i], patchTable);

            bb.Write(bw);

            bw.Write((byte)terrainFlag);
            bw.Write(WeatherIntensity);
            bw.Write(WeatherType);
            bw.Write(TerrainFlagUnknown);

            bw.Write(id);

            bw.Write(trackPos);
            bw.Write(midunk);
            //bw.Write(midflags);

            bw.Write(ptrTexLow, patchTable);
            bw.Write(mosaicStruct, patchTable);

            foreach (Vector2 v in unk3)
                bw.WriteVector2s(v, 1 / 4096f);

            if (bw.BaseStream.Position - sizeCheck != SizeOf)
            {
                throw new Exception("QuadBlock: size mismatch.");
            }
        }
    }
}