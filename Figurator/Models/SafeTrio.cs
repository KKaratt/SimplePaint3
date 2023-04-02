using Avalonia;
using Avalonia.Media;
using System;
using System.Text;

namespace Figurator.Models {
    public class SafeTrio: ForcePropertyChange, ISafe {
        private bool valid = true;
        private readonly Action<object?>? hook;
        private readonly object? inst;
        private readonly string separator;
        public SafeTrio(int x, int y, int z, Action<object?>? hook = null, object? inst = null, bool altSeparator = false) {
            X = x; Y = y; Z = z; this.hook = hook; this.inst = inst;
            separator = altSeparator ? " " : ",";
        }
        public SafeTrio(string init, Action<object?>? hook = null, object? inst = null, bool altSeparator = false) {
            this.hook = hook; this.inst = inst;
            separator = altSeparator ? " " : ",";
            Set(init);
            if (!valid) throw new FormatException("Неверный формат инициализации SafeTrio: " + init);
        }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        private void Upd_valid(bool v) {
            valid = v;
            hook?.Invoke(inst);
        }
        private void Re_check() {
            if (!valid) {
                valid = true;
            }
        }
        public bool Valid => valid;

        public void Set(string str) {
            var ss = str.TrimAll().Split(separator);
            if (ss == null || ss.Length != 3) { Upd_valid(false); return; }

            int a, b, c;
            try {
                a = int.Parse(ss[0]);
                b = int.Parse(ss[1]);
                c = int.Parse(ss[2]);
            } catch { Upd_valid(false); return; }

            if (Math.Abs(a) > 10000 || Math.Abs(b) > 10000 || Math.Abs(c) > 10000) { Upd_valid(false); return; }

            X = a; Y = b; Z = c;
            Upd_valid(true);
        }

        public string Value {
            get { Re_check(); return X + separator + Y + separator + Z; }
            set {
                Set(value);
                UpdProperty(nameof(Color));
            }
        }

        public IBrush Color { get => valid ? Brushes.BlueViolet : Brushes.Red; }
    }
}
