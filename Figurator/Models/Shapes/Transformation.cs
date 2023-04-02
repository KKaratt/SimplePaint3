using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Figurator.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Figurator.Models.Shapes {
    public class Transformation {

        public SafeNum rotateTransformAngle;
        public SafePoint rotateTransformCenter;
        public SafeDPoint scaleTransform;
        public SafePoint skewTransform;
        public ObservableCollection<string> matrix = new();

        public Transformation(Action<object?>? upd, object? inst) {
            rotateTransformAngle = new(0, upd, inst);
            rotateTransformCenter = new(0, 0, upd, inst, true);
            scaleTransform = new(1, 1, upd, inst, true);
            skewTransform = new(0, 0, upd, inst, true);
            for (int i = 0; i < 6; i++) matrix.Add("?");
        }

        public void Transform(Shape shape, bool preview) {
            var group = new TransformGroup();

            if (rotateTransformAngle.Valid && rotateTransformCenter.Valid) {
                double angle = 0, centerX = 0, centerY = 0;
                if (rotateTransformAngle.Valid) angle = rotateTransformAngle.Num;
                if (rotateTransformCenter.Valid) {
                    var p = rotateTransformCenter.Point;
                    centerX = p.X;
                    centerY = p.Y;
                }
                group.Children.Add(new RotateTransform(angle, centerX, centerY));
            }

            if (scaleTransform.Valid) {
                var p = scaleTransform.Point;
                group.Children.Add(new ScaleTransform(p.X, p.Y));
            }

            if (skewTransform.Valid) {
                var p = skewTransform.Point;
                group.Children.Add(new SkewTransform(p.X, p.Y));
            }

            if (preview) {
                var mat = group.Value;
                matrix[0] = mat.M11.ToString("0.#####");
                matrix[1] = mat.M12.ToString("0.#####");
                matrix[2] = mat.M21.ToString("0.#####");
                matrix[3] = mat.M22.ToString("0.#####");
                matrix[4] = mat.M31.ToString("0.#####");
                matrix[5] = mat.M32.ToString("0.#####");
            }
            shape.RenderTransform = group;
        }
        public void Disassemble(Shape shape) {
            rotateTransformAngle.Set(0);
            rotateTransformCenter.Set(0, 0);
            scaleTransform.Set(1, 1);
            skewTransform.Set(0, 0);

            if (shape.RenderTransform is not TransformGroup @group) return;

            foreach (var el in @group.Children)
                switch (el) { 
                case RotateTransform @rotate:
                    rotateTransformAngle.Set((int) @rotate.Angle);
                    rotateTransformCenter.Set((int) @rotate.CenterX, (int) @rotate.CenterY);
                    break;
                case ScaleTransform @scale:
                    scaleTransform.Set(@scale.ScaleX, @scale.ScaleY);
                    break;
                case SkewTransform @skew:
                    skewTransform.Set((int) @skew.AngleX, (int) @skew.AngleY);
                    break;
                }
        }

        public static Dictionary<string, object?> Export(Shape shape) {
            Dictionary<string, object?> dict = new();
            if (shape.RenderTransform is not TransformGroup @group) return dict;

            foreach (var el in @group.Children)
                switch (el) { 
                case RotateTransform @rotate:
                    if (@rotate.Angle != 0)
                        dict["rotate"] = (int) @rotate.Angle + " " + (int) @rotate.CenterX + " " + (int) @rotate.CenterY;
                    break;
                case ScaleTransform @scale:
                    var str = @scale.ScaleX.ToString("0.#####") + " " + @scale.ScaleY.ToString("0.#####");
                    if (str != "1 1") dict["scale"] = str;
                    break;
                case SkewTransform @skew:
                    var str2 = (int) @skew.AngleX + " " + (int) @skew.AngleY;
                    if (str2 != "0 0") dict["skew"] = str2;
                    break;
                }

            return dict;
        }
        public static void Import(Shape shape, Dictionary<string, object?> dict) {
            var group = new TransformGroup();

            foreach (var entry in dict)
                switch(entry.Key) {
                case "rotate":
                    if (entry.Value is not string @rotate) { Log.Write("Неверный тип значения rotate ключа"); break; }
                    try {
                        var trio = new SafeTrio(@rotate, null, null, true);
                        group.Children.Add(new RotateTransform(trio.X, trio.Y, trio.Z));
                    } catch (FormatException fe) { Log.Write("rotate format error:\n" + fe); break; }
                    break;

                case "scale":
                    if (entry.Value is not string @scale) { Log.Write("Неверный тип значения scale ключа"); break; }
                    try {
                        var p = new SafeDPoint(@scale, null, null, true).Point;
                        group.Children.Add(new ScaleTransform(p.X, p.Y));
                    } catch (FormatException fe) { Log.Write("scale format error:\n" + fe); break; }
                    break;

                case "skew":
                    if (entry.Value is not string @skew) { Log.Write("Неверный тип значения skew ключа"); break; }
                    try {
                        var p = new SafePoint(@skew, null, null, true).Point;
                        group.Children.Add(new SkewTransform(p.X, p.Y));
                    } catch (FormatException fe) { Log.Write("skew format error:\n" + fe); break; }
                    break;
                }

            shape.RenderTransform = group;
        }

        public static ScaleTransform GetScale(Shape shape) {
            if (shape.RenderTransform is not TransformGroup @group) shape.RenderTransform = @group = new TransformGroup();
            foreach (var el in @group.Children)
                if (el is ScaleTransform @res) return @res;
            var res2 = new ScaleTransform(1, 1);
            group.Children.Add(res2);
            return res2;
        }
    }
}