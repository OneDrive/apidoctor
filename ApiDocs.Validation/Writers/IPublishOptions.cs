using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.Writers
{
    public interface IPublishOptions
    {
        /// <summary>
        /// A list of relative paths for files in the 
        /// input directory that should be published.
        /// </summary>
        string[] FilesToPublish { set; get; }

        /// <summary>
        /// Relative path to the folder that contains template
        /// files for the publishing engine.
        /// </summary>
        string TemplatePath { get; set; }
    }

    public class DefaultPublishOptions : IPublishOptions
    {
        public string[] FilesToPublish { get; set; }

        public string TemplatePath { get; set; }
    }
}
