using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Figurator.Models;
using Figurator.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Text;
using System.Threading;

namespace Figurator.ViewModels {
    public class Log {
        static readonly List<string> logs = new();
        static readonly string path = "../../../Log.txt";
        static bool first = true;

        public static MainWindowViewModel? Mwvm { private get; set; }
        public static void Write(string message, bool without_update = false) {
            if (!without_update) {
                foreach (var mess in message.Split('\n')) logs.Add(mess);
                while (logs.Count > 50) logs.RemoveAt(0);

                if (Mwvm != null) Mwvm.Logg = string.Join('\n', logs);
            }

            if (first) File.WriteAllText(path, message + "\n");
            else File.AppendAllText(path, message + "\n");
            first = false;
        }
    }

    public class MainWindowViewModel : ViewModelBase {
        private int shaper_n = 0;
        private readonly Mapper map;
        private readonly Canvas canv;

        private readonly UserControl[] contentArray = new UserControl[] {
            new Shape1_UserControl(),
            new Shape2_UserControl(),
            new Shape3_UserControl(),
            new Shape4_UserControl(),
            new Shape5_UserControl(),
            new Shape6_UserControl()
        };
        private UserControl content;
        private UserControl? sharedContent = new ShapeT_UserControl();

        private string log = "";
        public string Logg { get => log; set => this.RaiseAndSetIfChanged(ref log, value); }

        private bool is_enabled = true;
        private IBrush add_color = Brushes.White;
        public IBrush AddColor { get => add_color; set => this.RaiseAndSetIfChanged(ref add_color, value); }

        private Shape? animated_part = null;

        private void Update()        {
            bool valid = map.ValidInput();
            bool valid2 = map.ValidName();

            is_enabled = valid;

            AddColor = valid ? valid2 ? Brushes.BlueViolet : Brushes.Yellow : Brushes.Red;
            ShapeNameColor = valid2 ? Brushes.BlueViolet : Brushes.Yellow;

            if (map.newName != null)
            {
                var name = map.newName;
                map.newName = null; 
                ShapeName = name;
            }

            // Log.Write("upd: " + map.update_marker_lock + "\n" + GetStackInfo());
            if (!map.update_marker_lock)
            { // Иначе не получится перетаскивать нормально маркер курсором
                if (animated_part != null)
                {
                    canv.Children.Remove(animated_part);
                    animated_part = null;
                }

                if (valid)
                {
                    Shape? newy = map.Create(true);
                    if (newy != null)
                    {
                        newy.Classes.Add("anim");
                        canv.Children.Add(newy);
                        animated_part = newy;
                    }
                }
            }

            int select = map.select_shaper;
            if (select != -1)
            {
                map.select_shaper = -1;
                if (select == -2) select = shaper_n; // Просто обновляет всё меню
                if (select == shaper_n) SelectedShaper = select == 0 ? 1 : 0; // Перебросочка
                SelectedShaper = select;
                SharedContent = null; // Перебросочка
                SharedContent = new ShapeT_UserControl();
            }
        }
        private static void Update(object? inst)
        {
            if (inst != null && inst is MainWindowViewModel @mwvm) @mwvm.Update();
        }

        public MainWindowViewModel(MainWindow mw)
        {
            Log.Mwvm = this;
            content = contentArray[0];
            map = new(Update, this);
            canv = mw.Find<Canvas>("canvas");
            Update();

            Add = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAdd(); return new Unit(); });
            Clear = ReactiveCommand.Create<Unit, Unit>(_ => { FuncClear(); return new Unit(); });
            Export = ReactiveCommand.Create<string, Unit>(n => { FuncExport(n); return new Unit(); });
            Import = ReactiveCommand.Create<string, Unit>(n => { FuncImport(n); return new Unit(); });

            /* canv.PointerEnter += (object? sender, PointerEventArgs e) => {
                Log.Write("PointerEnter: " + (e.Source == null ? "null" : e.Source.GetType().Name));
            };
            canv.PointerLeave += (object? sender, PointerEventArgs e) => {
                Log.Write("PointerLeave: " + (e.Source == null ? "null" : e.Source.GetType().Name));
            };*/
            canv.PointerPressed += (object? sender, PointerPressedEventArgs e) => {
                // Log.Write("PointerPressed: " + (e.Source == null ? "null" : e.Source.GetType().Name) + " pos: " + e.GetCurrentPoint(canv).Position);
                if (e.Source != null && e.Source is Shape @shape) map.PressShape(@shape, e.GetCurrentPoint(canv).Position);
            };
            canv.PointerMoved += (object? sender, PointerEventArgs e) => {
                // Log.Write("PointerMoved: " + (e.Source == null ? "null" : e.Source.GetType().Name) + " pos: " + e.GetCurrentPoint(canv).Position);
                if (e.Source != null && e.Source is Shape @shape) map.MoveShape(@shape, e.GetCurrentPoint(canv).Position);
            };
            canv.PointerReleased += (object? sender, PointerReleasedEventArgs e) => {
                // Log.Write("PointerReleased: " + (e.Source == null ? "null" : e.Source.GetType().Name) + " pos: " + e.GetCurrentPoint(canv).Position);
                if (e.Source != null && e.Source is Shape @shape)
                {
                    var item = map.ReleaseShape(@shape, e.GetCurrentPoint(canv).Position);
                    this.RaiseAndSetIfChanged(ref cur_shape, item, nameof(SelectedShape));
                }
            };
            canv.PointerWheelChanged += (object? sender, PointerWheelEventArgs e) => {
                // Log.Write("PointerWheelChanged: " + (e.Source == null ? "null" : e.Source.GetType().Name) + " delta: " + e.Delta);
                if (e.Source != null && e.Source is Shape @shape) map.WheelMove(@shape, e.Delta.Y);
            };

            var panel = canv.Parent;
            if (panel == null) return;
            panel.AddHandler(DragDrop.DropEvent, map.Drop);
            panel.AddHandler(DragDrop.DragOverEvent, map.DragOver);
            panel.AddHandler(DragDrop.DragEnterEvent, (object? sender, DragEventArgs e) => DropboxVisible = true);
            mw.AddHandler(DragDrop.DragLeaveEvent, (object? sender, DragEventArgs e) => DropboxVisible = false); // Не работает :/
            panel.AddHandler(DragDrop.DropEvent, (object? sender, DragEventArgs e) => {
                Shape[]? beginners = map.Drop(sender, e);
                foreach (var beginner in beginners) canv.Children.Add(beginner);
                Update();
                DropboxVisible = false; // Работает ;'-}
            });
        }
        bool dropbox_visible = false;
        public bool DropboxVisible { get => dropbox_visible; set => this.RaiseAndSetIfChanged(ref dropbox_visible, value); }

        public int SelectedShaper
        {
            get => shaper_n;
            set { this.RaiseAndSetIfChanged(ref shaper_n, value); map.ChangeFigure(value); Content = contentArray[value]; }
        }

        public UserControl Content
        {
            get => content;
            set => this.RaiseAndSetIfChanged(ref content, value);
        }
        public UserControl? SharedContent
        {
            get => sharedContent;
            set => this.RaiseAndSetIfChanged(ref sharedContent, value);
        }

        /*
         * КнопочК:
         */

        private void FuncAdd()
        {
            if (!is_enabled) return;
            // Log.Write("Add");

            Shape? newy = map.Create(false);
            if (newy == null) return;

            canv.Children.Add(newy);
            Update();
        }
        private void FuncClear() => map.Clear();
        private void FuncExport(string Type)
        {
            if (Type == "PNG")
            {
                ServiceVisible = false;
                if (animated_part != null) animated_part.IsVisible = false;

                try
                {
                    Utils.RenderToFile(canv, "../../../Export.png");
                }
                catch (Exception e)
                {
                    Log.Write("Ошибка экспорта PNG: " + e);
                }

                ServiceVisible = true;
                if (animated_part != null) animated_part.IsVisible = true;
            }
            else map.Export(Type == "XML");
        }
        private void FuncImport(string Type)
        {
            if (Type == "PNG")
            {
                var stackSize = 10000000; // ДЕЙСТВИТЕЛЬНО ПРОКАНАЛО!!! XD 8000 вообще ни о чём, когда надо рекурсивно найти все группы пикселей, что может, в худшем случае, потребовать стек подобного размера...
                new Thread(Imager.Import, stackSize).Start();
                return;
            }
            Shape[]? beginners = map.Import(Type == "XML");
            if (beginners == null) return;

            foreach (var beginner in beginners) canv.Children.Add(beginner);
            Update();
        }

        public ReactiveCommand<Unit, Unit> Add { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }
        public ReactiveCommand<string, Unit> Export { get; }
        public ReactiveCommand<string, Unit> Import { get; }

        /*
         * Просто параметры фигур:
         */

        private IBrush nameColor = Brushes.White;
        public string ShapeName { get => map.shapeName; set { this.RaiseAndSetIfChanged(ref map.shapeName, value); Update(); } }
        public IBrush ShapeNameColor { get => nameColor; set => this.RaiseAndSetIfChanged(ref nameColor, value); }

        public string ShapeColor { get => map.shapeColor; set { this.RaiseAndSetIfChanged(ref map.shapeColor, value); Update(); } }
        public string ShapeFillColor { get => map.shapeFillColor; set { this.RaiseAndSetIfChanged(ref map.shapeFillColor, value); Update(); } }
        public int ShapeThickness { get => map.shapeThickness; set { this.RaiseAndSetIfChanged(ref map.shapeThickness, value); Update(); } }

        public SafeNum ShapeWidth => map.shapeWidth;
        public SafeNum ShapeHeight => map.shapeHeight;
        public SafeNum ShapeHorizDiagonal => map.shapeHorizDiagonal;
        public SafeNum ShapeVertDiagonal => map.shapeVertDiagonal;

        public SafePoint ShapeStartDot => map.shapeStartDot;
        public SafePoint ShapeEndDot => map.shapeEndDot;
        public SafePoint ShapeCenterDot => map.shapeCenterDot;
        public SafePoints ShapeDots => map.shapeDots;

        public SafeGeometry ShapeCommands => map.shapeCommands;

        // Аддон трансформаций:

        public SafeNum RenderTransformAngle => map.tformer.rotateTransformAngle;
        public SafePoint RenderTransformCenter => map.tformer.rotateTransformCenter;
        public SafeDPoint ScaleTransform => map.tformer.scaleTransform;
        public SafePoint SkewTransform => map.tformer.skewTransform;
        public ObservableCollection<string> MatrixTransform => map.tformer.matrix;

        /*
         * База цветов
         */

        public static string[] ColorsArr { get => Imager.colors; }

        public ObservableCollection<ShapeListBoxItem> Shapes { get => map.shapes; }

        private bool service_visible = true;
        public bool ServiceVisible { get => service_visible; set => this.RaiseAndSetIfChanged(ref service_visible, value); }

        private ShapeListBoxItem? cur_shape;
        public ShapeListBoxItem? SelectedShape
        {
            get => cur_shape;
            set { this.RaiseAndSetIfChanged(ref cur_shape, value); map.Select(value); }
        }
    }
}