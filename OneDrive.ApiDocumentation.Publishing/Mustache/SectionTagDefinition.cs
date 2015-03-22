using Mustache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Publishing
{
    public class SectionTagDefinition : TagDefinition
    {
        public SectionTagDefinition()
            : base("ifmatch")
        {

        }

        public override void GetText(System.IO.TextWriter writer, Dictionary<string, object> arguments, Scope context)
        {
            base.GetText(writer, arguments, context);
        }

        protected override IEnumerable<TagParameter> GetParameters()
        {
            return new TagParameter[] { new TagParameter("currentValue") { IsRequired = true },
                                        new TagParameter("expectedValue") { IsRequired = true } };
        }


        public override bool ShouldGeneratePrimaryGroup(Dictionary<string, object> arguments)
        {
            var currentValue = arguments["currentValue"] as string;
            var expectedValue = arguments["expectedValue"] as string;

            return (currentValue == expectedValue);
        }

        protected override bool GetHasContent()
        {
            return true;
        }

        public override IEnumerable<TagParameter> GetChildContextParameters()
        {
            return new TagParameter[0];
        }
    }
}
