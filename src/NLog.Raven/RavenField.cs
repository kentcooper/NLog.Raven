using System;
using NLog.Config;
using NLog.Layouts;

namespace NLog.Raven
{
    /// <summary>
    /// Defines single document field in RavenDb
    /// </summary>
    [NLogConfigurationItem]
    public class RavenField
    {
        /// <summary>
        /// Gets or sets the name for document-field
        /// </summary>
        [RequiredParameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the layout for rendering value for document-field
        /// </summary>
        [RequiredParameter]
        public Layout Layout { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenField"/> class.
        /// </summary>
        public RavenField()
            : this(null, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenField"/> class.
        /// </summary>
        public RavenField(string name, Layout layout)
        {
            Name = name;
            Layout = layout;
        }
    }
}
