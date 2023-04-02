using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Figurator.Models.Shapes;
using Figurator.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Figurator.Models.Shapes.PropsN;

namespace Figurator.Models
{
    public class Mapper
    {
        public string shapeName = "Линия 1";

        public string shapeColor = "Blue";
        public string shapeFillColor = "Yellow";
        public int shapeThickness = 2;

        public SafeNum shapeWidth;
        public SafeNum shapeHeight;
        public SafeNum shapeHorizDiagonal;
        public SafeNum shapeVertDiagonal;

        public SafePoint shapeStartDot;
        public SafePoint shapeEndDot;
        public SafePoint shapeCenterDot;

        public SafePoints shapeDots;

        public SafeGeometry shapeCommands;

        public Transformation tformer;

        private readonly Action<object?>? UPD;
        private readonly object? INST;

        public readonly ObservableCollection<ShapeListBoxItem> shapes = new();
        private readonly Dictionary<string, ShapeListBoxItem> name2shape = new();

        public Mapper(Action<object?>? upd, object? inst)
        {
            shapeWidth = new(200, Update, this);
            shapeHeight = new(100, Update, this);
            shapeHorizDiagonal = new(100, Update, this);
            shapeVertDiagonal = new(200, Update, this);

            shapeStartDot = new(50, 50, Update, this);
            shapeEndDot = new(100, 100, Update, this);
            shapeCenterDot = new(150, 150, Update, this);

            shapeDots = new("50,50 100,100 50,100 100,50", Update, this);

            shapeCommands = new("M 10 70 l 30,30 10,10 35,0 0,-35 m 50 0 l 0,-50 10,0 35,35 m 50 0 l 0,-50 10,0 35,35z m 70 0 l 0,30 30,0 5,-35z", Update, this);

            tformer = new(upd, inst);
            /* 
             * Вывод: какой-то Geometry недоделанный...:
             * 1.) Geometry.Stringify нет :///////////////,
             * 2.) у Geometry.Parse нет второго параметра типа bool,
             * 3.) F x u штучки не поддерживаются :///, что позволяют добавлять всяки тени, отключать заливку и т.д...
             *      F - включает тень
             *      u - отрубает отрисовку заливки
             *      x - сбрасывает выше-перечисленные параметры
             * Возможно я GoDiagram абилку путаю с ванильной авалонией))) Ток ща заметил, что это не совсем та авалония... ;'-}
             * 4.) А, и ещё... НЕТ НОРМАЛИЗАЦИИ!!! :/// А, фух! M-параметр является глобальным! ;'-}
             */

            UPD = upd;
            INST = inst;
        }
        private void Update()
        {
            UPD?.Invoke(INST);
        }
        private static void Update(object? me)
        {
            if (me != null && me is Mapper @map) @map.Update();
        }

        private static IShape[] Shapers => new IShape[] {
            new Shape1_Line(),
            new Shape2_BreakedLine(),
            new Shape3_Polygonal(),
            new Shape4_Rectangle(),
            new Shape5_Ellipse(),
            new Shape6_CompositeFigure(),
        };
        private static Dictionary<string, IShape> TShapers => new(Shapers.Select(shaper => new KeyValuePair<string, IShape>(shaper.Name, shaper)));

        private IShape cur_shaper = Shapers[0];
        private readonly Dictionary<string, Shape> shape_dict = new();
        public string? newName = null; // Обрабатывается в конечном Update'е
        public short select_shaper = -1; // Обрабатывается в конечном Update'е
        private bool update_name_lock = false;
        public bool update_marker_lock = false; // Обрабатывается в конечном Update'е

        public void ChangeFigure(int n)
        {
            cur_shaper = Shapers[n];
            if (!update_name_lock) newName = GenName(cur_shaper.Name);
            Update();
        }

        internal object GetProp(PropsN num)
        {
            return num switch
            {
                PName => shapeName,
                PColor => shapeColor,
                PFillColor => shapeFillColor,
                PThickness => shapeThickness,
                PWidth => shapeWidth,
                PHeight => shapeHeight,
                PHorizDiagonal => shapeHorizDiagonal,
                PVertDiagonal => shapeVertDiagonal,
                PStartDot => shapeStartDot,
                PEndDot => shapeEndDot,
                PCenterDot => shapeCenterDot,
                PDots => shapeDots,
                PCommands => shapeCommands,
                _ => 0
            };
        }
        internal void SetProp(PropsN num, object obj)
        {
            switch (num)
            {
                case PName: shapeName = (string)obj; break;
                case PColor: shapeColor = (string)obj; break;
                case PFillColor: shapeFillColor = (string)obj; break;
                case PThickness: shapeThickness = (int)obj; break;
                    /* Можно заменить SetProp(..., obj) на GetProp(...).Set(obj)
                    case PWidth: shapeWidth = (SafeNum) obj; break;
                    case PHeight: shapeHeight = (SafeNum) obj; break;
                    case PHorizDiagonal: shapeHorizDiagonal = (SafeNum) obj; break;
                    case PVertDiagonal: shapeVertDiagonal = (SafeNum) obj; break;
                    case PStartDot: shapeStartDot = (SafePoint) obj; break;
                    case PEndDot: shapeEndDot = (SafePoint) obj; break;
                    case PCenterDot: shapeCenterDot = (SafePoint) obj; break;
                    case PDots: shapeDots = (SafePoints) obj; break;
                    case PCommands: shapeCommands = (SafeGeometry) obj; break;*/
            };
        }

        public bool ValidInput()
        {
            foreach (PropsN num in cur_shaper.Props)
                if (GetProp(num) is ISafe @prop && !@prop.Valid) return false;
            return true;
        }
        public bool ValidName() => !shape_dict.ContainsKey(shapeName);

        private string GenName(string prefix)
        {
            prefix += " ";
            int n = 1;
            while (true)
            {
                string res = prefix + n;
                if (!shape_dict.ContainsKey(res)) return res;
                n += 1;
            }
        }
        private void AddShape(Shape newy, string? name = null)
        {
            name ??= shapeName; // Согл... XD    if (name == null) name = shapeName;    было изначально

            shape_dict[name] = newy;
            var item = new ShapeListBoxItem(name, this);
            shapes.Add(item);
            name2shape[name] = item;
        }

        public Shape? Create(bool preview)
        {
            Shape? newy = cur_shaper.Build(this);
            if (newy == null) return null;
            tformer.Transform(newy, preview);

            if (preview)
            {
                newy.Name = "sn_marker";
                return newy;
            }

            if (name2shape.TryGetValue(shapeName, out var value)) Remove(value);

            AddShape(newy);

            newName = GenName(cur_shaper.Name);
            return newy;
        }

        internal void Remove(ShapeListBoxItem item)
        {
            var Name = item.Name;
            if (!shape_dict.ContainsKey(Name)) return;

            var shape = shape_dict[Name];
            if (shape == null || shape.Parent is not Canvas @c) return;

            @c.Children.Remove(shape);
            shapes.Remove(item);
            name2shape.Remove(Name);
            shape_dict.Remove(Name);

            newName = GenName(cur_shaper.Name);
            Update();
        }

        public void Clear()
        {
            foreach (var item in shape_dict)
            {
                var shape = item.Value;
                if (shape == null || shape.Parent is not Canvas @c) continue;
                @c.Children.Clear();
            }
            shapes.Clear();
            name2shape.Clear();
            shape_dict.Clear();

            newName = GenName(cur_shaper.Name);
            Update();
        }

        public void Export(bool is_xml)
        {
            List<object> data = new();
            foreach (var item in shape_dict)
            {
                var shape = item.Value;
                // Log.Write("shape: " + shape);
                bool R = true;
                foreach (var shaper in Shapers)
                {
                    var res = shaper.Export(shape);
                    // Log.Write("  res: " + res);
                    if (res != null)
                    {
                        res["type"] = shaper.Name;

                        var tform = Transformation.Export(shape);
                        if (tform.Count > 0) res["transform"] = tform;

                        data.Add(res);
                        R = false;
                        break;
                    }
                }
                if (R) Log.Write("Потеряна одна из фигур при экспортировании :/");
            }
            if (is_xml)
            {
                var xml = Utils.Obj2xml(data);
                if (xml == null) { Log.Write("Не удалось экспортировать в Export.xml :/"); return; }
                // Log.Write("X: " + xml);
                File.WriteAllText("../../../Export.xml", xml);
            }
            else
            {
                var json = Utils.Obj2json(data);
                if (json == null) { Log.Write("Не удалось экспортировать в Export.json :/"); return; }
                // Log.Write("J: " + json);
                File.WriteAllText("../../../Export.json", json);
            }
        }

        public Shape[]? Import(bool is_xml, object? content = null)
        {
            string name = is_xml ? "Export.xml" : "Export.json";
            if (content == null)
            {
                if (!File.Exists("../../../" + name)) { Log.Write(name + " не обнаружен"); return null; }

                var data = File.ReadAllText("../../../" + name);
                // Log.Write("data: " + (is_xml ? Utils.Xml2json(data) : data));

                content = is_xml ? Utils.Xml2obj(data) : Utils.Json2obj(data);
            }
            if (content is not List<object?> @list) { Log.Write("В начале " + name + " не список"); return null; }

            List<Shape> res = new();
            Clear();

            foreach (object? item in @list)
            {
                if (item is not Dictionary<string, object?> @dict) { Log.Write("Одна из фигур при импорте - не словарь"); continue; }
                // Log.Write("D: " + @dict); // Работает!!!

                if (!@dict.ContainsKey("type") || @dict["type"] is not string @type) { Log.Write("Нет поля type, либо оно - не строка"); continue; }
                if (!@dict.ContainsKey("name") || @dict["name"] is not string @shapeName) { Log.Write("Нет поля name, либо оно - не строка"); continue; }
                if (!TShapers.ContainsKey(@type)) { Log.Write("Фигуратор " + @type + " не обнаружен :/"); continue; }

                var shaper = TShapers[@type];
                var newy = shaper.Import(@dict);
                if (newy == null) { Log.Write("Не получилось собрть фигуру " + Utils.Obj2json(@dict)); continue; }

                if (@dict.TryGetValue("transform", out object? tform))
                    if (tform is not Dictionary<string, object?> @dict2) Log.Write("У одной из фигур при импорте transform - не словарь");
                    else Transformation.Import(newy, @dict2);
                // Log.Write("N: " + @type);
                AddShape(newy, shapeName);

                res.Add(newy);
            }

            newName = GenName(cur_shaper.Name);
            return res.ToArray();
        }

        public void Select(ShapeListBoxItem? shapeItem)
        {
            // Log.Write("sel: " + shapeItem + " | " + (shapeItem == null));
            if (shapeItem == null) return;

            var shape = shape_dict[shapeItem.Name];
            bool yeah = false;
            short n = 0;
            foreach (var shaper in Shapers)
            {
                yeah = shaper.Load(this, shape);
                if (yeah) break;
                n++;
            }
            if (yeah)
            {
                if (shape.Name != null && shape.Name.StartsWith("sn_")) SetProp(PName, shape.Name[3..]);
                // Log.Write("Удачно");
                tformer.Disassemble(shape);
                update_name_lock = true;
                select_shaper = n;
                Update();
                update_name_lock = false;
            }
            else Log.Write("Не удалось распаковать фигуру :/");
        }

        /*
         * Действия с мышью над фигурами
         */

        public ShapeListBoxItem? ShapeTap(string name)
        {
            if (name.StartsWith("sn_")) name = name[3..];
            else if (name.StartsWith("sn|")) name = Utils.Base64Decode(name.Split('|')[1]);
            else return null;

            if (name2shape.TryGetValue(name, out var item))
            {
                Select(item);
                return item;
            }
            return null;
        }

        public void WheelMove(Shape shape, double move)
        {
            var scale = Transformation.GetScale(shape);
            move = move < 0 ? 1.1 : 1 / 1.1;
            scale.ScaleX *= move;
            scale.ScaleY *= move;

            if ((shape.Name ?? "") == "sn_marker")
            {
                tformer.scaleTransform.Set(scale.ScaleX, scale.ScaleY);
                select_shaper = -2;
                Update();
            }
            // Движения колеса осилил, значит и всё остальное осилю ;'-}
        }

        Shape? moved_shape;
        Point moved_pos;
        Point shape_old_pos;
        bool tapped = false;

        public void PressShape(Shape shape, Point pos)
        {
            Point? old_pos = null;
            foreach (var shaper in Shapers)
            {
                old_pos = shaper.GetPos(shape);
                if (old_pos != null) break;
            }
            if (old_pos == null) { Log.Write("Не удалось считать позицию фигуры :/"); return; }

            moved_shape = shape;
            moved_pos = pos;
            shape_old_pos = (Point)old_pos;
            tapped = true;
        }

        public void MoveShape(Shape shape, Point pos)
        {
            if (moved_shape != shape) return;
            var delta = pos - moved_pos;
            if (delta.X == 0 && delta.Y == 0) return; // Фиктивные перемещения. Могут возникнуть при отпускании фигуры сразу после нажатия из-за ReleaseShape метода в этом классе.

            if (Math.Pow(delta.X, 2) + Math.Pow(delta.Y, 2) > 9) tapped = false;
            var new_pos = shape_old_pos + delta;

            bool yeah = false;
            foreach (var shaper in Shapers)
            {
                yeah = shaper.SetPos(shape, (int)new_pos.X, (int)new_pos.Y);
                if (yeah) break;
            }
            if (!yeah) { Log.Write("Не удалось переместить фигуру :/"); return; }
            // else Log.Write("Перемещено!");

            if (shape.Name == "sn_marker")
            {
                update_marker_lock = update_name_lock = true;
                yeah = false;
                foreach (var shaper in Shapers)
                {
                    yeah = shaper.Load(this, shape);
                    if (yeah) break;
                }
                if (yeah)
                {
                    select_shaper = -2;
                    Update();
                }
                else Log.Write("Не удалось распотрошить фигуру :/");
                update_marker_lock = update_name_lock = false;
            }
        }

        public ShapeListBoxItem? ReleaseShape(Shape shape, Point pos)
        {
            if (moved_shape != shape) return null;
            MoveShape(shape, pos);
            moved_shape = null;

            if (tapped) return ShapeTap(shape.Name ?? "");
            return null;
        }

        public void DragOver(object? sender, DragEventArgs e)
        {
            // Log.Write("DragOver " + e.DragEffects);
            // Only allow Copy or Link as Drop Operations.
            e.DragEffects &= DragDropEffects.Copy | DragDropEffects.Link;

            // Only allow if the dragged data contains text or filenames.
            if (!e.Data.Contains(DataFormats.Text) && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None;
        }

        private Shape[]? GrandImport(string data)
        {
            object? content = null;

            try { content = Utils.Json2obj(data); } catch { }
            if (content != null) return Import(false, content);

            try { content = Utils.Xml2obj(data); } catch { }
            if (content != null) return Import(true, content);

            Log.Write("Не получилось разпознать тип данных :/ Нужен JSON, либо XML");
            return null;
        }

        public Shape[]? Drop(object? sender, DragEventArgs e)
        {
            // Log.Write("Drop");
            if (e.Data.Contains(DataFormats.Text))
            {
                var data = e.Data.GetText();
                if (data != null) return GrandImport(data);
            }

            if (e.Data.Contains(DataFormats.FileNames))
            {
                var list = e.Data.GetFileNames();
                if (list == null) return null;

                var files = list.ToArray();
                if (files.Length == 0) return null;

                return GrandImport(File.ReadAllText(files[0]));
            }

            return null;
        }
    }
}