using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;
using static Figurator.Models.Shapes.PropsN;

namespace Figurator.Models.Shapes {
    public class Shape4_Rectangle: IShape {
        private static readonly PropsN[] props = new[] { PName, PStartDot, PWidth, PHeight, PColor, PThickness, PFillColor };

        public PropsN[] Props => props;

        public string Name => "Прямоугольник";

        public Shape? Build(Mapper map) {
            if (map.GetProp(PName) is not string @name) return null;

            if (map.GetProp(PStartDot) is not SafePoint @start || !@start.Valid) return null;

            if (map.GetProp(PWidth) is not SafeNum @width || !@width.Valid) return null;

            if (map.GetProp(PHeight) is not SafeNum @height || !@height.Valid) return null;

            if (map.GetProp(PColor) is not string @color) return null;

            if (map.GetProp(PFillColor) is not string @fillColor) return null;

            if (map.GetProp(PThickness) is not int @thickness) return null;

            var p = @start.Point;

            return new Rectangle {
                Name = "sn_" + @name,
                Margin = new(p.X, p.Y, 0, 0), 
                Width = @width.Num,
                Height = @height.Num,
                Stroke = new SolidColorBrush(Color.Parse(@color)),
                Fill = new SolidColorBrush(Color.Parse(@fillColor)),
                StrokeThickness = @thickness
            };
        }
        public bool Load(Mapper map, Shape shape) {
            if (shape is not Rectangle @rect) return false;
            if (@rect.Name == null || !@rect.Name.StartsWith("sn_")) return false;
            if (@rect.Stroke == null || @rect.Fill == null) return false;

            if (map.GetProp(PStartDot) is not SafePoint @start) return false;
            if (map.GetProp(PWidth) is not SafeNum @width) return false;
            if (map.GetProp(PHeight) is not SafeNum @height) return false;

            map.SetProp(PName, @rect.Name[3..]);

            @start.Set(new Point(@rect.Margin.Left, @rect.Margin.Top));
            @width.Set((short) @rect.Width);
            @height.Set((short) @rect.Height);

            map.SetProp(PColor, ((SolidColorBrush) @rect.Stroke).Color.ToString());
            map.SetProp(PFillColor, ((SolidColorBrush) @rect.Fill).Color.ToString());
            map.SetProp(PThickness, (int) @rect.StrokeThickness);

            return true;
        }

        public Dictionary<string, object?>? Export(Shape shape) {
            if (shape is not Rectangle @rect) return null;
            if (@rect.Name == null || !@rect.Name.StartsWith("sn_")) return null;

            return new() {
                ["name"] = @rect.Name[3..],
                ["margin"] = @rect.Margin,
                ["width"] = (short) @rect.Width,
                ["height"] = (short) @rect.Height,
                ["stroke"] = @rect.Stroke,
                ["fill"] = @rect.Fill,
                ["thickness"] = (short) @rect.StrokeThickness
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

            return new Rectangle {
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
            if (shape is not Rectangle @rect) return null;
            Point pos = new(@rect.Margin.Left, @rect.Margin.Top);
            return pos + new Point(@rect.Width, @rect.Height) / 2;
        }
        public bool SetPos(Shape shape, int x, int y) {
            var old = GetPos(shape);
            if (old == null) return false;

            var rect = (Rectangle)shape;
            Point delta = new Point(x, y) - (Point)old;
            rect.Margin = new Thickness(rect.Margin.Left + delta.X, rect.Margin.Top + delta.Y);

            return true;
        }
    }
}
