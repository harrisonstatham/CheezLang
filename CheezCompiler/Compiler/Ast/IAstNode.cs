using Cheez.Compiler.ParseTree;

namespace Cheez.Compiler.Ast
{
    public interface IAstNode
    {
        ILocation Location { get; }
    }
}
