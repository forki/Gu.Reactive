// ReSharper disable All
namespace Gu.Reactive.Tests.Collections
{
    public partial class MappingViewTests
    {
        public class Model
        {
            public Model(int value)
            {
                this.Value = value;
            }

            public int Value { get; }

            public override string ToString()
            {
                return $"{nameof(this.Value)}: {this.Value}";
            }
        }

        private class Vm
        {
            public Vm()
            {
            }

            public Vm(Model model,  int index)
            {
                this.Model = model;
                this.Index = index;
            }

            public Model Model { get; set; }

            public int Value { get; set; }

            public int Index { get; set; }

            public Vm WithIndex(int i)
            {
                this.Index = i;
                return this;
            }

            public override string ToString()
            {
                return $"{nameof(this.Value)}: {this.Value}, {nameof(this.Index)}: {this.Index}, {nameof(this.Model)}: {this.Model}";
            }
        }
    }
}