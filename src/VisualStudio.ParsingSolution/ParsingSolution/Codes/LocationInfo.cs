using EnvDTE;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// Location of the code
    /// </summary>
    public class LocationInfo
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationInfo"/> class.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        internal LocationInfo(TextPoint startPoint, TextPoint endPoint)
        {
            this.Start = new LocationPoint(startPoint);
            this.End = new LocationPoint(endPoint);
        }

        /// <summary>
        /// Gets the end point location of the code.
        /// </summary>
        /// <value>
        /// The end.
        /// </value>
        public LocationPoint End { get; private set; }

        /// <summary>
        /// Gets the start point location of the code.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        public LocationPoint Start { get; private set; }

    }

}