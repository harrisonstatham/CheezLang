﻿using Cheez.Ast;
using Cheez.Parsing;
using System;
using System.Collections.Generic;

namespace Cheez
{
    public class CheezFile
    {
        public string Name { get; }
        public string RawText { get; set; }

        public Scope ExportScope { get; } = new Scope();
        private Scope mPrivateScope = new Scope();
        public ScopeRef PrivateScope { get; }

        public List<Statement> Statements { get; }

        public CheezFile(string name, string raw, List<Statement> statements)
        {
            this.Name = name;
            this.RawText = raw;
            PrivateScope = new ScopeRef(mPrivateScope, ExportScope);
            Statements = statements;
        }
    }

    public struct CompilationUnit
    {
        public CheezFile file;
        public Statement statement;
        public Workspace workspace;
    }

    public class Compiler
    {
        private Dictionary<string, CheezFile> mFiles = new Dictionary<string, CheezFile>();
        private Workspace mMainWorkspace;
        private Dictionary<string, Workspace> mWorkspaces = new Dictionary<string, Workspace>();

        public Workspace DefaultWorkspace => mMainWorkspace;

        public Compiler()
        {
            mMainWorkspace = new Workspace(this);
            mWorkspaces["main"] = mMainWorkspace;
        }

        public CheezFile AddFile(string fileName, Workspace workspace = null)
        {
            if (mFiles.ContainsKey(fileName))
                return mFiles[fileName];

            if (workspace == null)
                workspace = mMainWorkspace;

            // parse file
            List<Statement> statements = new List<Statement>();
            var lexer = Lexer.FromFile(fileName);
            var parser = new Parser(lexer);

            try
            {
                while (true)
                {

                    var s = parser.ParseStatement();
                    if (s == null)
                        break;
                    statements.Add(s);
                }
            }
            catch (Exception e)
            {
                throw;
            }

            var file = new CheezFile(fileName, lexer.Text, statements);
            mFiles[fileName] = file;
            workspace.AddFile(file);

            return file;
        }
    }
}