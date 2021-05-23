using System;

namespace CodeChefRanklistTool
{
    public class XsrfStatementParserException : Exception
    {
        public XsrfStatementParserException() : base("Unexpected xsrf statement format")
        {
        }

        public XsrfStatementParserException(string message) : base(message)
        {
        }
    }

    public class HttpErrorException : Exception
    {
        public HttpErrorException() : base("Shit, they're onto us!")
        {
        }

        public HttpErrorException(string message) : base(message)
        {
        }
    }

    public class CaptchaEncounteredException : Exception
    {
        public CaptchaEncounteredException()
        {
        }

        public CaptchaEncounteredException(string message) : base(message)
        {
        }
    }
}