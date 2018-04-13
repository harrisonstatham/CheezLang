﻿using Cheez.Parsing;
using Cheez.Visitor;
using System.Diagnostics;

namespace Cheez.Ast
{
    public abstract class Statement : ILocation
    {
        public LocationInfo Beginning { get; set; }
        public LocationInfo End { get; set; }
        public int Id { get; }

        public Statement(LocationInfo beg, LocationInfo end)
        {
            this.Beginning = beg;
            this.End = end;
            this.Id = Util.NewId;
        }

        [DebuggerStepThrough]
        public abstract T Accept<T, D>(IVisitor<T, D> visitor, D data = default(D));

        [DebuggerStepThrough]
        public abstract void Accept<D>(IVoidVisitor<D> visitor, D data = default(D));

        public override bool Equals(object obj)
        {
            return obj == this;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}