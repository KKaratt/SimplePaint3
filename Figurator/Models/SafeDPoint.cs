using Avalonia;
using Avalonia.Media;
using System;
using System.Text;

namespace Figurator.Models {
    public class SafeDPoint: ForcePropertyChange, ISafe {
        private double X, Y;
        private bool valid = true;
        private readonly Action<object?>? hook;
        private readonly object? inst;
        private readonly string separator;
        public SafeDPoint(double x, double y, Action<object?>? hook = null, object? inst = null, bool altSeparator = false) {
            X = x; Y = y; this.hook = hook; this.inst = inst;
            separator = altSeparator ? " " : ",";
        }
        public SafeDPoint(string init, Action<object?>? hook = null, object? inst = null, bool altSeparator = false) {
            this.hook = hook; this.inst = inst;
            separator = altSeparator ? " " : ",";
            Set(init);
            if (!valid) throw new FormatException("Неверный формат инициализации SafeDPoint: " + init);
        }
        public Point Point { get => new(X, Y); }

        private void Upd_valid(bool v) {
            valid = v;
            hook?.Invoke(inst);
        }
        private void Re_check() {
            if (!valid) {
                valid = true;
            }
        }
        public void Set(Point p) {
            X = p.X; Y = p.Y;
            valid = true;
        }
        public void Set(double x, double y) {
            X = x; Y = y;
            valid = true;
        }

        public bool Valid => valid;

        public void Set(string str) {
            var ss = str.TrimAll().Replace('.', ',').Split(separator);
            if (ss == null || ss.Length != 2) { Upd_valid(false); return; }

            double a, b;
            try {
                a = double.Parse(ss[0]);
                b = double.Parse(ss[1]);
            } catch { Upd_valid(false); return; }

            if (Math.Abs(a) > 10000 || Math.Abs(b) > 10000) { Upd_valid(false); return; }

            X = a; Y = b;
            Upd_valid(true);
        }
        public string Value {
            get { Re_check(); return X + separator + Y; }
            set {
                Set(value);
                UpdProperty(nameof(Color));
            }
        }

        public IBrush Color { get => valid ? Brushes.BlueViolet : Brushes.Red; }
    }
}
