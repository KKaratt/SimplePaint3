using Avalonia;
using Avalonia.Media;
using System;
using System.Linq;

namespace Figurator.Models {
    public class SafePoints: ForcePropertyChange, ISafe {
        private Points points = new();
        private bool valid = true;
        private readonly Action<object?>? hook;
        private readonly object? inst;
        public SafePoints(string init, Action<object?>? hook = null, object? inst = null) {
            this.hook = hook; this.inst = inst;
            Set(init);
            if (!valid) throw new FormatException("Неверный формат инициализации SafePoints: " + init);
        }
        public Points Points => points;

        private void Upd_valid(bool v) {
            valid = v;
            hook?.Invoke(inst);
        }
        private void Re_check() {
            if (!valid) {
                valid = true;
            }
        }
        public void Set(Points ps) {
            points = ps;
            valid = true;
        }
        public bool Valid => valid;

        public void Set(string str) {
            Points list = new();
            foreach (var p in str.Split()) {
                if (p.Length == 0) continue;

                var ss = p.Split(",");
                if (ss == null || ss.Length != 2) { Upd_valid(false); return; }

                int a, b;
                try {
                    a = int.Parse(ss[0]);
                    b = int.Parse(ss[1]);
                } catch { Upd_valid(false); return; }

                if (Math.Abs(a) > 10000 || Math.Abs(b) > 10000) { Upd_valid(false); return; }
                list.Add(new Point(a, b));
            }
            points = list;
            Upd_valid(true);
        }

        public string Value {
            get { Re_check(); return String.Join(" ", points.Select(p => p.X + "," + p.Y)); }
            set {
                Set(value);
                UpdProperty(nameof(Color));
            }
        }

        public IBrush Color { get => valid ? Brushes.BlueViolet : Brushes.Red; }
    }
}
