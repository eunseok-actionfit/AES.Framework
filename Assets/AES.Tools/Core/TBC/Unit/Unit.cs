using System;


namespace AES.Tools.TBC.Unit
{
    public readonly struct Unit : IEquatable<Unit>
    {
        public readonly static Unit Default = new();

        public bool Equals(Unit other) => true;
        public override bool Equals(object obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public override string ToString() => "()";
    }
}