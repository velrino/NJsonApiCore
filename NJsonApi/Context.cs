﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NJsonApi
{
    public class Context
    {
        public Context(Configuration configuration, Uri requestUri)
        {
            Configuration = configuration;
            RequestUri = requestUri;
        }

        public Configuration Configuration { get; private set; }
        public Uri RequestUri { get; private set; }
        public string BaseUri {
            get
            {
                var baseUri = new Uri(RequestUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
                return baseUri.AbsoluteUri;
            }
        }
    }
}