using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pretender
{
    public struct Argument
    {
        private readonly Type _declaredType;
        private object? _value;

        public Argument(Type declaredType, object? value)
        {
            _declaredType = declaredType;
            _value = value;
        }

        public readonly Type DeclaredType => _declaredType;
        public object? Value
        {
            readonly get { return _value; }
            set {  _value = value; }
        }
        public readonly Type ActualType => _value != null ? _value.GetType() : _declaredType;
    }
}
