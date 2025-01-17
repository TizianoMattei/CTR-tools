﻿using CTRFramework.Shared;
using CTRFramework.Vram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ThreeDeeBear.Models.Ply;

namespace CTRFramework
{
    public class CtrMesh : IRead
    {
        public string name = "default_name";
        public int unk0 = 0; //0?
        public short lodDistance = 1000;
        public short billboard = 0; //bit0 forces model to always face the camera, check other bits
        public Vector3 scale = new Vector3(4096);

        public UIntPtr ptrCmd = UIntPtr.Zero; //this is null if we have anims
        public UIntPtr ptrVerts = UIntPtr.Zero;
        public UIntPtr ptrTex = UIntPtr.Zero;
        public UIntPtr ptrClut = UIntPtr.Zero;

        public int unk3 = 0; //?
        public int numAnims = 0;
        public UIntPtr ptrAnims = UIntPtr.Zero;
        public int unk4 = 0; //?

        public Vector4s posOffset = new Vector4s(0, 0, 0, 0);

        public List<Vertex> verts = new List<Vertex>();
        public List<TextureLayout> matIndices = new List<TextureLayout>();

        public int cmdNum = 0x40;
        public int vrenderMode = 0x1C;

        public bool IsAnimated
        {
            get
            {
                return numAnims > 0;
            }
        }


        //private int maxTex = 0;
        //private int maxClut = 0;

        List<CtrAnim> anims = new List<CtrAnim>();

        public List<CtrDraw> drawList = new List<CtrDraw>();
        public List<Vector3b> vtx = new List<Vector3b>();
        public List<TextureLayout> tl = new List<TextureLayout>();
        public List<Vector4b> cols = new List<Vector4b>();

        public List<int> animPtrMap = new List<int>();


        public bool isTextured
        {
            get => ptrTex == ptrClut;
        }

        public CtrMesh()
        {

        }

        public CtrMesh(BinaryReaderEx br)
        {
            Read(br);
        }

        public static CtrMesh FromReader(BinaryReaderEx br)
        {
            return new CtrMesh(br);
        }

        /// <summary>
        /// Reads CTR model using BinaryReaderEx.
        /// </summary>
        /// <param name="br">BinaryReaderEx object.</param>
        public void Read(BinaryReaderEx br)
        {
            name = br.ReadStringFixed(16);
            unk0 = br.ReadInt32();
            lodDistance = br.ReadInt16();
            billboard = br.ReadInt16();
            scale = br.ReadVector3sPadded();
            ptrCmd = br.ReadUIntPtr();
            ptrVerts = br.ReadUIntPtr();
            ptrTex = br.ReadUIntPtr();
            ptrClut = br.ReadUIntPtr();
            unk3 = br.ReadInt32();
            numAnims = br.ReadInt32();
            ptrAnims = br.ReadUIntPtr();
            unk4 = br.ReadInt32();

            Console.WriteLine($"CtrHeader: {name}");

            if (unk0 != 0)
                Helpers.Panic(this, PanicType.Assume, $"check unusual unk0 value = {unk0}");

            if (billboard > 1)
                Helpers.Panic(this, PanicType.Assume, $"check unusual billboard value = {billboard}");


            int returnto = (int)br.BaseStream.Position;

            br.Jump(ptrAnims);

            for (int i = 0; i < numAnims; i++)
                animPtrMap.Add(br.ReadInt32());

            if (unk3 != 0)
                Helpers.Panic(this, PanicType.Assume, $"check unusual unk3 value = {unk3}");

            if (unk4 != 0)
                Helpers.Panic(this, PanicType.Assume, $"check unusual unk4 value = {unk4}");



            //read all drawing commands

            br.Jump(ptrCmd);

            cmdNum = br.ReadInt32();

            uint x;

            do
            {
                x = br.ReadUInt32(); //big endian or little endian?
                if (x != 0xFFFFFFFF)
                    drawList.Add(new CtrDraw(x));
            }
            while (x != 0xFFFFFFFF);

            //should read anims here

            /*
            if (numAnims > 0)
            {
                for (int f = 0; f < numAnims; f++)
                {
                    br.Jump(ptrAnims + f * 4);
                    br.Jump(br.ReadInt32());
                    anims.Add(new CTRAnim(br));
                }
            }
            */

            TextureLayout curtl;

            //define temporary arrays
            Vector4b[] clr = new Vector4b[4];       //color buffer
            Vector3s[] crd = new Vector3s[4];       //face buffer
            TextureLayout[] tlb = new TextureLayout[4];       //face buffer

            Vector3s[] stack = new Vector3s[256];   //vertex buffer

            int maxv = 0;
            int maxc = 0;
            int maxt = 0;

            //one pass through all draw commands to get the array lengths
            foreach (var draw in drawList)
            {
                //only increase vertex count for commands that don't take vertex from stack
                if (!draw.flags.HasFlag(CtrDrawFlags.v))
                    maxv++;

                //simply find max color index
                if (draw.colorIndex > maxc)
                    maxc = draw.colorIndex;

                //find max index, but 0 means no texture.
                if (draw.texIndex > 0)
                    if (draw.texIndex - 1 > maxt)
                        maxt = draw.texIndex;

                Console.WriteLine(draw.ToString());
            }

            Console.WriteLine("maxv: " + maxv);
            Console.WriteLine("maxc: " + maxc);
            Console.WriteLine("maxt: " + maxt);

            //int ppos = (int)br.BaseStream.Position;

            br.Jump(ptrClut);
            for (int k = 0; k <= maxc; k++)
                cols.Add(new Vector4b(br));


            //read texture layouts
            br.Jump(ptrTex);
            uint[] texptrs = br.ReadArrayUInt32(maxt);

            Console.WriteLine("texptrs: " + texptrs.Length);

            foreach (uint t in texptrs)
            {
                Console.WriteLine(t.ToString("X8"));
                br.Jump(t);
                TextureLayout tx = TextureLayout.FromReader(br);
                tl.Add(tx);
                Console.WriteLine(tx.ToString());
            }

            Console.WriteLine("tlcnt: " + tl.Count);



            //if static model
            if (!IsAnimated)
            {
                br.Jump(ptrVerts);

                posOffset = new Vector4s(br);

                Console.WriteLine(posOffset);

                br.Seek(16);

                vrenderMode = br.ReadInt32();

                if (!(new List<int> { 0x1C, 0x22 }).Contains(vrenderMode))
                {
                    Helpers.Panic(this, PanicType.Assume, $"check vrender {vrenderMode.ToString("X8")}");
                }
            }
            else
            {
                //jump to first animation, read header and jump to vertex garbage
                br.Jump(animPtrMap[0]);

                CtrAnim anim = CtrAnim.FromReader(br);

                Console.WriteLine(anim.name + " " + anim.numFrames);

                posOffset = new Vector4s(br);

                br.Seek(16);

                vrenderMode = br.ReadInt32();

                Console.WriteLine("anime!");

                if (anim.someOffset != 0)
                {
                    br.Jump(returnto);
                    return;
                }
                //Console.ReadKey();
            }

            //read vertices
            for (int k = 0; k < maxv; k++)
                vtx.Add(new Vector3b(br));

            foreach (var v in vtx)
                Console.WriteLine(v.ToString(VecFormat.Hex));


            List<Vector3s> vfixed = new List<Vector3s>();

            foreach (var v in vtx)
                vfixed.Add(new Vector3s(v.X, v.Y, v.Z));

            foreach (Vector3s v in vfixed)
            {
                //scale vertices
                v.X = (short)((((float)(v.X + posOffset.X) / 255.0f) * scale.X));
                v.Y = (short)(-(((float)(v.Y + posOffset.Z) / 255.0f) * scale.Z));
                v.Z = (short)((((float)(v.Z + posOffset.Y) / 255.0f) * scale.Y));

                //flip axis
                short zz = v.Z;
                v.Z = (short)-v.Y;
                v.Y = zz;
            }

            int vertexIndex = 0;
            int stripLength = 0;

            //process all commands
            foreach (CtrDraw d in drawList)
            {
                //curtl = d.texIndex == 0 ? null : tl[d.texIndex - 1];
                //Console.WriteLine(tl.Count + " " + d.texIndex);


                //if we got no stack vertex flag
                if (!d.flags.HasFlag(CtrDrawFlags.v))
                {
                    //push vertex from the array to the buffer
                    stack[d.stackIndex] = vfixed[vertexIndex];
                    vertexIndex++;
                }

                //push new vertex from stack
                crd[0] = crd[1];
                crd[1] = crd[2];
                crd[2] = crd[3];
                crd[3] = stack[d.stackIndex];

                //push new color
                clr[0] = clr[1];
                clr[1] = clr[2];
                clr[2] = clr[3];
                clr[3] = cols[d.colorIndex];

                /*
                tlb[0] = tlb[1];
                tlb[1] = tlb[2];
                tlb[2] = tlb[3];
                tlb[3] = (d.texIndex == 0 ? null : tl[d.texIndex - 1]);
                */

                if (d.flags.HasFlag(CtrDrawFlags.l))
                {
                    crd[1] = crd[0];
                    clr[1] = clr[0];
                }


                //if got reset flag, reset tristrip vertex counter
                if (d.flags.HasFlag(CtrDrawFlags.s))
                {
                    stripLength = 0;
                }

                //if we got 3 indices in tristrip (0,1,2)
                if (stripLength >= 2)
                {
                    //read 3 vertices and push to the array
                    for (int z = 3 - 1; z >= 0; z--)
                    {
                        Vertex v = new Vertex();
                        v.coord = new Vector3(crd[1 + z].X, crd[z + 1].Y, crd[z + 1].Z);
                        v.color = clr[1 + z];
                        v.color_morph = v.color;
                        verts.Add(v);
                    }

                    //if got normal flag, change vertex order to flip normals
                    if (d.flags.HasFlag(CtrDrawFlags.n))
                    {
                        Vertex v = verts[verts.Count - 1];
                        verts[verts.Count - 1] = verts[verts.Count - 2];
                        verts[verts.Count - 2] = v;
                    }

                    matIndices.Add(tlb[3]);
                }

                stripLength++;
            }

            for (int i = 0; i < verts.Count / 3; i++)
                verts.Reverse(i * 3, 3);

            br.Jump(returnto);
        }

        /// <summary>
        /// Exports CTR model data to OBJ format.
        /// </summary>
        /// <returns>OBJ text as string.</returns>
        public string ToObj()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#Converted to OBJ using model_reader, CTR-Tools by DCxDemo*.");
            sb.AppendLine($"#{Meta.GetVersion()}");
            sb.AppendLine("#Original models: (C) 1999, Activision, Naughty Dog.\r\n");
            sb.AppendLine($"mtllib test_{name}.mtl\r\n");

            sb.AppendLine("# textures used:");

            List<string> uniquetags = new List<string>();

            foreach (TextureLayout t in tl)
            {
                if (!uniquetags.Contains(t.Tag()))
                    uniquetags.Add(t.Tag());
            }

            foreach (string t in uniquetags)
            {
                sb.AppendLine($"# {t}.png");
            }


            sb.AppendLine("o " + name);
            foreach (Vertex v in verts)
            {
                //while the lev is scaled down by 100, ctr models are scaled down by 1000?
                sb.AppendLine("v " +
                    v.coord.X / 1000f + " " +
                    v.coord.Y / 1000f + " " +
                    v.coord.Z / 1000f + " " +
                    v.color.ToString(VecFormat.Numbers));
            }

            Console.WriteLine(matIndices.Count);
            Console.WriteLine(verts.Count / 3);

            for (int i = 0; i < verts.Count / 3; i++)
            {
                if (matIndices[i] != null)
                {
                    sb.AppendLine($"usemtl {matIndices[i].Tag()}");
                    sb.AppendLine(matIndices[i].ToObj());
                }
                else
                {
                    sb.AppendLine($"usemtl no_texture");
                    sb.AppendLine($"vt 0 0");
                    sb.AppendLine($"vt 0 1");
                    sb.AppendLine($"vt 1 1");
                    sb.AppendLine($"vt 1 0");
                }

                sb.AppendLine($"f {i * 3 + 1}/{i * 3 + 1} {i * 3 + 2}/{i * 3 + 2} {i * 3 + 3}/{i * 3 + 3}");
            }

            /*
            StringBuilder bb = new StringBuilder();

            bb.AppendLine(matIndices.Count + "");

            foreach (var tl in matIndices)
            {
                if (tl != null)
                {
                    string texname = $"texModels\\{tl.Tag()}.png";

                    bb.AppendLine($"newmtl {tl.Tag()}");
                    bb.AppendLine("Kd 2.0 2.0 2.0"); //not sure if it actually works in obj, but it's what psx does
                    bb.AppendLine($"map_Kd {texname}\r\n");
                }
            }

            Helpers.WriteToFile(Path.Combine(Meta.BasePath, $"test_{name}.mtl"), bb.ToString());
            */

            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"name: {name}");
            sb.AppendLine($"unk0: {unk0}");
            sb.AppendLine($"lodDistance: {lodDistance}");
            sb.AppendLine($"billboard: {billboard}");
            sb.AppendLine($"scale: {scale.ToString()}");
            sb.AppendLine($"ptrCmd: {ptrCmd.ToUInt32().ToString("X8")}");
            sb.AppendLine($"ptrVerts: {ptrVerts.ToUInt32().ToString("X8")}");
            sb.AppendLine($"ptrTex: {ptrTex.ToUInt32().ToString("X8")}");
            sb.AppendLine($"ptrClut: {ptrClut.ToUInt32().ToString("X8")}");
            sb.AppendLine($"unk3: {unk3}");
            sb.AppendLine($"numAnims: {numAnims}");
            sb.AppendLine($"ptrAnims: {ptrAnims.ToUInt32().ToString("X8")}");
            sb.AppendLine($"unk4: {unk4.ToString("X8")}");

            return sb.ToString();
        }



        /// <summary>
        /// Builds CTR model from raw data arrays.
        /// </summary>
        /// <param name="name">Model name.</param>
        /// <param name="vertices">Vertex array.</param>
        /// <param name="colors">Color array.</param>
        /// <param name="faces">Face indices array.</param>
        /// <returns>CtrHeader object.</returns>
        public static CtrMesh FromRawData(string name, List<Vector3f> vertices, List<Vector4b> colors, List<Vector3i> faces)
        {
            CtrMesh model = new CtrMesh();
            model.name = name + "_hi";
            model.lodDistance = -1;

            List<Vector4b> cc = new List<Vector4b>();

            foreach (var c in colors)
            {
                System.Drawing.Color cl = Tim.Convert16(Tim.ConvertTo16(System.Drawing.Color.FromArgb(c.W, c.Z, c.Y, c.X)), false);

                if (cl.R == 255 && cl.G == 0 && cl.B == 255)
                    cl = System.Drawing.Color.Black;

                cc.Add(new Vector4b(cl.R, cl.G, cl.B, 0));
            }

            colors = cc;

            //get distinct values from input lists
            List<Vector3f> dVerts = new List<Vector3f>();
            List<Vector4b> dColors = new List<Vector4b>();

            foreach (var v in vertices)
            {
                if (!dVerts.Contains(v))
                    dVerts.Add(v);
            }

            foreach (var c in colors)
            {
                if (!dColors.Contains(c))
                    dColors.Add(c);
            }


            //recalculate indices for distinct arrays
            List<Vector3i> vfaces = new List<Vector3i>();
            List<Vector3i> cfaces = new List<Vector3i>();

            if (dVerts.Count != vertices.Count)
            {
                foreach (var f in faces)
                    vfaces.Add(new Vector3i(
                        dVerts.IndexOf(vertices[f.X]),
                        dVerts.IndexOf(vertices[f.Y]),
                        dVerts.IndexOf(vertices[f.Z])
                        ));
            }

            if (dColors.Count != colors.Count)
            {
                foreach (var f in faces)
                    cfaces.Add(new Vector3i(
                        dColors.IndexOf(colors[f.X]),
                        dColors.IndexOf(colors[f.Y]),
                        dColors.IndexOf(colors[f.Z])
                        ));
            }

            if (vfaces.Count == 0) vfaces = faces;
            if (cfaces.Count == 0) cfaces = faces;

            int clutlimit = 128;

            //check for clut overflow
            if (dColors.Count > clutlimit)
            {
                Helpers.Panic("CtrHeader", PanicType.Info, "More than 128 distinct colors! Truncating...");
                dColors = dColors.GetRange(0, clutlimit);

                foreach (var x in cfaces)
                {
                    if (x.X >= clutlimit) x.X = 0;
                    if (x.Y >= clutlimit) x.Y = 0;
                    if (x.Z >= clutlimit) x.Z = 0;
                }
            }


            //get bbox
            BoundingBox bb = BoundingBox.GetBB(dVerts);

            //offset the bbox to world origin
            BoundingBox bb2 = bb - bb.minf;

            //offset all vertices to world origin
            for (int i = 0; i < dVerts.Count; i++)
                dVerts[i] -= bb.minf;

            //save converted offset to model
            model.posOffset = new Vector4s(
                (short)(bb.minf.X / bb2.maxf.X * 255),
                (short)(bb.minf.Y / bb2.maxf.Y * 255),
                (short)(bb.minf.Z / bb2.maxf.Z * 255),
                0);

            //save scale to model
            model.scale = new Vector3(
                bb2.maxf.X * 1000f,
                bb2.maxf.Y * 1000f,
                bb2.maxf.Z * 1000f
                );


            //compress vertices to byte vector 
            model.vtx.Clear();

            foreach (var v in dVerts)
            {
                Vector3b vv = new Vector3b(
                   (byte)(v.X / bb2.maxf.X * 255),
                   (byte)(v.Z / bb2.maxf.Z * 255),
                   (byte)(v.Y / bb2.maxf.Y * 255)
                    );

                model.vtx.Add(vv);
            }


            //save colors
            if (dColors.Count > 0)
            {
                model.cols = dColors;
            }
            else
            {
                model.cols.Add(new Vector4b(0x40, 0x40, 0x40, 0));
                model.cols.Add(new Vector4b(0x80, 0x80, 0x80, 0));
                model.cols.Add(new Vector4b(0xC0, 0xC0, 0xC0, 0));
            }


            //create new vertex array and loop through all faces
            List<Vector3b> newlist = new List<Vector3b>();

            for (int i = 0; i < faces.Count; i++)
            {
                CtrDraw[] cmd = new CtrDraw[3];

                cmd[0] = new CtrDraw()
                {
                    texIndex = 0,
                    colorIndex = (byte)cfaces[i].X,
                    stackIndex = 87,
                    flags = CtrDrawFlags.s | CtrDrawFlags.d //| CtrDrawFlags.k
                };

                cmd[1] = new CtrDraw()
                {
                    texIndex = 0,
                    colorIndex = (byte)cfaces[i].Z,
                    stackIndex = 88,
                    flags = CtrDrawFlags.d //| CtrDrawFlags.k
                };

                cmd[2] = new CtrDraw()
                {
                    texIndex = 0,
                    colorIndex = (byte)cfaces[i].Y,
                    stackIndex = 89,
                    flags = CtrDrawFlags.d //| CtrDrawFlags.k
                };

                newlist.Add(model.vtx[vfaces[i].X]);
                newlist.Add(model.vtx[vfaces[i].Z]);
                newlist.Add(model.vtx[vfaces[i].Y]);

                model.drawList.AddRange(cmd);
            }

            model.vtx = newlist;

            return model;
        }

        /// <summary>
        /// Loads CTR model from PLY result arrays.
        /// </summary>
        /// <param name="name">Model name.</param>
        /// <param name="ply">PlyResult object.</param>
        /// <returns>CtrHeader object.</returns>
        public static CtrMesh FromPly(string name, PlyResult ply)
        {
            List<Vector3i> faces = new List<Vector3i>();

            for (int i = 0; i < ply.Triangles.Count / 3; i++)
                faces.Add(new Vector3i(ply.Triangles[i * 3], ply.Triangles[i * 3 + 1], ply.Triangles[i * 3 + 2]));

            return FromRawData(name, ply.Vertices, ply.Colors, faces);
        }

        /// <summary>
        /// Loads CTR model from OBJ model object.
        /// </summary>
        /// <param name="name">Model name.</param>
        /// <param name="obj">OBJ object.</param>
        /// <returns>CtrHeader object.</returns>
        public static CtrMesh FromObj(string name, OBJ obj)
        {
            return CtrMesh.FromPly(name, obj.Result);
        }

        public void ExportPly(string filename)
        {
            PlyResult ply = new PlyResult(new List<Vector3f>(), new List<int>(), new List<Vector4b>());

            foreach (var v in verts)
                ply.Vertices.Add(new Vector3f(v.coord.X, v.coord.Y, v.coord.Z));
        }

        /// <summary>
        /// Writes CTR model in original CTR format.
        /// </summary>
        /// <param name="bw">BinaryWriterEx object.</param>
        /// <param name="mode">Write mode (writes wither header or data).</param>
        public void Write(BinaryWriterEx bw, CtrWriteMode mode, List<UIntPtr> patchTable)
        {
            int pos = 0;

            switch (mode)
            {
                case CtrWriteMode.Header:
                    pos = (int)bw.BaseStream.Position;
                    bw.Write(name.ToCharArray().Take(16).ToArray());
                    bw.Jump(pos + 16);
                    bw.Write(unk0);
                    bw.Write(lodDistance);
                    bw.Write(billboard);
                    bw.WriteVector3sPadded(scale);
                    bw.Write(ptrCmd, patchTable);
                    bw.Write(ptrVerts, patchTable);
                    bw.Write(ptrTex, patchTable);
                    bw.Write(ptrClut, patchTable);
                    bw.Write(unk3);
                    bw.Write(numAnims);
                    bw.Write(ptrAnims, patchTable);
                    bw.Write(unk4);

                    break;

                case CtrWriteMode.Data:

                    //write commands

                    bw.Jump(ptrCmd + 4);

                    bw.Write(cmdNum);

                    foreach (var c in drawList)
                    {
                        bw.Write(c.GetValue());
                    }

                    bw.Write(0xFFFFFFFF);


                    //write texturelayouts

                    if (tl.Count > 0)
                    {
                        bw.Jump(ptrTex + 4);

                        pos = (int)bw.BaseStream.Position;

                        for (int i = 0; i < tl.Count; i++)
                        {
                            //CtrModel.ptrs.Add((int)bw.BaseStream.Position - 4);

                            UIntPtr ptr = (UIntPtr)(pos + 4 * tl.Count + i * 12);
                            bw.Write(ptr, null);
                        }

                        foreach (var t in tl)
                            t.Write(bw);
                    }


                    //write vertices

                    bw.Jump(ptrVerts + 4);

                    posOffset.Write(bw);

                    bw.Seek(16);

                    bw.Write(vrenderMode);

                    Console.WriteLine(name);

                    foreach (var x in vtx)
                    {
                        x.Write(bw);
                        Console.WriteLine(x.X.ToString("X2") + x.Y.ToString("X2") + x.Z.ToString("X2"));
                    }

                    Console.WriteLine("---");


                    //write clut

                    bw.Jump(ptrClut + 4);

                    foreach (var x in cols)
                    {
                        x.W = 0;
                        x.Write(bw);
                        Console.WriteLine(x.X.ToString("X2") + x.Y.ToString("X2") + x.Z.ToString("X2") + x.W.ToString("X2"));
                    }


                    break;

                default: Helpers.Panic(this, PanicType.Warning, $"unimplemented mode {mode.ToString()}"); break;
            }
        }
    }
}
