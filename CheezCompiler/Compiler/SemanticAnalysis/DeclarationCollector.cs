using Cheez.Compiler.Ast;
using Cheez.Compiler.ParseTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cheez.Compiler.SemanticAnalysis
{
    public class DeclarationCollector
    {
        private IErrorHandler mErrorHandler;
        private Workspace mWorkspace;

        private List<AstStructDecl> mPolyStructs = new List<AstStructDecl>();
        private List<AstStructDecl> mStructs = new List<AstStructDecl>();
        private List<AstEnumDecl> mEnums = new List<AstEnumDecl>();
        private List<AstVariableDecl> mVariables = new List<AstVariableDecl>();
        private List<AstFunctionDecl> mFunctions = new List<AstFunctionDecl>();
        private List<AstImplBlock> mImpls = new List<AstImplBlock>();

        private void ReportError(ILocation lc, string message)
        {
            var (callingFunctionFile, callingFunctionName, callLineNumber) = Util.GetCallingFunction().GetValueOrDefault(("", "", -1));
            var file = mWorkspace.GetFile(lc.Beginning.file);
            mErrorHandler.ReportError(file, lc, message, null, callingFunctionFile, callingFunctionName, callLineNumber);
        }

        [SkipInStackFrame]
        private void ReportError(ILocation lc, string message, params string[] details)
        {
            var (callingFunctionFile, callingFunctionName, callLineNumber) = Util.GetCallingFunction().GetValueOrDefault(("", "", -1));
            var file = mWorkspace.GetFile(lc.Beginning.file);
            mErrorHandler.ReportError(new Error
            {
                Text = file,
                Location = lc,
                Message = message,
                Details = details.ToList()
            }, callingFunctionFile, callingFunctionName, callLineNumber);
        }

        public void CollectDeclarations(Workspace ws, IErrorHandler errorHandler)
        {
            mWorkspace = ws;
            mErrorHandler = errorHandler;

            var globalScope = ws.GlobalScope;

            // pass 1:
            // collect types
            foreach (var s in ws.Statements)
            {
                switch (s)
                {
                    case AstStructDecl @struct:
                        {
                            @struct.Scope = globalScope;
                            Pass1StructDeclaration(@struct);
                            mStructs.Add(@struct);
                            break;
                        }

                    case AstEnumDecl @enum:
                        {
                            @enum.Scope = globalScope;
                            Pass1EnumDeclaration(@enum);
                            mEnums.Add(@enum);
                            break;
                        }

                    case AstVariableDecl @var:
                        {
                            mVariables.Add(@var);
                            break;
                        }

                    case AstFunctionDecl func:
                        {
                            mFunctions.Add(func);
                            break;
                        }

                    case AstImplBlock impl:
                        {
                            mImpls.Add(impl);
                            break;
                        }
                }
            }

            // pass 2:
            // resolve struct member types
            foreach (var @struct in mStructs)
            {
                Pass2StructDeclaration(@struct);
            }
        }

        private void Pass1EnumDeclaration(AstEnumDecl @enum)
        {
            // @todo: check if members hava unique name, calculate indices

            int currentValue = 0;

            var nameSet = new HashSet<string>();
            foreach (var member in @enum.Members)
            {
                if (nameSet.Contains(member.Name))
                    ReportError(member.ParseTreeNode, $"An enum member with name '{member.Name}' already exists");
                nameSet.Add(member.Name);

                member.Value = currentValue;

                currentValue++;
            }

            @enum.Scope.TypeDeclarations.Add(@enum);
            var type = new EnumType(@enum);

            if (!@enum.Scope.DefineTypeSymbol(@enum.Name.Name, type))
            {
                ReportError(@enum.Name.GenericParseTreeNode, $"A symbol with name '{@enum.Name.Name}' already exists in current scope");
            }
        }

        private void Pass1StructDeclaration(AstStructDecl @struct)
        {
            if (@struct.Parameters.Count > 0)
            {
                @struct.IsPolymorphic = true;
                mPolyStructs.Add(@struct);

                var nameSet = new HashSet<string>();

                // check parameter types
                foreach (var p in @struct.Parameters)
                {
                    if (nameSet.Contains(p.Name.Name))
                        ReportError(p.Name.GenericParseTreeNode, "Duplicate parameter names are not allowed");
                    nameSet.Add(p.Name.Name);

                    switch (p.TypeExpr)
                    {
                        case AstIdentifierExpr i:
                            {
                                var sym = @struct.Scope.GetSymbol(i.Name, false);
                                if (sym is CompTimeVariable c && c.Type == CheezType.Type)
                                {
                                    switch (c.Value)
                                    {
                                        case CheezTypeType _:
                                        case IntType _:
                                        case FloatType _:
                                        case BoolType _:
                                        case CharType _:
                                        case StringType _:
                                            p.Type = c.Value as CheezType;
                                            continue;
                                    }
                                }
                                break;
                            }
                    }

                    ReportError(p.ParseTreeNode, $"Type '{p.TypeExpr}' is not allowed here", "The following types are allowed: type, bool, f32, f64, string, char and all integer types");
                }
            }

            @struct.Scope.TypeDeclarations.Add(@struct);
            var structType = new StructType(@struct);

            if (!@struct.Scope.DefineTypeSymbol(@struct.Name.Name, structType))
            {
                ReportError(@struct.Name.GenericParseTreeNode, $"A symbol with name '{@struct.Name.Name}' already exists in current scope");
            }
        }

        //////////////////////////////////////////////////////////////////
        #region Pass 2

        private void Pass2StructDeclaration(AstStructDecl @struct)
        {
            @struct.SubScope = new Scope("struct", @struct.Scope);

            var nameSet = new HashSet<string>();
            foreach (var m in @struct.Members)
            {
                if (nameSet.Contains(m.Name.Name))
                    ReportError(m.Name.GenericParseTreeNode, $"A member with name '{m.Name.Name}' already exists in struct '{@struct.Name}'");
                nameSet.Add(m.Name.Name);

                m.TypeExpr.Scope = @struct.SubScope;
                m.Type = ResolveType(m.TypeExpr);
            }
        }

        private CheezType ResolveType(AstExpression typeExpr)
        {
            switch (typeExpr)
            {
                case AstIdentifierExpr i:
                    {
                        var sym = typeExpr.Scope.GetSymbol(i.Name, false);
                        if (sym is CompTimeVariable c && c.Type == CheezType.Type)
                            return c.Value as CheezType;
                        break;
                    }

                case AstPointerTypeExpr p:
                    {
                        p.Target.Scope = typeExpr.Scope;
                        var subType = ResolveType(p.Target);
                        return PointerType.GetPointerType(subType);
                    }

                case AstArrayTypeExpr a:
                    {
                        a.Target.Scope = typeExpr.Scope;
                        var subType = ResolveType(a.Target);
                        return SliceType.GetSliceType(subType);
                    }

                case AstArrayAccessExpr arr:
                    {
                        arr.SubExpression.Scope = typeExpr.Scope;
                        arr.Indexer.Scope = typeExpr.Scope;
                        var subType = ResolveType(arr.SubExpression);
                        var indexer = ResolveExpression(arr.Indexer);
                        if (indexer.IsCompTimeValue && indexer.Value is long length)
                        {
                            return ArrayType.GetArrayType(subType, (int)length);
                        }
                        else
                        {
                            ReportError(arr.Indexer.GenericParseTreeNode, "Index must be a constant int");
                            return CheezType.Error;
                        }
                    }
            }
            return CheezType.Error;
        }

        private AstExpression ResolveExpression(AstExpression indexer)
        {
            switch (indexer)
            {
                case AstNumberExpr n:
                    {
                        if (n.Data.Type == Parsing.NumberData.NumberType.Float)
                        {
                            n.Type = FloatType.LiteralType;
                            n.Value = n.Data.ToDouble();
                        }
                        else
                        {
                            n.Type = IntType.LiteralType;
                            n.Value = n.Data.ToLong();
                        }

                        n.IsCompTimeValue = true;
                        break;
                    }
            }
            return indexer;
        }

        #endregion
    }
}
