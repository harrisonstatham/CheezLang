using System.Collections.Generic;

namespace Cheez.Compiler.Ast
{
    public class AstDirective : IAstNode
    {
        public ParseTree.PTDirective ParseTreeNode { get; }
        public ParseTree.ILocation Location => ParseTreeNode;

        public string Name { get; }

        public List<AstExpression> Arguments { get; set; }

        public AstDirective(ParseTree.PTDirective node, string name, List<AstExpression> args)
        {
            ParseTreeNode = node;
            this.Name = name;
            this.Arguments = args;
        }
    }
}
