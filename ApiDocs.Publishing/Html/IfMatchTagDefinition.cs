namespace ApiDocs.Publishing.Html
{
    using System.Collections.Generic;
    using System.Linq;
    using Mustache;

    public class IfMatchTagDefinition : TagDefinition
    {
        public IfMatchTagDefinition()
            : base("ifmatch")
        {

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

        protected override IEnumerable<string> GetChildTags()
        {
            return new string[] { "else", "elif" };
        }

        public override bool ShouldCreateSecondaryGroup(TagDefinition definition)
        {
            return this.GetChildTags().Contains(definition.Name);
        }

        public override IEnumerable<TagParameter> GetChildContextParameters()
        {
            return new TagParameter[0];
        }
    }

    public class ExtendedElseTagDefinition : ElseTagDefinition
    {
        protected override IEnumerable<string> GetClosingTags()
        {
            var tags = new List<string>(base.GetClosingTags()) { "ifmatch" };
            return tags;
        }
    }

    public class ExtendedElseIfTagDefinition : ElifTagDefinition
    {
        protected override IEnumerable<string> GetClosingTags()
        {
            var tags = new List<string>(base.GetClosingTags()) { "ifmatch" };
            return tags;
        }
    }

   
}
