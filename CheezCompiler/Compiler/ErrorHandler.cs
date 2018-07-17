using Cheez.Compiler.ParseTree;
using Cheez.Compiler.Parsing;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cheez.Compiler
{
    public class Error
    {
        public IText Text { get; set; }
        public ILocation Location { get; set; }
        public string Message { get; set; }
        public List<Error> SubErrors { get; set; }
        public List<string> Details { get; set; }
    }

    public interface IErrorHandler
    {
        void ReportError(string message, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0);
        void ReportError(IText text, ILocation location, string message, List<Error> subErrors = null, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0);
        void ReportError(Error error, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0);

        bool HasErrors { get; set; }
    }
    
    //public class SilentError
    //{
    //    public Error Error { get; set; }
    //    public string CallerFile { get; set; }
    //    public string CallerFunction { get; set; }
    //    public int CallerLineNumber { get; set; }
    //}

    public class SilentErrorHandler : IErrorHandler
    {
        public bool HasErrors { get; set; }

        public List<Error> Errors { get; } = new List<Error>();

        public void ReportError(string message, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0)
        {
            HasErrors = true;
            Errors.Add(new Error
            {
                Message = message,
            });
        }

        public void ReportError(IText text, ILocation location, string message, List<Error> subErrors, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0)
        {
            ReportError(new Error
            {
                Text = text,
                Location = location,
                Message = message,
                SubErrors = subErrors
            }, callingFunctionFile, callingFunctionName, callLineNumber);
        }

        public void ReportError(Error error, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0)
        {
            HasErrors = true;
            Errors.Add(error);
        }

        public void ClearErrors()
        {
            HasErrors = false;
            Errors.Clear();
        }

        public void ForwardErrors(IErrorHandler next)
        {
            foreach (var e in Errors)
            {
                next.ReportError(e);
            }
        }
    }
}
