﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTRFramework;
using CTRFramework.Vram;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ctrviewer.Engine.Render;
using ctrviewer.Engine;

namespace ctrviewer.Loaders
{
    public class CrashTeamRacingLevel : MGLevel
    {
        public static MGLevel FromScene(Scene s, Detail detail)
        {
            CrashTeamRacingLevel level = new CrashTeamRacingLevel();
            level.LoadCrashTeamRacingScene(s, detail);

            return level as MGLevel;
        }

        private void LoadCrashTeamRacingScene(Scene s, Detail detail)
        {
            List<VertexPositionColorTexture> monolist = new List<VertexPositionColorTexture>();
            List<CTRFramework.Vertex> vts = new List<Vertex>();

            switch (detail)
            {
                case Detail.Low:
                    {
                        Console.WriteLine("doin low");

                        foreach (QuadBlock qb in s.quads)
                        {
                            monolist.Clear();
                            vts = qb.GetVertexListq(s.verts, -1);

                            if (vts != null)
                            {
                                foreach (Vertex cv in vts)
                                    monolist.Add(DataConverter.ToVptc(cv, cv.uv));

                                TextureLayout t = qb.texlow;

                                string texTag = t.Tag();

                                foreach (QuadFlags fl in (QuadFlags[])Enum.GetValues(typeof(QuadFlags)))
                                {
                                    if (qb.quadFlags.HasFlag(fl))
                                        Push(flagq, fl.ToString(), monolist, "flag");
                                }

                                if (qb.isWater)
                                {
                                    Push(waterq, "water", monolist);
                                    continue;
                                }

                                if (qb.quadFlags.HasFlag(QuadFlags.InvisibleTriggers))
                                {
                                    Push(flagq, "invis", monolist);
                                    continue;
                                }

                                if (ContentVault.alphalist.Contains(texTag))
                                {
                                    Push(alphaq, texTag, monolist);
                                }
                                else
                                {
                                    Push(normalq, texTag, monolist);
                                }
                            }
                        }

                        break;
                    }

                case Detail.Med:
                    {
                        Console.WriteLine("doin med");

                        foreach (QuadBlock qb in s.quads)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                monolist.Clear();
                                vts = qb.GetVertexListq(s.verts, j);

                                if (vts != null)
                                {
                                    foreach (Vertex cv in vts)
                                        monolist.Add(DataConverter.ToVptc(cv, cv.uv));

                                    bool isAnimated = false;
                                    string texTag = "test";

                                    if (qb.ptrTexMid[j] != UIntPtr.Zero)
                                    {
                                        isAnimated = qb.tex[j].isAnimated;
                                        if (texTag != "00000000")
                                            texTag = qb.tex[j].midlods[2].Tag();
                                    }

                                    foreach (QuadFlags fl in (QuadFlags[])Enum.GetValues(typeof(QuadFlags)))
                                    {
                                        if (qb.quadFlags.HasFlag(fl))
                                            Push(flagq, fl.ToString(), monolist, "flag");
                                    }

                                    if (qb.isWater)
                                    {
                                        Push(waterq, "water", monolist);
                                        continue;
                                    }

                                    if (qb.quadFlags.HasFlag(QuadFlags.InvisibleTriggers))
                                    {
                                        Push(flagq, "invis", monolist);
                                        continue;
                                    }

                                    Push((isAnimated ? animatedq : (ContentVault.alphalist.Contains(texTag) ? alphaq : normalq)), texTag, monolist);

                                    if (isAnimated)
                                        animatedq[texTag].ScrollingEnabled = true;
                                }
                            }
                        }

                        break;
                    }
            }

            Seal();
        }
    }
}