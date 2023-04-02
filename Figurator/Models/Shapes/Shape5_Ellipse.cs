using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;
using static Figurator.Models.Shapes.PropsN;

namespace Figurator.Models.Shapes {
    public class Shape5_Ellipse: IShape {
        private static readonly PropsN[] props = new[] { PName, PCenterDot, PHorizDiagonal, PVertDiagonal, PColor, PThickness, PFillColor };

        public PropsN[] Props => props;

        public string Name => "Эллипс";

        public Shape? Build(Mapper map) {
            if (map.GetProp(PName) is not string @name) return null;

            if (map.GetProp(PCenterDot) is not SafePoint @center || !@center.Valid) return null;

            if (map.GetProp(PVertDiagonal) is not SafeNum @width || !@width.Valid) return null;

            if (map.GetProp(PHorizDiagonal) is not SafeNum @height || !@height.Valid) return null;

            if (map.GetProp(PColor) is not string @color) return null;

            if (map.GetProp(PFillColor) is not string @fillColor) return null;

            if (map.GetProp(PThickness) is not int @thickness) return null;

            var p = @center.Point;
            int w = @width.Num;
            int h = @height.Num;

            return new Ellipse {
                Name = "sn_" + @name,
                Margin = new(p.X - w/2, p.Y - h/2, 0, 0), 
                Width = w,
                Height = h,
                Stroke = new SolidColorBrush(Color.Parse(@color)),
                Fill = new SolidColorBrush(Color.Parse(@fillColor)),
                StrokeThickness = @thickness
            };
        }
        public bool Load(Mapper map, Shape shape) {
            if (shape is not Ellipse @ellipse) return false;
            if (@ellipse.Name == null || !@ellipse.Name.StartsWith("sn_")) return false;
            if (@ellipse.Stroke == null || @ellipse.Fill == null) return false;

            if (map.GetProp(PCenterDot) is not SafePoint @start) return false;
            if (map.GetProp(PVertDiagonal) is not SafeNum @width) return false;
            if (map.GetProp(PHorizDiagonal) is not SafeNum @height) return false;

            map.SetProp(PName, @ellipse.Name[3..]);

            short w = (short) @ellipse.Width;
            short h = (short) @ellipse.Height;

            @start.Set(new Point(@ellipse.Margin.Left + w/2, @ellipse.Margin.Top + h/2));
            @width.Set(w);
            @height.Set(h);

            map.SetProp(PColor, ((SolidColorBrush) @ellipse.Stroke).Color.ToString());
            map.SetProp(PFillColor, ((SolidColorBrush) @ellipse.Fill).Color.ToString());
            map.SetProp(PThickness, (int) @ellipse.StrokeThickness);

            return true;
        }

        public Dictionary<string, object?>? Export(Shape shape) {
            if (shape is not Ellipse @ellipse) return null;
            if (@ellipse.Name == null || !@ellipse.Name.StartsWith("sn_")) return null;

            return new() {
                ["name"] = @ellipse.Name[3..],
                ["margin"] = @ellipse.Margin,
                ["width"] = (short) @ellipse.Width,
                ["height"] = (short) @ellipse.Height,
                ["stroke"] = @ellipse.Stroke,
                ["fill"] = @ellipse.Fill,
                ["thickness"] = (short) @ellipse.StrokeThickness
            };
        }
        public Shape? Import(Dictionary<string, object?> data) {
            if (!data.ContainsKey("name") || data["name"] is not string @name) return null;

            if (!data.ContainsKey("margin") || data["margin"] is not Thickness @margin) return null;
            if (!data.ContainsKey("width") || data["width"] is not short @width) return null;
            if (!data.ContainsKey("height") || data["height"] is not short @height) return null;

            if (!data.ContainsKey("stroke") || data["stroke"] is not SolidColorBrush @color) return null;
            if (!data.ContainsKey("fill") || data["fill"] is not SolidColorBrush @fillColor) return null;
            if (!data.ContainsKey("thickness") || data["thickness"] is not short @thickness) return null;

            return new Ellipse {
                Name = "sn_" + @name,
                Margin = @margin,
                Width = @width,
                Height = @height,
                Stroke = @color,
                Fill = @fillColor,
                StrokeThickness = @thickness
            };
        }

        public Point? GetPos(Shape shape) {
            if (shape is not Ellipse @ellipse) return null;
            Point pos = new(@ellipse.Margin.Left, @ellipse.Margin.Top);
            return pos + new Point(@ellipse.Width, @ellipse.Height) / 2;
        }
        public bool SetPos(Shape shape, int x, int y) {
            var old = GetPos(shape);
            if (old == null) return false;

            var ellipse = (Ellipse)shape;
            Point delta = new Point(x, y) - (Point)old;
            ellipse.Margin = new Thickness(ellipse.Margin.Left + delta.X, ellipse.Margin.Top + delta.Y);

            return true;
        }
    }
}