using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog.Config;
using NLog.Layouts;

namespace NLog.Raven
{
    [NLogConfigurationItem]
    public class RavenField
    {
        [RequiredParameter]
        public string Name { get; set; }
        [RequiredParameter]
        public Layout Layout { get; set; }

        public RavenField()
            : this(null, null)
        { }

        public RavenField(string name, Layout layout)
        {
            Name = name;
            Layout = layout;
        }
    }
}
