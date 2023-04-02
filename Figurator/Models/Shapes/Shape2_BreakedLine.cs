using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using DynamicData;
using Figurator.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static Figurator.Models.Shapes.PropsN;

namespace Figurator.Models.Shapes
{
    public class Shape2_BreakedLine : IShape
    {
        private static readonly PropsN[] props = new[] { PName, PDots, PColor, PThickness };

        public PropsN[] Props => props;

        public string Name => "Ломанная";

        public Shape? Build(Mapper map)
        {
            if (map.GetProp(PName) is not string @name) return null;

            if (map.GetProp(PDots) is not SafePoints @dots || !@dots.Valid) return null;

            if (map.GetProp(PColor) is not string @color) return null;

            if (map.GetProp(PThickness) is not int @thickness) return null;

            return new Polyline
            {
                Name = "sn_" + @name,
                Points = @dots.Points,
                Stroke = new SolidColorBrush(Color.Parse(@color)),
                StrokeThickness = @thickness
            };
        }
        public bool Load(Mapper map, Shape shape)
        {
            if (shape is not Polyline @polyline) return false;
            if (@polyline.Name == null || !@polyline.Name.StartsWith("sn_")) return false;
            if (@polyline.Stroke == null) return false;

            if (map.GetProp(PDots) is not SafePoints @dots) return false;

            map.SetProp(PName, @polyline.Name[3..]);

            @dots.Set((Points)@polyline.Points);

            map.SetProp(PColor, ((SolidColorBrush)@polyline.Stroke).Color.ToString());
            map.SetProp(PThickness, (int)@polyline.StrokeThickness);

            return true;
        }

        public Dictionary<string, object?>? Export(Shape shape)
        {
            if (shape is not Polyline @polyline) return null;
            if (@polyline.Name == null || !@polyline.Name.StartsWith("sn_")) return null;

            return new()
            {
                ["name"] = @polyline.Name[3..],
                ["points"] = @polyline.Points,
                ["stroke"] = @polyline.Stroke,
                ["thickness"] = (int)@polyline.StrokeThickness
            };
        }
        public Shape? Import(Dictionary<string, object?> data)
        {
            if (!data.ContainsKey("name") || data["name"] is not string @name) return null;

            if (!data.ContainsKey("points") || data["points"] is not Points @dots) return null;

            if (!data.ContainsKey("stroke") || data["stroke"] is not SolidColorBrush @color) return null;
            if (!data.ContainsKey("thickness") || data["thickness"] is not short @thickness) return null;

            return new Polyline
            {
                Name = "sn_" + @name,
                Points = @dots,
                Stroke = @color,
                StrokeThickness = @thickness
            };
        }


        public Point? GetPos(Shape shape)
        {
            if (shape is not Polyline @polyline) return null;
            Point sum = new();
            foreach (var pos in @polyline.Points) sum += pos;
            return sum / @polyline.Points.Count;
        }
        public bool SetPos(Shape shape, int x, int y)
        {
            var old = GetPos(shape);
            if (old == null) return false;

            var polyline = (Polyline)shape;
            Point delta = new Point(x, y) - (Point)old;
            Points upd = new();
            for (int i = 0; i < polyline.Points.Count; i++) upd.Add(polyline.Points[i] + delta);
            polyline.Points = upd;

            return true;
        }
    }
}
