﻿using Cheez.Compiler.Ast;
using Cheez.Compiler.ParseTree;
using System.Collections.Generic;

namespace Cheez.Compiler
{
    public class Scope
    {
        public string Name { get; set; }

        public Scope Parent { get; }

        public List<AstFunctionDecl> FunctionDeclarations { get; } = new List<AstFunctionDecl>();
        public List<AstVariableDecl> VariableDeclarations { get; } = new List<AstVariableDecl>();
        public List<AstTypeDecl> TypeDeclarations { get; } = new List<AstTypeDecl>();

        private CTypeFactory types = new CTypeFactory();

        private Dictionary<string, AstFunctionDecl> mFunctionTable = new Dictionary<string, AstFunctionDecl>();
        private Dictionary<string, IVariableDecl> mVariableTable = new Dictionary<string, IVariableDecl>();

        public Scope(string name, Scope parent = null)
        {
            this.Name = name;
            this.Parent = parent;
        }

        public CheezType GetCheezType(PTTypeExpr expr)
        {
            return types.GetCheezType(expr) ?? Parent?.GetCheezType(expr);
        }

        public CheezType GetCheezType(string name)
        {
            return types.GetCheezType(name) ?? Parent?.GetCheezType(name);
        }

        public AstFunctionDecl GetFunction(string name, List<CheezType> parameters = null)
        {
            if (mFunctionTable.ContainsKey(name))
                return mFunctionTable[name];

            return Parent?.GetFunction(name);
        }

        public bool DefineVariable(IVariableDecl variable)
        {
            if (mVariableTable.ContainsKey(variable.Name))
                return false;

            mVariableTable[variable.Name] = variable;

            return true;
        }

        public IVariableDecl GetVariable(string name)
        {
            if (mVariableTable.ContainsKey(name))
                return mVariableTable[name];

            return Parent?.GetVariable(name);
        }

        public bool DefineType(AstTypeDecl t)
        {
            if (types.GetCheezType(t.Name) != null)
                return false;

            types.CreateAlias(t.Name, new StructType(t));
            return true;
        }

        public bool DefineType(string name, CheezType type)
        {
            if (types.GetCheezType(name) != null)
                return false;

            types.CreateAlias(name, type);
            return true;
        }

        public bool DefineFunction(AstFunctionDecl f)
        {
            if (mFunctionTable.ContainsKey(f.Name))
            {
                return false;
            }

            mFunctionTable[f.Name] = f;

            return true;
        }

        public override string ToString()
        {
            if (Parent != null)
                return $"{Parent}::{Name}";
            return $"::{Name}";
        }
    }
}
