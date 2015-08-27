namespace ApiDocs.Publishing.Html
{
    using Mustache;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class ExtendedElseTagDefinition : ContentTagDefinition
    {
         /// <summary>
        /// Initializes a new instance of a ElseTagDefinition.
        /// </summary>
        public ExtendedElseTagDefinition()
            : base("elsematch")
        {
        }

        /// <summary>
        /// Gets whether the tag only exists within the scope of its parent.
        /// </summary>
        protected override bool GetIsContextSensitive()
        {
            return true;
        }

        /// <summary>
        /// Gets the tags that indicate the end of the current tag's content.
        /// </summary>
        protected override IEnumerable<string> GetClosingTags()
        {
            var tags = new List<string>(base.GetClosingTags()) { "ifmatch" };
            return tags;
        }

        /// <summary>
        /// Gets the parameters that are used to create a new child context.
        /// </summary>
        /// <returns>The parameters that are used to create a new child context.</returns>
        public override IEnumerable<TagParameter> GetChildContextParameters()
        {
            return new TagParameter[0];
        }
    }

   
}
