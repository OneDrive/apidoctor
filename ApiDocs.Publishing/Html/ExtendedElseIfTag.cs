//namespace ApiDocs.Publishing.Html
//{
//    using Mustache;
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Text;
//    using System.Threading.Tasks;

//    internal class ExtendedElseIfTagDefinition : ExtendedConditionalTag
//    {
//         /// <summary>
//        /// Initializes a new instance of an ElifTagDefinition.
//        /// </summary>
//        public ExtendedElseIfTagDefinition()
//            : base("elif")
//        {
//        }

//        /// <summary>
//        /// Gets whether the tag only exists within the scope of its parent.
//        /// </summary>
//        protected override bool GetIsContextSensitive()
//        {
//            return true;
//        }
        
//        /// <summary>
//        /// Gets the tags that indicate the end of the current tags context.
//        /// </summary>
//        protected override IEnumerable<string> GetClosingTags()
//        {
//            var tags = new List<string>(base.GetClosingTags()) { "ifmatch" };
//            return tags;
//        }
//    }
//}
