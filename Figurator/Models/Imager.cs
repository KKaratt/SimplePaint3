using Avalonia;
using Avalonia.Media;
using Figurator.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Color = Avalonia.Media.Color;

namespace Figurator.Models {
    public class Imager {
        public readonly static string[] colors = new[] {
            "Yellow", "Blue", "Green", "Red",
            "Orange", "Brown", "Pink", "Aqua",
            "Lime",
            "White", "LightGray", "DarkGray", "Black"
        };
        private readonly static byte[][] rgbs = colors.Select(Color.Parse).Select(x => new byte[] { x.R, x.G, x.B }).ToArray();

        private unsafe static void Dithering(BitmapData bd) {
            int size = bd.Width * bd.Height;
            var data = (byte*) bd.Scan0;

            for (int i = 0; i < size; i++) {
                byte* r = data++;
                byte* g = data++;
                byte* b = data++;

                int best_ci = 0, min_dist = 1000000000;
                for (int ci = 0; ci < rgbs.Length; ci++) {
                    var c = rgbs[ci];
                    int Rd = c[0] - *r, Gd = c[1] - *g, Bd = c[2] - *b;
                    int dist = (int)(Rd * Rd * 0.299 + Gd * Gd * 0.587 + Bd * Bd * 0.114);
                    
                    if (dist < min_dist) {
                        min_dist = dist;
                        best_ci = ci;
                    }
                }

                var cc = rgbs[best_ci];
                *r = cc[0];
                *g = cc[1];
                *b = cc[2];
            }
        }

        private unsafe static void Blur(BitmapData bd) {
            int w = bd.Width, h = bd.Height;
            var data = (byte*) bd.Scan0;
            int[][] nbs = { new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } };

            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    int r = 0, g = 0, b = 0, c = 0;
                    foreach (var nb in nbs) {
                        int nx = x + nb[0], ny = y + nb[1];
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        var pix = data + (3 * (nx + ny * w));
                        r += pix[0];
                        g += pix[1];
                        b += pix[2];
                        c += 1;
                    }
                    var pix2 = data + (3 * (x + y * w));
                    pix2[0] = (byte) (r / c);
                    pix2[1] = (byte) (g / c);
                    pix2[2] = (byte) (b / c);
                }
            }
        }

        class PixShape {
            public readonly int[] shape, border;
            public readonly int weight;

            public PixShape(int[] s, int[] b) {
                shape = s;
                border = b;
                weight = s.Length;
            }
        }
        class NoiseDestroyerRes {
            public List<PixShape> groups;
            public int yeahs, nops;

            public NoiseDestroyerRes(List<PixShape> groups, int yeahs, int nops) {
                this.groups = groups;
                this.yeahs = yeahs;
                this.nops = nops;
            }
        }
        private unsafe static NoiseDestroyerRes NoiseDestroyer(BitmapData bd, int threshold) {
            int w = bd.Width, h = bd.Height, groups_count = 0, size = w * h;
            var data = (byte*) bd.Scan0;
            var g_mat = new int[w * h];

            Stack<int> stack = new();
            List<PixShape> groups = new();

            for (int i = 0; i < size; i++) {
                if (g_mat[i] != 0) continue;

                var c = data + (3 * i);
                int cur_c = c[0] << 16 | c[1] << 8 | c[2];
                groups_count++;

                stack.Push(i);
                List<int> shape = new();
                List<int> border = new();

                while (stack.Count > 0) {
                    int pos = stack.Pop();

                    c = data + (3 * pos);
                    if (cur_c != (c[0] << 16 | c[1] << 8 | c[2])) {
                        border.Add(pos);
                        continue;
                    }

                    g_mat[pos] = groups_count;
                    shape.Add(pos);

                    int x = pos % w, y = pos / w;
                    if (x > 0)
                        if (g_mat[pos - 1] == 0) stack.Push(pos - 1);
                        else border.Add(pos - 1);
                    if (y > 0)
                        if (g_mat[pos - w] == 0) stack.Push(pos - w);
                        else border.Add(pos - w);
                    if (x + 1 < w)
                        if (g_mat[pos + 1] == 0) stack.Push(pos + 1);
                        else border.Add(pos + 1);
                    if (y + 1 < h)
                        if (g_mat[pos + w] == 0) stack.Push(pos + w);
                        else border.Add(pos + w);
                }

                groups.Add(new(shape.ToArray(), border.ToArray()));
            }

            int yeahs = 0, nops = 0;
            foreach (var shape in groups) {
                if (shape.weight >= threshold) continue;

                Dictionary<int, int> colors = new();
                foreach (var pos in shape.border) {
                    var gr = groups[g_mat[pos] - 1];
                    if (gr.weight < threshold) continue;

                    var c = data + (3 * pos);
                    int clr = c[0] << 16 | c[1] << 8 | c[2];
                    if (colors.TryGetValue(clr, out int value)) value++;
                    else colors.Add(clr, 1);
                }

                if (colors.Count == 0) {
                    nops++;
                    continue;
                }
                var best_clr = colors.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                byte r = (byte) (best_clr >> 16);
                byte g = (byte) (best_clr >> 8);
                byte b = (byte) best_clr;

                foreach (var pos in shape.shape) {
                    var c = data + (3 * pos);
                    c[0] = r;
                    c[1] = g;
                    c[2] = b;
                }
                yeahs++;
            }

            return new(groups, yeahs, nops);
        }

        private unsafe static int BurrDestroyer(BitmapData bd) {
            int w = bd.Width, h = bd.Height, size = w * h;
            var data = (byte*) bd.Scan0;

            Dictionary<int, int> buff = new();

            for (int pos = 0; pos < size; pos++) {
                var clr = data + (3 * pos);
                int C = clr[0] << 16 | clr[1] << 8 | clr[2], origC = C;

                int x = pos % w, y = pos / w;

                clr = data + 3 * (x > 0 ? pos - 1 : pos);
                int L = clr[0] << 16 | clr[1] << 8 | clr[2];

                clr = data + 3 * (y > 0 ? pos - w : pos);
                int U = clr[0] << 16 | clr[1] << 8 | clr[2];

                clr = data + 3 * (x + 1 < w ? pos + 1 : pos);
                int R = clr[0] << 16 | clr[1] << 8 | clr[2];

                clr = data + 3 * (y + 1 < h ? pos + w : pos);
                int D = clr[0] << 16 | clr[1] << 8 | clr[2];

                if (L == C && C == R && U == D) C = U;
                else if (U == C && C == D && L == R) C = L;

                else if (U == L && L == D && C == R) C = U;
                else if (L == U && U == R && C == D) C = L;
                else if (U == R && R == D && C == L) C = U;
                else if (L == D && D == R && C == U) C = L;

                if (C != origC) buff[pos] = C;
            }

            foreach (var entry in buff) {
                var clr = data + (3 * entry.Key);
                int C = entry.Value;
                clr[0] = (byte) (C >> 16);
                clr[1] = (byte) (C >> 8);
                clr[2] = (byte) C;
            }
            return buff.Count;
        }


        private unsafe static void Contrast(BitmapData bd, int contrast) {
            int size = bd.Width * bd.Height;
            var data = (byte*) bd.Scan0;

            int midBright = 0;
            byte *d = data;
            for (int i = 0; i < size; i++) midBright += *d++ * 77 + *d++ * 150 + *d++ * 29;
            midBright /= 256 * size / 3;

            var buf = new byte[256];
            for (int i = 0; i < 256; i++) {
                int a = (((i - midBright) * contrast) >> 8) + midBright;
                buf[i] = a < 0 ? (byte) 0 : a > 255 ? (byte) 255 : (byte) a;
            }
            for (var i = data; i < data + size * 3; i++) *i = buf[*i];
        }

        public static void Import() {
            string name = "Export.png";

            if (!File.Exists("../../../" + name)) { Log.Write(name + " не обнаружен"); return; }
            var bmp = new Bitmap("../../../" + name);
            var w = bmp.Width;
            var h = bmp.Height;
            Log.Write("Size: " + w + "x" + h);
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Contrast(bd, 256*2);

            try { Dithering(bd); }
            catch (Exception e) { Log.Write("Error: " + e); return; }

            bmp.UnlockBits(bd);
            bmp.Save("../../../Res.png", ImageFormat.Png);

            Log.Write("OK");
        }
    }
}
