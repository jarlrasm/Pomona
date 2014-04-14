﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2014 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using Pomona.Common.Internals;
using Pomona.Common.Linq;
using Pomona.Common.Proxies;

namespace Pomona.Common
{
    public class ChildResourceRepository<TResource, TPostResponseResource>
        : ClientRepository<TResource, TPostResponseResource>
        where TResource : class, IClientResource
        where TPostResponseResource : IClientResource
    {
        private readonly IClientResource parent;


        public ChildResourceRepository(ClientBase client, string uri, IEnumerable results, IClientResource parent)
            : base(client, uri, results, parent)
        {
            this.parent = parent;
        }


        public override TPostResponseResource Post(IPostForm form)
        {
            RequestOptions requestOptions = new RequestOptions();
            AddEtagOptions(requestOptions);
            return (TPostResponseResource)Client.Post(Uri, (TResource)((object)form), requestOptions);
        }


        public override TPostResponseResource Post<TSubResource>(Action<TSubResource> postAction)
        {
            throw new NotImplementedException();
        }


        public override TPostResponseResource Post(Action<TResource> postAction)
        {
            return base.Post(postAction, AddEtagOptions);
        }


        private void AddEtagOptions(IRequestOptions options)
        {
            var parentResourceInfo = Client.GetMostInheritedResourceInterfaceInfo(this.parent.GetType());
            if (parentResourceInfo.HasEtagProperty)
            {
                var etag = parentResourceInfo.EtagProperty.GetValue(this.parent, null);
                options.ModifyRequest(r => r.Headers.Add("If-Match", string.Format("\"{0}\"", etag)));
            }
        }
    }

    public class ClientRepository<TResource, TPostResponseResource> :
        IClientRepository<TResource, TPostResponseResource>,
        IQueryable<TResource>
        where TResource : class, IClientResource
        where TPostResponseResource : IClientResource
    {
        private readonly ClientBase client;

        private readonly IEnumerable<TResource> results;
        private readonly string uri;


        public ClientRepository(ClientBase client, string uri, IEnumerable results, IClientResource parent)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            this.client = client;
            this.uri = uri;
            this.results = results as IEnumerable<TResource> ?? (results != null ? results.Cast<TResource>() : null);
        }


        public Type ElementType
        {
            get { return typeof(TResource); }
        }

        public Expression Expression
        {
            get { return Expression.Constant(Query()); }
        }

        public IQueryProvider Provider
        {
            get { return new RestQueryProvider(this.client); }
        }

        public string Uri
        {
            get { return this.uri; }
        }

        internal ClientBase Client
        {
            get { return this.client; }
        }


        public virtual TPostResponseResource Post(IPostForm form)
        {
            return (TPostResponseResource)this.client.Post(Uri, (TResource)((object)form), null);
        }


        public virtual TPostResponseResource Post<TSubResource>(Action<TSubResource> postAction)
            where TSubResource : class, TResource
        {
            return (TPostResponseResource)this.client.Post(Uri, postAction, null);
        }


        public virtual TPostResponseResource Post<TSubResource>(Action<TSubResource> postAction,
            Action<IRequestOptions<TSubResource>> options) where TSubResource : class, TResource
        {
            return (TPostResponseResource)this.client.Post(Uri, postAction, RequestOptions.Create(options));
        }


        public TResource Get(object id)
        {
            return this.client.Get<TResource>(GetResourceUri(id));
        }


        public IEnumerator<TResource> GetEnumerator()
        {
            return this.results != null ? this.results.GetEnumerator() : Query().GetEnumerator();
        }


        public TResource GetLazy(object id)
        {
            return this.client.GetLazy<TResource>(GetResourceUri(id));
        }


        public TSubResource Patch<TSubResource>(TSubResource resource,
            Action<TSubResource> patchAction,
            Action<IRequestOptions<TSubResource>> options) where TSubResource : class, TResource
        {
            return this.client.Patch(resource, patchAction, options);
        }


        public TSubResource Patch<TSubResource>(TSubResource resource, Action<TSubResource> patchAction)
            where TSubResource : class, TResource
        {
            return Patch(resource, patchAction, null);
        }


        public virtual TPostResponseResource Post(Action<TResource> postAction)
        {
            return (TPostResponseResource)this.client.Post(Uri, postAction, null);
        }


        public object Post<TPostForm>(TResource resource, TPostForm form)
            where TPostForm : class, IPostForm, IClientResource
        {
            if (resource == null)
                throw new ArgumentNullException("resource");
            if (form == null)
                throw new ArgumentNullException("form");

            return this.client.Post(((IHasResourceUri)resource).Uri, form, null);
        }


        public IQueryable<TSubResource> Query<TSubResource>()
            where TSubResource : TResource
        {
            return this.client.Query<TSubResource>(this.uri);
        }


        public IQueryable<TResource> Query()
        {
            return this.client.Query<TResource>(this.uri);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        private string GetResourceUri(object id)
        {
            return string.Format("{0}/{1}",
                this.uri,
                HttpUtility.UrlPathSegmentEncode(Convert.ToString(id, CultureInfo.InvariantCulture)));
        }


        public void Delete(TResource resource)
        {
            Client.Delete(resource);
        }
    }
}