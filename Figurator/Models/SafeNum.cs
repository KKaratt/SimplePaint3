using Avalonia;
using Avalonia.Media;
using System;

namespace Figurator.Models {
    public class SafeNum: ForcePropertyChange, ISafe {
        private int num;
        private bool valid = true;
        private readonly Action<object?>? hook;
        private readonly object? inst;
        public SafeNum(int num, Action<object?>? hook = null, object? inst = null) {
            this.num = num; this.hook = hook; this.inst = inst;
        }
        public SafeNum(string init, Action<object?>? hook = null, object? inst = null) {
            this.hook = hook; this.inst = inst;
            Set(init);
            if (!valid) throw new FormatException("Неверный формат инициализации SafeNum: " + init);
        }
        public int Num => num;

        private void Upd_valid(bool v) {
            valid = v;
            hook?.Invoke(inst);
        }
        private void Re_check() {
            if (!valid) {
                valid = true;
            }
        }
        public void Set(int n) {
            num = n;
            valid = true;
        }

        public bool Valid => valid;

        public void Set(string str) {
            int a;
            try {
                a = int.Parse(str);
            } catch { Upd_valid(false); return; }

            if (Math.Abs(a) > 10000) { Upd_valid(false); return; }

            num = a;
            Upd_valid(true);
        }

        public string Value {
            get { 
                Re_check(); return num.ToString(); }
            set {
                Set(value);
                UpdProperty(nameof(Color));
            }
        }

        public IBrush Color { get => valid ? Brushes.BlueViolet : Brushes.Red; }
    }
}
