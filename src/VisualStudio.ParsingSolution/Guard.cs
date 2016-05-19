using System;

namespace VsxFactory.Modeling
{
    // Summary:
    //     Common guard clauses
    public static class Guard
    {
        // Summary:
        //     Checks an argument to ensure it isn't null
        //
        // Parameters:
        //   argumentValue:
        //     The argument value to check.
        //
        //   argumentName:
        //     The name of the argument.
        public static void ArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
                throw new ArgumentNullException(argumentName);
        }

        //
        // Summary:
        //     Checks a string argument to ensure it isn't null or empty
        //
        // Parameters:
        //   argumentValue:
        //     The argument value to check.
        //
        //   argumentName:
        //     The name of the argument.
        public static void ArgumentNotNullOrEmptyString(string argumentValue, string argumentName)
        {
            if (String.IsNullOrEmpty(argumentValue))
                throw new ArgumentNullException(argumentName);
        }

        public static void AssumeNotNull(object variable , string variableName)
        {
            if( variable == null)
                throw new Exception(String.Format("Var {0} must be not null", variableName));
        }
    }
}
