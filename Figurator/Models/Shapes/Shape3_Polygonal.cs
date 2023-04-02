using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;
using static Figurator.Models.Shapes.PropsN;

namespace Figurator.Models.Shapes {
    public class Shape3_Polygonal: IShape {
        private static readonly PropsN[] props = new[] { PName, PDots, PColor, PThickness, PFillColor };
        public PropsN[] Props => props;

        public string Name => "Многоугольник";

        public Shape? Build(Mapper map) {
            if (map.GetProp(PName) is not string @name) return null;

            if (map.GetProp(PDots) is not SafePoints @dots || !@dots.Valid) return null;

            if (map.GetProp(PColor) is not string @color) return null;

            if (map.GetProp(PFillColor) is not string @fillColor) return null;

            if (map.GetProp(PThickness) is not int @thickness) return null;

            return new Polygon {
                Name = "sn_" + @name,
                Points = @dots.Points,
                Stroke = new SolidColorBrush(Color.Parse(@color)),
                Fill = new SolidColorBrush(Color.Parse(@fillColor)),
                StrokeThickness = @thickness
            };
        }
        public bool Load(Mapper map, Shape shape) {
            if (shape is not Polygon @polygon) return false;
            if (@polygon.Name == null || !@polygon.Name.StartsWith("sn_")) return false;
            if (@polygon.Stroke == null || @polygon.Fill == null) return false;

            if (map.GetProp(PDots) is not SafePoints @dots) return false;

            map.SetProp(PName, @polygon.Name[3..]);

            @dots.Set((Points) @polygon.Points);

            map.SetProp(PColor, ((SolidColorBrush) @polygon.Stroke).Color.ToString());
            map.SetProp(PFillColor, ((SolidColorBrush) @polygon.Fill).Color.ToString());
            map.SetProp(PThickness, (int) @polygon.StrokeThickness);

            return true;
        }

        public Dictionary<string, object?>? Export(Shape shape) {
            if (shape is not Polygon @polygon) return null;
            if (@polygon.Name == null || !@polygon.Name.StartsWith("sn_")) return null;

            return new() {
                ["name"] = @polygon.Name[3..],
                ["points"] = @polygon.Points,
                ["stroke"] = @polygon.Stroke,
                ["fill"] = @polygon.Fill,
                ["thickness"] = (int) @polygon.StrokeThickness
            };
        }
        public Shape? Import(Dictionary<string, object?> data) {
            if (!data.ContainsKey("name") || data["name"] is not string @name) return null;

            if (!data.ContainsKey("points") || data["points"] is not Points @dots) return null;

            if (!data.ContainsKey("stroke") || data["stroke"] is not SolidColorBrush @color) return null;
            if (!data.ContainsKey("fill") || data["fill"] is not SolidColorBrush @fillColor) return null;
            if (!data.ContainsKey("thickness") || data["thickness"] is not short @thickness) return null;

            return new Polygon {
                Name = "sn_" + @name,
                Points = @dots,
                Stroke = @color,
                Fill = @fillColor,
                StrokeThickness = @thickness
            };
        }

        public Point? GetPos(Shape shape) { 
            if (shape is not Polygon @polygon) return null;
            Point sum = new();
            foreach (var pos in @polygon.Points) sum += pos;
            return sum / @polygon.Points.Count;
        }
        public bool SetPos(Shape shape, int x, int y) {
            var old = GetPos(shape);
            if (old == null) return false;

            var polygon = (Polygon)shape;
            Point delta = new Point(x, y) - (Point)old;
            Points upd = new(); 
            for (int i = 0; i < polygon.Points.Count; i++) upd.Add(polygon.Points[i] + delta);
            polygon.Points = upd;

            return true;
        }
    }
}