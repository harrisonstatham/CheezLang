using Cheez.Compiler.Ast;
using Cheez.Compiler.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Cheez.Compiler.ParseTree
{
    public interface ILocation
    {
        TokenLocation Beginning { get; }
        TokenLocation End { get; }
    }

    public class Location : ILocation
    {
        public TokenLocation Beginning { get; }
        public TokenLocation End { get; }

        public IText Text => throw new System.NotImplementedException();

        public Location(TokenLocation beg)
        {
            this.Beginning = beg;
            this.End = beg;
        }

        public Location(TokenLocation beg, TokenLocation end)
        {
            this.Beginning = beg;
            this.End = end;
        }
        
        public Location(IEnumerable<ILocation> locations)
        {
            this.Beginning = locations.First().Beginning;
            this.End = locations.Last().End;
        }

        public Location(IEnumerable<IAstNode> locations)
        {
            this.Beginning = locations.First().Location.Beginning;
            this.End = locations.Last().Location.End;
        }
    }
}
