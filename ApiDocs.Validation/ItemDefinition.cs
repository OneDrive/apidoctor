namespace ApiDocs.Validation
{
    using System.Collections.Generic;

    public class ItemDefinition
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public List<ParameterDefinition> Parameters { get; set; }

        public ItemDefinition()
        {
            this.Parameters = new List<ParameterDefinition>();
        }

    }
}
