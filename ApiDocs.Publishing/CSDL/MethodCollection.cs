using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Publishing.CSDL
{
    /// <summary>
    /// Wrapper around a collection of methods that operate on the same REST path.
    /// </summary>
    internal class MethodCollection : List<Validation.MethodDefinition>
    {
        public bool PostAllowed
        {
            get { return HttpVerbAllowed("POST"); }
        }
        public bool GetAllowed
        {
            get { return HttpVerbAllowed("GET"); }
        }
        public bool DeleteAllowed
        {
            get { return HttpVerbAllowed("DELETE"); }
        }
        public bool PutAllowed
        {
            get { return HttpVerbAllowed("PUT"); }
        }

        /// <summary>
        /// Indicates that all methods in this collection are Idempotent methods
        /// </summary>
        public bool AllMethodsIdempotent
        {
            get { return this.All(x => x.RequestMetadata.IsIdempotent); }
        }

        /// <summary>
        /// Returns the request body parameters if the collection contains only a single request method.
        /// </summary>
        public List<Validation.ParameterDefinition> RequestBodyParameters
        {
            get
            {
                // TODO: For collections with multiple requests, we should create a union of these requests
                return this.First().RequestBodyParameters;
            }
        }

        /// <summary>
        /// Returns the response type if the collection contains only a single request method.
        /// </summary>
        public Validation.ParameterDataType ResponseType
        {
            get
            {
                // TODO: For collections with multiple requests, we should make sure the return types are consistent.
                return this.First().ExpectedResponseMetadata.Type;
            }
        }



        protected bool HttpVerbAllowed(string verb)
        {
            var query = from m in this
                        where m.HttpMethodVerb().ToUpperInvariant() == verb
                        select verb;

            return query.Any();
        }


    }
}
