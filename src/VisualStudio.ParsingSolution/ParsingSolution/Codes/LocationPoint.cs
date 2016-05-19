using System;
using EnvDTE;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// location point of code
    /// </summary>
    public class LocationPoint
    {

        private TextPoint point;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationPoint"/> class.
        /// </summary>
        /// <param name="point">The point.</param>
        public LocationPoint(TextPoint point)
        {
            this.point = point;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid { get { return this.point != null; } }

        /// <summary>
        /// Gets the absolute character offset.
        /// </summary>
        /// <value>
        /// The absolute character offset.
        /// </value>
        public int AbsoluteCharOffset { get { return this.point.AbsoluteCharOffset; } }

        /// <summary>
        /// Gets a value indicating whether [at end of document].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [at end of document]; otherwise, <c>false</c>.
        /// </value>
        public bool AtEndOfDocument { get { return this.point.AtEndOfDocument; } }

        /// <summary>
        /// Gets a value indicating whether [at end of line].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [at end of line]; otherwise, <c>false</c>.
        /// </value>
        public bool AtEndOfLine { get { return this.point.AtEndOfLine; } }

        /// <summary>
        /// Gets a value indicating whether [at start of document].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [at start of document]; otherwise, <c>false</c>.
        /// </value>
        public bool AtStartOfDocument { get { return this.point.AtStartOfDocument; } }

        /// <summary>
        /// Gets a value indicating whether [at start of line].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [at start of line]; otherwise, <c>false</c>.
        /// </value>
        public bool AtStartOfLine { get { return this.point.AtStartOfLine; } }

        /// <summary>
        /// Gets the line.
        /// </summary>
        /// <value>
        /// The line.
        /// </value>
        public int Line { get { return this.point.Line; } }

        /// <summary>
        /// Gets the line character offset.
        /// </summary>
        /// <value>
        /// The line character offset.
        /// </value>
        public int LineCharOffset { get { return this.point.LineCharOffset; } }

        /// <summary>
        /// Gets the length of the line.
        /// </summary>
        /// <value>
        /// The length of the line.
        /// </value>
        public int LineLength { get { return this.point.LineLength; } }

        public int ResolveRealOffset(string code, bool isStart)
        {

            int compense = isStart ? 1 : 0;
            int index = -1;
            int _line = 0;
            var e = code.Length;
            for (index = 0; index < e; index++)
            {
                char c = code[index];
                if (c == '\r')
                {
                    _line++;
                    if (_line == Line - compense)
                    {
                        index++;
                        break;
                    }
                }
            }

            return index;

        }


    }

}