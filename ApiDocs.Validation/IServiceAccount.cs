namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IServiceAccount
    {
        string Name { get; }
        bool Enabled { get; }

        string BaseUrl { get; }
    }
}
